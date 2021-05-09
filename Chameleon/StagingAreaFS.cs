using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Chameleon
{
    class StagingAreaFS
    {
        private string rootPath;
        public string RootPath
        {
            get => rootPath;
        }

        public string CompressedStatePath
        {
            get => Path.Combine(RootPath, "compressed-state.json");
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

        public StagingAreaFS(string rootPath)
        {
            this.rootPath = rootPath;
        }
    }
}