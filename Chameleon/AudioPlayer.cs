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

        private Button PlayPauseButton;
        private Button RewindButton;
        private Button FastForwardButton;
        private CheckBox LoopCheckBox;

        private MediaPlayer Player;

        private string audioSource;
        public string AudioSource
        {
            get => audioSource;
            set
            {
                PrepareToStart();

                audioSource = value;
                UpdateControls();
                if (audioSource != null)
                {
                    Player.Reset();
                    Player.SetDataSource(audioSource);
                    Player.Prepare();
                    Player.Looping = LoopCheckBox.Checked;
                }
            }
        }

        private void UpdateControls()
        {
            PlayPauseButton.Enabled = audioSource != null && !controlsLocked;
            RewindButton.Enabled = audioSource != null && !controlsLocked;
            FastForwardButton.Enabled = audioSource != null && !controlsLocked;
        }

        public int CurrentPositionMsec
        {
            get => Player.CurrentPosition;
        }

        public int DurationMsec
        {
            get => Player.Duration;
        }

        private bool loopingLocked;
        public bool LoopingLocked
        {
            get => loopingLocked;
            set
            {
                loopingLocked = value;
                LoopCheckBox.Enabled = !value;
                UpdateLoopingValue();
            }
        }

        private bool controlsLocked;
        public bool ControlsLocked
        {
            get => controlsLocked;
            set
            {
                controlsLocked = value;
                UpdateControls();
            }
        }

        private void UpdateLoopingValue()
        {
            Player.Looping = LoopCheckBox.Enabled && LoopCheckBox.Checked;
        }

        public event EventHandler PlaybackStopped;

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

        public void Pause()
        {
            if (Player.IsPlaying)
            {
                Player.Pause();
                PlayPauseButton.Text = Resources.GetText(Resource.String.action_play);
            }
        }

        public void Play()
        {
            if (!Player.IsPlaying)
            {
                Player.Start();
                PlayPauseButton.Text = Resources.GetText(Resource.String.action_pause);
            }
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
                    PlaybackStopped?.Invoke(this, EventArgs.Empty);
                }
            };

            PlayPauseButton.Click += PlayPauseClicked;
            RewindButton.Click += (s, e) => Player.SeekTo(Player.CurrentPosition - POS_SHORT_JUMP_SECONDS);
            RewindButton.LongClick += (s, e) => Player.SeekTo(Player.CurrentPosition - POS_LONG_JUMP_SECONDS);
            FastForwardButton.Click += (s, e) => Player.SeekTo(Player.CurrentPosition + POS_SHORT_JUMP_SECONDS);
            FastForwardButton.LongClick += (s, e) => Player.SeekTo(Player.CurrentPosition + POS_LONG_JUMP_SECONDS);
            LoopCheckBox.CheckedChange += (s, e) => UpdateLoopingValue();

            // AudioSource se debe establecer a null explícitamente para desactivar la interfaz.
            AudioSource = null;

            loopingLocked = false;
        }

        private void PlayPauseClicked(object sender, EventArgs e)
        {
            if (!Player.IsPlaying)
            {
                Play();
            }
            else
            {
                Pause();
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