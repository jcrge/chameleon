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

namespace Chameleon
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class MidpointChooserActivity : AppCompatActivity
    {
        public static readonly string INPUT_CHUNK_ID = "input_chunk_id";
        public static readonly string OUTPUT_CHUNK_ID = "output_chunk_id";
        private static readonly int ACTIVITY_CONFIRM_MIDPOINT = 1;

        private AudioPlayer AudioPlayer;
        private Button OkButton;
        private string ChunkId;
        private Project Project;
        private PcmWavView PcmWavView;

        private int MidpointMsec;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_midpoint_chooser);

            OkButton = FindViewById<Button>(Resource.Id.ok_button);
            AudioPlayer = FindViewById<AudioPlayer>(Resource.Id.player);

            OkButton.Click += (s, e) => OkClicked();

            ChunkId = Intent.GetStringExtra(INPUT_CHUNK_ID);
            Project = StagingArea.LoadRootDir();
            PcmWavView = new PcmWavView(Settings.GetPathForChunk(ChunkId));
            AudioPlayer.AudioSource = Settings.GetPathForChunk(ChunkId);
        }

        private void OkClicked()
        {
            MidpointMsec = AudioPlayer.CurrentPositionMsec;
            if (0 < MidpointMsec && MidpointMsec < AudioPlayer.DurationMsec)
            {
                OkButton.Enabled = false;
                AudioPlayer.Pause();

                if (Directory.Exists(Settings.SplitOpDir))
                {
                    Directory.Delete(Settings.SplitOpDir, true);
                }
                Directory.CreateDirectory(Settings.SplitOpDir);

                WAVEdition.Split(PcmWavView, MidpointMsec, Settings.SplitOpLeftFile, Settings.SplitOpRightFile);

                StartActivityForResult(typeof(MidpointPreviewerActivity), ACTIVITY_CONFIRM_MIDPOINT);
            }
            else
            {
                Toast.MakeText(this, Resource.String.error_invalid_midpoint, ToastLength.Short).Show();
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (requestCode == ACTIVITY_CONFIRM_MIDPOINT)
            {
                if (resultCode == Result.Ok)
                {
                    double midpointSec = MidpointMsec / (double)1000;
                    Project.ReplaceWithSubchunks(
                        ChunkId,
                        midpointSec,
                        Settings.SplitOpLeftFile,
                        PcmWavView.DurationSec - midpointSec,
                        Settings.SplitOpRightFile);

                    Intent intent = new Intent();
                    intent.PutExtra(OUTPUT_CHUNK_ID, ChunkId);
                    SetResult(Result.Ok, intent);
                    Finish();
                }
                else
                {
                    OkButton.Enabled = true;
                }
            }
            else
            {
                base.OnActivityResult(requestCode, resultCode, data);
            }
        }

        protected override void OnDestroy()
        {
            AudioPlayer.Dispose();
            PcmWavView.Dispose();
            base.OnDestroy();
        }
    }
}