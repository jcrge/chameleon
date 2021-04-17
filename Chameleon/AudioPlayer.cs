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
using Plugin.SimpleAudioPlayer;

namespace Chameleon
{
    public class AudioPlayer : LinearLayout, IDisposable
    {
        private static readonly int POS_SHORT_JUMP_SECONDS = 1;
        private static readonly int POS_LONG_JUMP_SECONDS = 5;

        public TextView PositionText;
        public Button PlayPauseButton;
        public Button RewindButton;
        public Button FastForwardButton;
        public CheckBox LoopCheckBox;

        private ISimpleAudioPlayer Player;

        // IMPORTANTE: Es esta clase la que se encarga de liberar AudioStream al recibir
        // un valor nuevo para el campo y en Dispose.
        private Stream audioStream;
        public Stream AudioStream
        {
            get => audioStream;
            set
            {
                PrepareToStart();

                PlayPauseButton.Enabled = value != null;
                RewindButton.Enabled = value != null;
                FastForwardButton.Enabled = value != null;

                if (audioStream != null)
                {
                    audioStream.Dispose();
                }

                audioStream = value;
                if (value != null)
                {
                    Player.Load(value);
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

            PositionText = view.FindViewById<TextView>(Resource.Id.position_text);
            PlayPauseButton = view.FindViewById<Button>(Resource.Id.play_pause_button);
            RewindButton = view.FindViewById<Button>(Resource.Id.rewind_button);
            FastForwardButton = view.FindViewById<Button>(Resource.Id.fastforward_button);
            LoopCheckBox = view.FindViewById<CheckBox>(Resource.Id.loop_checkbox);

            Player = CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
            Player.PlaybackEnded += (s, e) =>
            {
                if (!Player.Loop)
                {
                    PrepareToStart();
                }
            };

            PlayPauseButton.Click += PlayPauseClicked;
            RewindButton.Click += (s, e) => Player.Seek(Player.CurrentPosition - POS_SHORT_JUMP_SECONDS);
            RewindButton.LongClick += (s, e) => Player.Seek(Player.CurrentPosition - POS_LONG_JUMP_SECONDS);
            FastForwardButton.Click += (s, e) => Player.Seek(Player.CurrentPosition + POS_SHORT_JUMP_SECONDS);
            FastForwardButton.LongClick += (s, e) => Player.Seek(Player.CurrentPosition + POS_LONG_JUMP_SECONDS);
            LoopCheckBox.CheckedChange += (s, e) => Player.Loop = LoopCheckBox.Checked;

            // AudioStream se debe establecer a null explícitamente para desactivar la interfaz.
            AudioStream = null;
        }

        private void PlayPauseClicked(object sender, EventArgs e)
        {
            if (!Player.IsPlaying)
            {
                Player.Play();
                PlayPauseButton.Text = Resources.GetText(Resource.String.action_pause);
            }
            else
            {
                // La posición cambia ligeramente durante la pausa. El efecto de volver a
                // position es que se repiten las últimas décimas o milésimas de segundo
                // reproducidas (el tiempo exacto depende de la latencia presente). Dependiendo
                // de cuánto tiempo sea, puede ser más o menos notable. En cualquier caso es
                // muy preferible a no modificar la posición, ya que entonces hay un pequeño
                // tramo que no se llega a escuchar en ningún momento y eso, para el uso que
                // se dará a este reproductor, es inaceptable.
                double position = Player.CurrentPosition;
                Player.Pause();
                Player.Seek(position);

                PlayPauseButton.Text = Resources.GetText(Resource.String.action_play);
            }
        }

        private void PrepareToStart()
        {
            Player.Stop();
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
                        audioStream?.Dispose();
                        Player?.Dispose();
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