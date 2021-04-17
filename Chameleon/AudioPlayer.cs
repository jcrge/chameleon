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

namespace Chameleon
{
    public class AudioPlayer : LinearLayout
    {
        TextView PositionText;
        Button PlayPauseButton;
        Button RewindButton;
        Button FastForwardButton;
        CheckBox LoopCheckBox;

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
            view.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            AddView(view);

            PositionText = view.FindViewById<TextView>(Resource.Id.position_text);
            PlayPauseButton = view.FindViewById<Button>(Resource.Id.play_pause_button);
            RewindButton = view.FindViewById<Button>(Resource.Id.rewind_button);
            FastForwardButton = view.FindViewById<Button>(Resource.Id.fastforward_button);
            LoopCheckBox = view.FindViewById<CheckBox>(Resource.Id.loop_checkbox);
        }
    }
}