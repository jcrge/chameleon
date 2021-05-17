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
using Xamarin.Essentials;

namespace Chameleon
{
    class StagingAreaNotReadyException : Exception
    {
        public StagingAreaNotReadyException() : base()
        {

        }

        public StagingAreaNotReadyException(string message) : base(message)
        {

        }
    }

    class StagingArea
    {
        public static Project LoadRootDir()
        {
            if (!Directory.Exists(Settings.StagingAreaPath))
            {
                throw new StagingAreaNotReadyException($"Staging area directory '{Settings.StagingAreaPath}' does not exist.");
            }
            if (!Directory.Exists(Settings.UncompressedProjectPath))
            {
                throw new StagingAreaNotReadyException($"Project directory '{Settings.UncompressedProjectPath}' does not exist.");
            }
            if (!Directory.Exists(Settings.ChunksPath))
            {
                throw new StagingAreaNotReadyException($"Chunks directory '{Settings.ChunksPath}' does not exist.");
            }

            CompressedStateInfo compressedState;
            try
            {
                string compressedStateJson;
                using (StreamReader sr = new StreamReader(Settings.CompressedStatePath))
                {
                    compressedStateJson = sr.ReadToEnd();
                }
                compressedState = JsonConvert.DeserializeObject<CompressedStateInfo>(compressedStateJson);
            }
            catch (Exception e) when (e is IOException || e is JsonReaderException)
            {
                throw new StagingAreaNotReadyException(
                    $"I/O exception accessing {Settings.CompressedStatePath}: {e.Message}");
            }

            ProjectIndex index;
            try
            {
                string indexJson;
                using (StreamReader sr = new StreamReader(Settings.IndexPath))
                {
                    indexJson = sr.ReadToEnd();
                }
                index = JsonConvert.DeserializeObject<ProjectIndex>(indexJson);
            }
            catch (Exception e) when (e is IOException || e is JsonReaderException)
            {
                throw new StagingAreaNotReadyException(
                    $"I/O exception accessing {Settings.IndexPath}: {e.Message}");
            }

            return new Project(index, compressedState);
        }

        public static void Clean()
        {
            if (!Directory.Exists(Settings.StagingAreaPath))
            {
                throw new StagingAreaNotReadyException(
                    $"Staging area directory '{Settings.StagingAreaPath}' does not exist.");
            }

            if (Directory.Exists(Settings.UncompressedProjectPath))
            {
                Directory.Delete(Settings.UncompressedProjectPath, true);
            }

            if (File.Exists(Settings.CompressedStatePath))
            {
                File.Delete(Settings.CompressedStatePath);
            }
        }

        public static void PrepareNewProject()
        {
            Clean();
            using (StreamWriter sw = new StreamWriter(Settings.CompressedStatePath))
            {
                sw.WriteLine(JsonConvert.SerializeObject(new CompressedStateInfo()));
            }

            Directory.CreateDirectory(Settings.UncompressedProjectPath);
            Directory.CreateDirectory(Settings.ChunksPath);
            using (StreamWriter sw = new StreamWriter(Settings.IndexPath))
            {
                sw.WriteLine(JsonConvert.SerializeObject(new ProjectIndex()));
            }
        }

        public static void UncompressProject(string name)
        {
            Clean();
            Directory.CreateDirectory(Settings.UncompressedProjectPath);

            try
            {
                ZipFile.ExtractToDirectory(Settings.GetPathForProject(name), Settings.UncompressedProjectPath);
            }
            catch (InvalidDataException)
            {
                Clean();
                throw new IOException($"Invalid Chameleon compressed file: {name}.");
            }

            using (StreamWriter sw = new StreamWriter(Settings.CompressedStatePath))
            {
                sw.WriteLine(JsonConvert.SerializeObject(new CompressedStateInfo
                {
                    ProjectName = name,
                    UnsavedChanges = false,
                }));
            }
        }
    }
}