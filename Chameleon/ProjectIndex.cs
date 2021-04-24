using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chameleon
{
    class ProjectIndex
    {
        public int NextId;
        public List<ChunkEntry> Chunks;

        public ProjectIndex()
        {
            NextId = 0;
            Chunks = new List<ChunkEntry>();
        }
    }

    class ChunkEntry
    {
        public string Id;
        public string Name;
        public string Remarks;
        public string Subtitles;
    }
}