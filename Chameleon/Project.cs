﻿using Android.App;
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
        public string Name
        {
            get => CompressedState.ProjectName;
            set
            {
                CompressedState.ProjectName = value;
                FlushCompressedState();
            }
        }

        public bool UnsavedChanges
        {
            get => CompressedState.UnsavedChanges;
        }

        public ProjectIndex Index;
        private CompressedStateInfo CompressedState;

        public Project(ProjectIndex index, CompressedStateInfo compressedState)
        {
            Index = index;
            CompressedState = compressedState;
        }

        public void UpdateCompressedFile()
        {
            if (CompressedState.ProjectName == null)
            {
                throw new InvalidOperationException("ProjectName is null in CompressedState.");
            }

            FlushIndex();
            string path = Settings.GetPathForProject(CompressedState.ProjectName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            ZipFile.CreateFromDirectory(Settings.UncompressedProjectPath, path);

            CompressedState.UnsavedChanges = false;
            FlushCompressedState();
        }

        public void FlushCompressedState()
        {
            using (StreamWriter sw = new StreamWriter(Settings.CompressedStatePath))
            {
                sw.Write(JsonConvert.SerializeObject(CompressedState));
            }
        }

        public void FlushIndex()
        {
            using (StreamWriter sw = new StreamWriter(Settings.IndexPath))
            {
                sw.Write(JsonConvert.SerializeObject(Index));
            }
        }

        private void IndexUpdated()
        {
            FlushIndex();

            CompressedState.UnsavedChanges = true;
            FlushCompressedState();
        }

        public void AppendChunk(string path)
        {
            double durationSec;
            try
            {
                using (PcmWavView view = new PcmWavView(path))
                {
                    durationSec = view.DurationSec;
                }
            }
            catch (IOException)
            {
                throw new IOException($"Not a PCM WAV file: {path}.");
            }

            string id = "" + Index.NextId;

            File.Copy(path, Settings.GetPathForChunk(id));

            string name = Path.GetFileNameWithoutExtension(path);
            if (name.Length > 15)
            {
                name = name.Substring(0, 15) + "...";
            }

            Index.Chunks.Add(new ChunkEntry
            {
                Id = id,
                Name = name,
                DurationSec = durationSec,
            });
            Index.NextId++;

            IndexUpdated();
        }

        public void SplitChunk(string sourceChunkId, int midpointMsec)
        {
            double midpointSec = midpointMsec / (double)1000;
            string leftId = "" + Index.NextId;
            string rightId = "" + (Index.NextId + 1);

            double sourceChunkDurationSec;
            using (PcmWavView pcmWavView = new PcmWavView(Settings.GetPathForChunk(sourceChunkId)))
            {
                sourceChunkDurationSec = pcmWavView.DurationSec;
                WAVEdition.Split(
                    pcmWavView,
                    midpointMsec,
                    Settings.GetPathForChunk(leftId),
                    Settings.GetPathForChunk(rightId));
            }

            int sourceChunkPos = Index.Chunks.FindIndex(e => e.Id == sourceChunkId);
            ChunkEntry sourceChunk = Index.Chunks[sourceChunkPos];

            Index.Chunks.RemoveAt(sourceChunkPos);
            Index.Chunks.Insert(sourceChunkPos, new ChunkEntry
            {
                Id = leftId,
                Name = $"({sourceChunk.Name ?? "..."})[:{midpointMsec}ms]",
                DurationSec = midpointSec,
            });
            Index.Chunks.Insert(sourceChunkPos + 1, new ChunkEntry
            {
                Id = rightId,
                Name = $"({sourceChunk.Name ?? "..."})[{midpointMsec}ms:]",
                DurationSec = sourceChunkDurationSec - midpointSec,
            });
            Index.NextId += 2;

            IndexUpdated();

            File.Delete(Settings.GetPathForChunk(sourceChunkId));
        }

        public void DeleteChunk(string id)
        {
            Index.Chunks.RemoveAt(Index.Chunks.FindIndex(e => e.Id == id));
            IndexUpdated();

            File.Delete(Settings.GetPathForChunk(id));
        }

        public void CloneChunk(string id, int newPos)
        {
            //Index.Chunks.Insert(newPos, (ChunkEntry)Index.Chunks.Find(e => e.Id == id).Clone());
	    //FlushIndex();
        }
    }
}
