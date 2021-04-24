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
    class StagingAreaNotLoadedException : Exception { }
    class StagingAreaLoadedException : Exception { }

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
        private bool isLoaded;
        public bool IsLoaded
        {
            get => isLoaded;
        }

        public string StoredAtPath
        {
            get => Path.Combine(RootPath, "stored-at.txt");
        }

        private string compressedFilePath;
        public string CompressedFilePath
        {
            get
            {
                if (!isLoaded)
                {
                    throw new StagingAreaNotLoadedException();
                }

                return compressedFilePath;
            }
        }

        private string rootPath;
        public string RootPath
        {
            get => rootPath;
        }

        public string ProjectPath
        {
            get => Path.Combine(RootPath, "current-project");
        }

        public string IndexPath
        {
            get => Path.Combine(ProjectPath, "index.json");
        }

        public string ChunksPath
        {
            get => Path.Combine(ProjectPath, "chunks");
        }

        public string GetPathForChunk(string id)
        {
            return Path.Combine(ChunksPath, $"{id}.wav");
        }

        private ProjectIndex index;
        public ProjectIndex Index
        {
            get
            {
                if (!isLoaded)
                {
                    throw new StagingAreaNotLoadedException();
                }

                return index;
            }
        }

        public StagingArea(string rootPath)
        {
            this.rootPath = rootPath;
            isLoaded = false;
        }

        public void Load()
        {
            if (isLoaded)
            {
                throw new StagingAreaLoadedException();
            }

            if (!Directory.Exists(RootPath))
            {
                throw new StagingAreaNotReadyException($"Root directory '{RootPath}' does not exist.");
            }
            if (!Directory.Exists(ProjectPath))
            {
                throw new StagingAreaNotReadyException($"Project directory '{ProjectPath}' does not exist.");
            }
            if (!Directory.Exists(ChunksPath))
            {
                throw new StagingAreaNotReadyException($"Chunks directory '{ChunksPath}' does not exist.");
            }

            try
            {
                using (StreamReader sr = new StreamReader(StoredAtPath))
                {
                    compressedFilePath = sr.ReadToEnd().Trim();
                }
            }
            catch (IOException e)
            {
                throw new StagingAreaNotReadyException($"I/O exception accessing {StoredAtPath}: {e.Message}");
            }

            try
            {
                string indexJson;
                using (StreamReader sr = new StreamReader(IndexPath))
                {
                    indexJson = sr.ReadToEnd();
                }
                index = JsonConvert.DeserializeObject<ProjectIndex>(indexJson);
            }
            catch (Exception e) when (e is IOException || e is JsonReaderException)
            {
                throw new StagingAreaNotReadyException($"I/O exception accessing {IndexPath}: {e.Message}");
            }

            isLoaded = true;
        }

        public void CleanStagingArea()
        {
            if (isLoaded)
            {
                throw new StagingAreaLoadedException();
            }

            if (!Directory.Exists(RootPath))
            {
                throw new StagingAreaNotReadyException($"Root directory '{RootPath}' does not exist.");
            }

            if (Directory.Exists(ProjectPath))
            {
                Directory.Delete(ProjectPath, true);
            }

            if (File.Exists(StoredAtPath))
            {
                File.Delete(StoredAtPath);
            }
        }

        public void PrepareNewProject(string compressedFilePath)
        {
            if (isLoaded)
            {
                throw new StagingAreaLoadedException();
            }

            CleanStagingArea();
            using (StreamWriter sw = new StreamWriter(StoredAtPath))
            {
                sw.WriteLine(compressedFilePath);
            }

            Directory.CreateDirectory(ProjectPath);
            Directory.CreateDirectory(ChunksPath);
            using (StreamWriter sw = new StreamWriter(IndexPath))
            {
                sw.WriteLine(JsonConvert.SerializeObject(new ProjectIndex()));
            }
        }

        public void UncompressProject(string compressedFilePath)
        {
            if (isLoaded)
            {
                throw new StagingAreaLoadedException();
            }

            CleanStagingArea();
            Directory.CreateDirectory(ProjectPath);
            ZipFile.ExtractToDirectory(compressedFilePath, ProjectPath);
            using (StreamWriter sw = new StreamWriter(StoredAtPath))
            {
                sw.WriteLine(compressedFilePath);
            }
        }

        public void CompressProject()
        {
            if (!isLoaded)
            {
                throw new StagingAreaNotLoadedException();
            }

            Flush();
            ZipFile.CreateFromDirectory(ProjectPath, compressedFilePath);
        }

        public void Flush()
        {
            if (!isLoaded)
            {
                throw new StagingAreaNotLoadedException();
            }

            using (StreamWriter sw = new StreamWriter(IndexPath))
            {
                sw.Write(JsonConvert.SerializeObject(index));
            }
        }
    }
}