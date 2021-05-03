using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Chameleon
{
    class Project
    {
        // Si se implementa el setter, este debe actualizar `stored-at.txt`.
        public string CompressedFilePath { get; }

        private ProjectIndex Index;
        private StagingAreaFS StagingAreaFS;

        public Project(StagingAreaFS stagingAreaFS, ProjectIndex index, string compressedFilePath)
        {
            StagingAreaFS = stagingAreaFS;
            Index = index;
            CompressedFilePath = compressedFilePath;
        }

        public void UpdateCompressedFile()
        {
            FlushIndex();
            ZipFile.CreateFromDirectory(StagingAreaFS.ProjectPath, CompressedFilePath);
        }

        public void FlushIndex()
        {
            using (StreamWriter sw = new StreamWriter(StagingAreaFS.IndexPath))
            {
                sw.Write(JsonConvert.SerializeObject(Index));
            }
        }

        public void AppendChunk(string path)
        {
            if (!WAVEdition.IsPcmWav(path))
            {
                throw new IOException($"Not a PCM WAV file: {path}.");
            }

            string id = "" + Index.NextId;

            File.Copy(path, StagingAreaFS.GetPathForChunk(id));

            string name = Path.GetFileNameWithoutExtension(path);
            if (name.Length > 15)
            {
                name = name.Substring(0, 15) + "...";
            }

            Index.Chunks.Add(new ChunkEntry
            {
                Id = id,
                Name = name,
            });
            Index.NextId++;

            FlushIndex();
        }

        public void SplitChunk(string sourceChunkId, int midpointMsec)
        {
            string leftId = "" + Index.NextId;
            string rightId = "" + (Index.NextId + 1);

            WAVEdition.Split(
                StagingAreaFS.GetPathForChunk(sourceChunkId),
                midpointMsec,
                StagingAreaFS.GetPathForChunk(leftId),
                StagingAreaFS.GetPathForChunk(rightId));

            int sourceChunkPos = Index.Chunks.FindIndex(e => e.Id == sourceChunkId);
            ChunkEntry sourceChunk = Index.Chunks[sourceChunkPos];

            Index.Chunks.RemoveAt(sourceChunkPos);
            Index.Chunks.Insert(sourceChunkPos, new ChunkEntry
            {
                Id = leftId,
                Name = $"({sourceChunk.Name ?? "..."})[:{midpointMsec}ms]",
            });
            Index.Chunks.Insert(sourceChunkPos + 1, new ChunkEntry
            {
                Id = rightId,
                Name = $"({sourceChunk.Name ?? "..."})[{midpointMsec}ms:]",
            });
            Index.NextId += 2;
            FlushIndex();

            File.Delete(StagingAreaFS.GetPathForChunk(sourceChunkId));
        }

        public void DeleteChunk(string id)
        {
            Index.Chunks.RemoveAt(Index.Chunks.FindIndex(e => e.Id == id));
            FlushIndex();

            File.Delete(StagingAreaFS.GetPathForChunk(id));
        }

        public void CloneChunk(string id, int newPos)
        {
            //Index.Chunks.Insert(newPos, (ChunkEntry)Index.Chunks.Find(e => e.Id == id).Clone());
	    //FlushIndex();
        }
    }
}
