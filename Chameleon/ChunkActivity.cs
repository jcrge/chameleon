using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xamarin.Essentials;
using AndroidX.AppCompat.Widget;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using AndroidX.Core.App;
using Android;
using AndroidX.Core.Content;
using Android.Content.PM;

namespace Chameleon
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class ChunkActivity : AppCompatActivity
    {
        public static readonly string INPUT_CHUNK_ID = "input_chunk_id";
        public static readonly string OUTPUT_CHUNK_ID = "output_chunk_id";

        private AudioPlayer ChunkPlayer;
        private AudioPlayer AttemptPlayer;
        private AudioRecorder Recorder;
        private Button ComparisonButton;
        private EditText NewTitle;
        private EditText NewSubtitles;
        private EditText NewRemarks;

        private Project Project;
        private ChunkEntry ChunkEntry;

        private bool Comparing = false;
        private bool CanRecord = true;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_chunk);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            ChunkPlayer = FindViewById<AudioPlayer>(Resource.Id.chunk_player);
            AttemptPlayer = FindViewById<AudioPlayer>(Resource.Id.attempt_player);
            Recorder = FindViewById<AudioRecorder>(Resource.Id.recorder);
            ComparisonButton = FindViewById<Button>(Resource.Id.comparison_button);
            NewTitle = FindViewById<EditText>(Resource.Id.new_title);
            NewSubtitles = FindViewById<EditText>(Resource.Id.new_subtitles);
            NewRemarks = FindViewById<EditText>(Resource.Id.new_remarks);

            Project = StagingArea.LoadRootDir();

            string chunkId = Intent.GetStringExtra(INPUT_CHUNK_ID);
            ChunkEntry = Project.Index.Chunks.Find(e => e.Id == chunkId);

            NewTitle.Text = ChunkEntry.Name;
            NewSubtitles.Text = ChunkEntry.Subtitles;
            NewRemarks.Text = ChunkEntry.Remarks;
            ChunkPlayer.AudioSource = Settings.GetPathForChunk(ChunkEntry.Id);
            Recorder.AudioDestination = Settings.LastAttemptPath;

            Recorder.RecordingStarted += (s, e) => RecordingStarted();
            Recorder.RecordingReceived += p => RecordingReceived();

            ComparisonButton.Click += (s, e) => ComparisonButtonClicked();
            ComparisonButton.Enabled = false;

            if (savedInstanceState != null)
            {
                if (savedInstanceState.GetBoolean(ATTEMPT_EXISTS))
                {
                    AttemptPlayer.AudioSource = Settings.LastAttemptPath;
                    ComparisonButton.Enabled = true;
                }
            }

            SupportActionBar.Title = string.IsNullOrEmpty(ChunkEntry.Name)
                ? GetString(Resource.String.no_title)
                : ChunkEntry.Name;

            if (ContextCompat.CheckSelfPermission(Application.Context, Manifest.Permission.RecordAudio)
                != (int)Permission.Granted)
            {
                CanRecord = false;
                Recorder.Enabled = false;

                ActivityCompat.RequestPermissions(
                    Platform.CurrentActivity,
                    new string[] { Manifest.Permission.RecordAudio },
                    REQUEST_MIC);
            }
        }

        private static readonly string ATTEMPT_EXISTS = "attempt_exists";

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutBoolean(ATTEMPT_EXISTS, AttemptPlayer.AudioSource != null);
        }

        private void RecordingStarted()
        {
            ChunkPlayer.ControlsLocked = true;
            ChunkPlayer.Pause();
            AttemptPlayer.ControlsLocked = true;
            AttemptPlayer.Pause();
            ComparisonButton.Enabled = false;
        }

        private void RecordingReceived()
        {
            AttemptPlayer.AudioSource = Settings.LastAttemptPath;

            ChunkPlayer.ControlsLocked = false;
            AttemptPlayer.ControlsLocked = false;
            ComparisonButton.Enabled = true;
        }

        private void ChunkToAttemptEvent(object sender, EventArgs e)
        {
            ChunkPlayer.ControlsLocked = true;
            AttemptPlayer.ControlsLocked = false;
            AttemptPlayer.Play();
        }

        private void AttemptToChunkEvent(object sender, EventArgs e)
        {
            AttemptPlayer.ControlsLocked = true;
            ChunkPlayer.ControlsLocked = false;
            ChunkPlayer.Play();
        }

        private void ComparisonButtonClicked()
        {
            if (!Comparing)
            {
                ComparisonButton.Text = GetString(Android.Resource.String.Cancel);

                AttemptPlayer.ControlsLocked = true;
                AttemptPlayer.Pause();
                ChunkPlayer.ControlsLocked = true;
                ChunkPlayer.Pause();
                Recorder.Enabled = false;

                AttemptPlayer.LoopingLocked = true;
                ChunkPlayer.LoopingLocked = true;

                ChunkPlayer.PlaybackStopped += ChunkToAttemptEvent;
                AttemptPlayer.PlaybackStopped += AttemptToChunkEvent;

                ChunkPlayer.ControlsLocked = false;
                ChunkPlayer.Play();
            }
            else
            {
                ComparisonButton.Text = GetString(Resource.String.button_compare);

                AttemptPlayer.ControlsLocked = false;
                ChunkPlayer.ControlsLocked = false;
                Recorder.Enabled = CanRecord;

                AttemptPlayer.LoopingLocked = false;
                ChunkPlayer.LoopingLocked = false;

                ChunkPlayer.PlaybackStopped -= ChunkToAttemptEvent;
                AttemptPlayer.PlaybackStopped -= AttemptToChunkEvent;
            }

            Comparing = !Comparing;
        }

        public override void OnBackPressed()
        {
            UpdateMetadata();

            Intent intent = new Intent();
            intent.PutExtra(OUTPUT_CHUNK_ID, ChunkEntry.Id);
            SetResult(Result.Ok, intent);
            Finish();
        }

        protected override void OnDestroy()
        {
            ChunkPlayer.Dispose();
            AttemptPlayer.Dispose();
            Recorder.Dispose();

            UpdateMetadata();

            base.OnDestroy();
        }

        private void UpdateMetadata()
        {
            ChunkEntry.Name = NewTitle.Text;
            ChunkEntry.Subtitles = NewSubtitles.Text;
            ChunkEntry.Remarks = NewRemarks.Text;
            Project.FlushIndex();
        }

        private static readonly int REQUEST_MIC = 1;

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode == REQUEST_MIC && grantResults.Length == 1 && grantResults[0] == Permission.Granted)
            {
                CanRecord = true;
                Recorder.Enabled = true;
            }
        }
    }
}