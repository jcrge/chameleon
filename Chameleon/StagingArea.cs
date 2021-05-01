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
        private StagingAreaFS StagingAreaFS;

        public StagingArea(string rootPath)
        {
            StagingAreaFS = new StagingAreaFS(rootPath);
        }

        public Project LoadRootDir()
        {
            if (!Directory.Exists(StagingAreaFS.RootPath))
            {
                throw new StagingAreaNotReadyException($"Root directory '{StagingAreaFS.RootPath}' does not exist.");
            }
            if (!Directory.Exists(StagingAreaFS.ProjectPath))
            {
                throw new StagingAreaNotReadyException($"Project directory '{StagingAreaFS.ProjectPath}' does not exist.");
            }
            if (!Directory.Exists(StagingAreaFS.ChunksPath))
            {
                throw new StagingAreaNotReadyException($"Chunks directory '{StagingAreaFS.ChunksPath}' does not exist.");
            }

            string compressedFilePath;
            try
            {
                using (StreamReader sr = new StreamReader(StagingAreaFS.StoredAtPath))
                {
                    compressedFilePath = sr.ReadToEnd().Trim();
                }
            }
            catch (IOException e)
            {
                throw new StagingAreaNotReadyException($"I/O exception accessing {StagingAreaFS.StoredAtPath}: {e.Message}");
            }

            ProjectIndex index;
            try
            {
                string indexJson;
                using (StreamReader sr = new StreamReader(StagingAreaFS.IndexPath))
                {
                    indexJson = sr.ReadToEnd();
                }
                index = JsonConvert.DeserializeObject<ProjectIndex>(indexJson);
            }
            catch (Exception e) when (e is IOException || e is JsonReaderException)
            {
                throw new StagingAreaNotReadyException($"I/O exception accessing {StagingAreaFS.IndexPath}: {e.Message}");
            }

            return new Project(StagingAreaFS, index, compressedFilePath);
        }

        public void Clean()
        {
            if (!Directory.Exists(StagingAreaFS.RootPath))
            {
                throw new StagingAreaNotReadyException($"Root directory '{StagingAreaFS.RootPath}' does not exist.");
            }

            if (Directory.Exists(StagingAreaFS.ProjectPath))
            {
                Directory.Delete(StagingAreaFS.ProjectPath, true);
            }

            if (File.Exists(StagingAreaFS.StoredAtPath))
            {
                File.Delete(StagingAreaFS.StoredAtPath);
            }
        }

        public void PrepareNewProject(string compressedFilePath)
        {
            Clean();
            using (StreamWriter sw = new StreamWriter(StagingAreaFS.StoredAtPath))
            {
                sw.WriteLine(compressedFilePath);
            }

            Directory.CreateDirectory(StagingAreaFS.ProjectPath);
            Directory.CreateDirectory(StagingAreaFS.ChunksPath);
            using (StreamWriter sw = new StreamWriter(StagingAreaFS.IndexPath))
            {
                sw.WriteLine(JsonConvert.SerializeObject(new ProjectIndex()));
            }
        }

        public void UncompressProject(string compressedFilePath)
        {
            Clean();
            Directory.CreateDirectory(StagingAreaFS.ProjectPath);
            ZipFile.ExtractToDirectory(compressedFilePath, StagingAreaFS.ProjectPath);
            using (StreamWriter sw = new StreamWriter(StagingAreaFS.StoredAtPath))
            {
                sw.WriteLine(compressedFilePath);
            }
        }
    }
}