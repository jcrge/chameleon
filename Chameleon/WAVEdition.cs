using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Chameleon
{
    // Vista sobre un archivo WAV en formato PCM. Permite procesar un archivo para acceder a
    // sus propiedades, como el número de canales (mono o estéreo) o una vista sobre los datos
    // de sus muestras.
    class PcmWavView : IDisposable
    {
        public bool IsLittleEndian { get; }
        public int SampleRate { get; }
        public short NumChannels { get; }
        public short BytesPerSample { get; }
        public int SampleDataSize { get; }
        public MemoryMappedViewAccessor SampleData { get; }

        private MemoryMappedFile Mmf;

        public PcmWavView(string path)
        {
            Mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open);

            (int payloadSize, bool isLittleEndian) = WAVIO.ReadRIFFHeader(Mmf);
            IsLittleEndian = isLittleEndian;

            (int fmtOffset, int fmtSectionSize) = WAVIO.FindSectionOffset(Mmf, "fmt ", payloadSize, IsLittleEndian);
            (int dataOffset, int dataSectionSize) = WAVIO.FindSectionOffset(Mmf, "data", payloadSize, IsLittleEndian);
            SampleDataSize = dataSectionSize - 8;

            (SampleRate, NumChannels, BytesPerSample) =
                WAVIO.ReadPcmFmtSection(Mmf, fmtOffset, fmtSectionSize, IsLittleEndian);

            SampleData = Mmf.CreateViewAccessor(dataOffset + 8, dataSectionSize - 8);
        }

        private bool disposed = false;

        ~PcmWavView()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (!disposed)
            {
                try
                {
                    if (disposing)
                    {
                        Mmf.Dispose();
                        SampleData.Dispose();
                    }
                }
                finally
                {
                    disposed = true;
                }
            }
        }
    }

    // Métodos para realizar operaciones sobre archivos WAV, como juntarlos o dividirlos.
    class WAVEdition
    {
        // Devuelve true si y solo si `path` es una ruta válida a un archivo legible WAV en formato PCM.
        public static bool IsPcmWav(string path)
        {
            try
            {
                using (PcmWavView f = new PcmWavView(path))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }

        // Divide un archivo WAV en formato PCM en la ruta `sourcePath` en dos audios más cortos.
        // `midpointMsec` especifica el punto de ruptura en el audio en milisegundos. El segmento
        // previo al punto de ruptura se guarda en `leftDestPath`, y el segmento posterior, en
        // `rightDestPath`.
        public static void Split(string sourcePath, int midpointMsec, string leftDestPath, string rightDestPath)
        {
            if (midpointMsec <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    $"The given midpointMsec value of {midpointMsec} is too low.");
            }

            using (PcmWavView f = new PcmWavView(sourcePath))
            {
                // Número de bytes iniciales de la carga de "data" que deben conformar la carga total de la
                // sección "data" de leftDestPath.
                int leftDataByteCount = (int)((long)f.BytesPerSample * f.NumChannels * f.SampleRate * midpointMsec / 1000);
                int rightDataByteCount = f.SampleDataSize - leftDataByteCount;

                if (rightDataByteCount <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"The given midpointMsec value of {midpointMsec} is too high.");
                }

                using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(leftDestPath)))
                {
                    WAVIO.WritePcmWavRIFFHeader(bw, leftDataByteCount);
                    WAVIO.WritePcmWavFmtSection(bw, f.SampleRate, f.NumChannels, f.BytesPerSample);
                    WAVIO.WriteDataHeader(bw, leftDataByteCount);
                    WAVIO.CopySamples(bw, f.SampleData, 0, leftDataByteCount);

                    if (leftDataByteCount % 2 == 1)
                    {
                        bw.Write((byte)0);
                    }
                    bw.Flush();
                }

                using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(rightDestPath)))
                {
                    WAVIO.WritePcmWavRIFFHeader(bw, rightDataByteCount);
                    WAVIO.WritePcmWavFmtSection(bw, f.SampleRate, f.NumChannels, f.BytesPerSample);
                    WAVIO.WriteDataHeader(bw, rightDataByteCount);
                    WAVIO.CopySamples(bw, f.SampleData, leftDataByteCount, rightDataByteCount);

                    if (rightDataByteCount % 2 == 1)
                    {
                        bw.Write((byte)0);
                    }
                    bw.Flush();
                }
            }
        }
    }

    // Métodos a bajo nivel para leer, escribir y copiar tramos de archivos WAV.
    class WAVIO
    {
        private static readonly short AUDIO_FORMAT_PCM = 1;

        public static void WritePcmWavRIFFHeader(BinaryWriter bw, int sampleDataLength)
        {
            bw.Write("RIFF".ToCharArray());
            bw.Write(40 + sampleDataLength + (sampleDataLength % 2));
            bw.Write("WAVE".ToCharArray());
        }

        public static void WritePcmWavFmtSection(
            BinaryWriter bw, int sampleRate, short numChannels, short bytesPerSample)
        {
            bw.Write("fmt ".ToCharArray());
            bw.Write(16);
            bw.Write(AUDIO_FORMAT_PCM);
            bw.Write(numChannels);
            bw.Write(sampleRate);
            bw.Write(sampleRate * numChannels * bytesPerSample);
            bw.Write((short)(numChannels * bytesPerSample));
            bw.Write((short)(bytesPerSample * 8));
        }

        public static void WriteDataHeader(BinaryWriter bw, int sampleDataLength)
        {
            bw.Write("data".ToCharArray());
            bw.Write(sampleDataLength);
        }

        public static void CopySamples(
            BinaryWriter bw, MemoryMappedViewAccessor samplesAccessor, int samplesOffset, int samplesLength)
        {
            byte[] buffer = new byte[1024];
            int bytesLeft = samplesLength;
            while (bytesLeft > 0)
            {
                int bytesToRead = Math.Min(buffer.Length, bytesLeft);
                samplesAccessor.ReadArray(samplesOffset, buffer, 0, bytesToRead);
                bytesLeft -= bytesToRead;
                samplesOffset += bytesToRead;

                bw.Write(buffer, 0, bytesToRead);
            }
        }

        public static (int, short, short) ReadPcmFmtSection(
            MemoryMappedFile mmf, int fmtOffset, int fmtSectionSize, bool isLittleEndian)
        {
            byte[] buffer4 = new byte[4];
            byte[] buffer2 = new byte[2];
            using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor(fmtOffset, fmtSectionSize))
            {
                if (accessor.ReadArray(8, buffer2, 0, buffer2.Length) < buffer2.Length)
                {
                    throw new IOException($"Fmt section error: audio format: premature chunk end.");
                }
                int audioFormat = IntFromCharArray(EnsureBigEndian(buffer2, isLittleEndian));
                if (audioFormat != AUDIO_FORMAT_PCM)
                {
                    throw new IOException(
                        $"Fmt section error: audio format: expected PCM (code 1), but got code '{audioFormat}'.");
                }

                if (accessor.ReadArray(10, buffer2, 0, buffer2.Length) < buffer2.Length)
                {
                    throw new IOException($"Fmt section error: number of channels: premature chunk end.");
                }
                int numChannels = IntFromCharArray(EnsureBigEndian(buffer2, isLittleEndian));
                if (numChannels != 1 && numChannels != 2)
                {
                    throw new IOException($"Fmt section error: number of channels:"
                        + $" expected 1 (mono) or 2 (stereo), but got '{numChannels}'.");
                }

                if (accessor.ReadArray(12, buffer4, 0, buffer4.Length) < buffer4.Length)
                {
                    throw new IOException($"Fmt section error: sample rate: premature chunk end.");
                }
                int sampleRate = IntFromCharArray(EnsureBigEndian(buffer4, isLittleEndian));

                if (accessor.ReadArray(16, buffer4, 0, buffer4.Length) < buffer4.Length)
                {
                    throw new IOException($"Fmt section error: byte rate: premature chunk end.");
                }
                int byteRate = IntFromCharArray(EnsureBigEndian(buffer4, isLittleEndian));

                if (accessor.ReadArray(20, buffer2, 0, buffer2.Length) < buffer2.Length)
                {
                    throw new IOException($"Fmt section error: block align: premature chunk end.");
                }
                int blockAlign = IntFromCharArray(EnsureBigEndian(buffer2, isLittleEndian));

                if (accessor.ReadArray(22, buffer2, 0, buffer2.Length) < buffer2.Length)
                {
                    throw new IOException($"Fmt section error: bits per sample: premature chunk end.");
                }
                int bytesPerSample = IntFromCharArray(EnsureBigEndian(buffer2, isLittleEndian)) / 8;

                if (byteRate != sampleRate * numChannels * bytesPerSample)
                {
                    throw new IOException(
                        $"Fmt section error: byte rate ({byteRate}) contradicts expected value of"
                        + $" sample rate ({sampleRate})"
                        + $" * number of channels ({numChannels})"
                        + $" * bytes per sample ({bytesPerSample}).");
                }

                if (blockAlign != numChannels * bytesPerSample)
                {
                    throw new IOException(
                        $"Fmt section error: block align ({blockAlign}) contradicts expected value of"
                        + $" number of channels ({numChannels})"
                        + $" * bytes per sample ({bytesPerSample}).");
                }

                return (sampleRate, (short)numChannels, (short)bytesPerSample);
            }
        }

        public static (int, int) FindSectionOffset(
            MemoryMappedFile mmf, string targetSectionName, int riffPayloadSize, bool isLittleEndian)
        {
            int nextCheck = 12;
            byte[] buffer = new byte[4];
            using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor())
            {
                while (nextCheck < 8 + riffPayloadSize)
                {
                    if (accessor.ReadArray(nextCheck, buffer, 0, buffer.Length) < buffer.Length)
                    {
                        break;
                    }
                    string currSectionName = Encoding.UTF8.GetString(buffer);

                    if (accessor.ReadArray(nextCheck + 4, buffer, 0, buffer.Length) < buffer.Length)
                    {
                        break;
                    }
                    int currSectionSize = 8 + IntFromCharArray(EnsureBigEndian(buffer, isLittleEndian));

                    if (currSectionName == targetSectionName)
                    {
                        return (nextCheck, currSectionSize);
                    }

                    if (currSectionSize % 2 == 1)
                    {
                        // La sección "data" tiene un byte de padding al final si su tamaño total es impar.
                        // Hasta donde he podido entender, es la única sección que puede tener un tamaño
                        // impar o, si hubiera alguna más, debería cumplir también este requisito.
                        currSectionSize++;
                    }

                    nextCheck += currSectionSize;
                }

                throw new IOException($"Section '{targetSectionName}' not found.");
            }
        }

        public static (int, bool) ReadRIFFHeader(MemoryMappedFile mmf)
        {
            byte[] buffer = new byte[4];
            using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor(0, 12))
            {
                if (accessor.ReadArray(0, buffer, 0, buffer.Length) < buffer.Length)
                {
                    throw new IOException("Invalid magic number: premature file end.");
                }
                string magicNumber = Encoding.UTF8.GetString(buffer);
                if (magicNumber != "RIFF" && magicNumber != "RIFX")
                {
                    throw new IOException($"Invalid magic number: expected 'RIFF' or 'RIFX', but got '{magicNumber}'.");
                }
                bool isLittleEndian = magicNumber == "RIFF";

                if (accessor.ReadArray(4, buffer, 0, buffer.Length) < buffer.Length)
                {
                    throw new IOException("Invalid RIFF payload size number: premature file end.");
                }
                int payloadSize = IntFromCharArray(EnsureBigEndian(buffer, isLittleEndian));

                if (accessor.ReadArray(8, buffer, 0, buffer.Length) < buffer.Length)
                {
                    throw new IOException("Invalid RIFF payload type: premature file end.");
                }
                string payloadType = Encoding.UTF8.GetString(buffer);
                if (payloadType != "WAVE")
                {
                    throw new IOException($"Invalid RIFF payload type: expected 'WAVE', but got '{payloadType}'.");
                }

                return (payloadSize, isLittleEndian);
            }
        }

        private static int IntFromCharArray(byte[] source)
        {
            // Ej: 0x807060 = (0x80 << 16) + (0x70 << 8) + 0x60
            int res = 0;
            for (int i = 0; i < source.Length; i++)
            {
                res += source[i] << 8*(source.Length - 1 - i);
            }

            return res;
        }

        private static byte[] EnsureBigEndian(byte[] data, bool isLittleEndian)
        {
            if (!isLittleEndian)
            {
                return data;
            }

            int stopAt = data.Length / 2;
            for (int i = 0; i < stopAt; i++)
            {
                int leftBytePos = i;
                int rightBytePos = data.Length - 1 - i;

                byte oldLeftByte = data[leftBytePos];
                data[leftBytePos] = data[rightBytePos];
                data[rightBytePos] = oldLeftByte;
            }

            return data;
        }
    }
}