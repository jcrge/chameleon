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
    public class MidpointPreviewerActivity : AppCompatActivity
    {
        private AudioPlayer LeftPlayer;
        private AudioPlayer RightPlayer;
        private Button OkButton;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_midpoint_previewer);

            OkButton = FindViewById<Button>(Resource.Id.ok_button);
            LeftPlayer = FindViewById<AudioPlayer>(Resource.Id.left_player);
            RightPlayer = FindViewById<AudioPlayer>(Resource.Id.right_player);

            OkButton.Click += (s, e) => OkClicked();

            LeftPlayer.AudioSource = Settings.SplitOpLeftFile;
            RightPlayer.AudioSource = Settings.SplitOpRightFile;
        }

        private void OkClicked()
        {
            SetResult(Result.Ok);
            Finish();
        }

        protected override void OnDestroy()
        {
            LeftPlayer.Dispose();
            RightPlayer.Dispose();
            base.OnDestroy();
        }
    }
}