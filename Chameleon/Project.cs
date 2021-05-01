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
    }
}