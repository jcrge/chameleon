using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Xamarin.Essentials;
using System.Threading.Tasks;
using AndroidX.Core.App;
using Android;
using System.IO;

namespace Chameleon
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        AudioPlayer audioPlayer;
        AudioRecorder audioRecorder;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            audioPlayer = FindViewById<AudioPlayer>(Resource.Id.audio_player);
            audioRecorder = FindViewById<AudioRecorder>(Resource.Id.audio_recorder);
            //audioPlayer.AudioSource = await GetTestStream();

            ActivityCompat.RequestPermissions(this, new string[] { 
                Manifest.Permission.WriteExternalStorage,
                Manifest.Permission.RecordAudio
            }, 1);

            audioRecorder.AudioDestination = "/storage/emulated/0/Download/testfile2.out";
            audioRecorder.RecordingReceived += filePath => audioPlayer.AudioSource = filePath;

            //StagingArea stagingArea = new StagingArea(FileSystem.AppDataDirectory);
            StagingArea stagingArea = new StagingArea("/storage/emulated/0/Download/test/");
            stagingArea.PrepareNewProject("/storage/emulated/0/Download/compressed.chm");
            Project project = stagingArea.LoadRootDir();
            project.AppendChunk("/storage/emulated/0/Download/yy.wav");
            project.AppendChunk("/storage/emulated/0/Download/yy.wav");
            project.SplitChunk("0", 6000);
            project.SplitChunk("1", 3000);
            project.SplitChunk("2", 2500);

            Console.WriteLine("debug");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            audioPlayer.Dispose();
        }

        async Task<string> GetTestStream()
        {
            FileResult result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Testing FilePicker"
            });

            return result.FullPath;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View) sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (View.IOnClickListener)null).Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}
}
