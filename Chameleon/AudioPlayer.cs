using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Android.Media;

namespace Chameleon
{
    public class AudioPlayer : LinearLayout, IDisposable
    {
        private static readonly int POS_SHORT_JUMP_SECONDS = 2000;
        private static readonly int POS_LONG_JUMP_SECONDS = 5000;

        public Button PlayPauseButton;
        public Button RewindButton;
        public Button FastForwardButton;
        public CheckBox LoopCheckBox;

        private MediaPlayer Player;

        private string audioSource;
        public string AudioSource
        {
            get => audioSource;
            set
            {
                PrepareToStart();

                PlayPauseButton.Enabled = value != null;
                RewindButton.Enabled = value != null;
                FastForwardButton.Enabled = value != null;

                audioSource = value;
                if (audioSource != null)
                {
                    Player.Reset();
                    Player.SetDataSource(audioSource);
                    Player.Prepare();
                    Player.Looping = LoopCheckBox.Checked;
                }
            }
        }

        public AudioPlayer(Context context) : base(context)
        {
            Initialize();
        }

        public AudioPlayer(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        public AudioPlayer(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            Initialize();
        }

        private void Initialize()
        {
            LayoutInflater inflater = (LayoutInflater)Context.GetSystemService(Context.LayoutInflaterService);
            View view = inflater.Inflate(Resource.Layout.audio_player, null, true);
            view.LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            AddView(view);

            PlayPauseButton = view.FindViewById<Button>(Resource.Id.play_pause_button);
            RewindButton = view.FindViewById<Button>(Resource.Id.rewind_button);
            FastForwardButton = view.FindViewById<Button>(Resource.Id.fastforward_button);
            LoopCheckBox = view.FindViewById<CheckBox>(Resource.Id.loop_checkbox);

            Player = new MediaPlayer();
            Player.Completion += (s, e) =>
            {
                if (!Player.Looping)
                {
                    PrepareToStart();
                }
            };

            PlayPauseButton.Click += PlayPauseClicked;
            RewindButton.Click += (s, e) => Player.SeekTo(Player.CurrentPosition - POS_SHORT_JUMP_SECONDS);
            RewindButton.LongClick += (s, e) => Player.SeekTo(Player.CurrentPosition - POS_LONG_JUMP_SECONDS);
            FastForwardButton.Click += (s, e) => Player.SeekTo(Player.CurrentPosition + POS_SHORT_JUMP_SECONDS);
            FastForwardButton.LongClick += (s, e) => Player.SeekTo(Player.CurrentPosition + POS_LONG_JUMP_SECONDS);
            LoopCheckBox.CheckedChange += (s, e) => Player.Looping = LoopCheckBox.Checked;

            // AudioSource se debe establecer a null explícitamente para desactivar la interfaz.
            AudioSource = null;
        }

        private void PlayPauseClicked(object sender, EventArgs e)
        {
            if (!Player.IsPlaying)
            {
                Player.Start();
                PlayPauseButton.Text = Resources.GetText(Resource.String.action_pause);
            }
            else
            {
                Player.Pause();
                PlayPauseButton.Text = Resources.GetText(Resource.String.action_play);
            }
        }

        private void PrepareToStart()
        {
            Player.Pause();
            Player.SeekTo(0);
            PlayPauseButton.Text = Resources.GetText(Resource.String.action_play);
        }

        ~AudioPlayer()
        {
            Dispose(false);
        }

        private bool disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                try
                {
                    if (disposing)
                    {
                        Player?.Release();
                    }
                }
                finally
                {
                    disposed = true;
                    base.Dispose(disposing);
                }
            }
        }
    }
}