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
using Xamarin.Essentials;

namespace Chameleon
{
    class Settings
    {
        public static readonly string StagingAreaPath = FileSystem.AppDataDirectory;

        public static string CompressedStatePath
        {
            get => Path.Combine(StagingAreaPath, "compressed-state.json");
        }

        public static string SplitOpDir
        {
            get => Path.Combine(StagingAreaPath, "split-op");
        }

        public static string SplitOpLeftFile
        {
            get => Path.Combine(SplitOpDir, "left.wav");
        }

        public static string SplitOpRightFile
        {
            get => Path.Combine(SplitOpDir, "right.wav");
        }

        public static string UncompressedProjectPath
        {
            get => Path.Combine(StagingAreaPath, "current-project");
        }

        public static string IndexPath
        {
            get => Path.Combine(UncompressedProjectPath, "index.json");
        }

        public static string ChunksPath
        {
            get => Path.Combine(UncompressedProjectPath, "chunks");
        }

        public static string GetPathForChunk(string id)
        {
            return Path.Combine(ChunksPath, $"{id}.wav");
        }

        public static string ProjectsPath
        {
            get => Application.Context.GetExternalFilesDir(null).Path;
        }

        public static string GetPathForProject(string projectName)
        {
            return Path.Combine(ProjectsPath, $"{projectName}.chm");
        }
    }
}