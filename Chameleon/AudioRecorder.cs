using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chameleon
{
    public class AudioRecorder : Button
    {
        private MediaRecorder Recorder;

        private string audioDestination;
        public string AudioDestination
        {
            get => audioDestination;
            set
            {
                if (Recording)
                {
                    StopRecording();
                }

                audioDestination = value;
                Enabled = audioDestination != null;
                if (audioDestination != null)
                {
                    Recorder.SetAudioSource(AudioSource.Mic);
                    Recorder.SetOutputFormat(OutputFormat.ThreeGpp);
                    Recorder.SetAudioEncoder(AudioEncoder.AmrNb);
                    Recorder.SetOutputFile(audioDestination);
                    Recorder.Prepare();
                }
            }
        }

        public delegate void DRecordingReceived(string outputFile);
        public event DRecordingReceived RecordingReceived;

        public AudioRecorder(Context context) : base(context)
        {
            Initialize();
        }

        public AudioRecorder(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        public AudioRecorder(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            Initialize();
        }

        private void Initialize()
        {
            Text = Resources.GetText(Resource.String.action_start_recording);
            Recorder = new MediaRecorder();
            Click += (s, e) => Record();

            AudioDestination = null;
        }

        private bool Recording = false;

        private void Record()
        {
            if (!Recording)
            {
                Text = Resources.GetText(Resource.String.action_stop_recording);
                Recording = true;
                Recorder.Start();
            }
            else
            {
                StopRecording();
            }
        }

        private void StopRecording()
        {
            Text = Resources.GetText(Resource.String.action_start_recording);
            Recording = false;
            Recorder.Stop();

            RecordingReceived(AudioDestination);
        }

        ~AudioRecorder()
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
                        Recorder?.Release();
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