using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using AndroidX.RecyclerView.Widget;

namespace Chameleon
{
    class ProjectViewAdapter : RecyclerView.Adapter
    {
        public event EventHandler<ProjectViewAdapterClickEventArgs> ItemClick;
        public event EventHandler<ProjectViewAdapterClickEventArgs> ItemLongClick;
        private List<ChunkEntry> Chunks;

        public ProjectViewAdapter(List<ChunkEntry> chunks)
        {
            Chunks = chunks;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.rv_chunk, parent, false);
            return new ProjectViewAdapterViewHolder(itemView, OnClick, OnLongClick);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var context = viewHolder.ItemView.Context;
            ChunkEntry chunk = Chunks[position];

            string title = chunk.Name;
            string subs = chunk.Subtitles;

            double durationSec = chunk.DurationSec;
            int minutes = ((int)durationSec) / 60;
            int seconds = ((int)durationSec) % 60;
            int milliseconds = (int)((durationSec - ((int)durationSec)) * 1000);
            string duration = $"{minutes:00}:{seconds:00}.{milliseconds:000}";

            var holder = viewHolder as ProjectViewAdapterViewHolder;
            holder.Title.Text = string.IsNullOrEmpty(title) ? context.GetString(Resource.String.no_title) : title;
            holder.Subtitles.Text = string.IsNullOrEmpty(subs) ? context.GetString(Resource.String.no_subtitles) : subs;
            holder.Duration.Text = duration;
            //holder.ItemView.SetBackgroundColor(Color.ParseColor(position % 2 == 0 ? "#ff0000" : "#0000ff"));
        }

        public override int ItemCount => Chunks.Count;

        private void OnClick(ProjectViewAdapterClickEventArgs args) => ItemClick?.Invoke(this, args);
        private void OnLongClick(ProjectViewAdapterClickEventArgs args) => ItemLongClick?.Invoke(this, args);
    }

    public class ProjectViewAdapterViewHolder : RecyclerView.ViewHolder
    {
        public TextView Title { get; set; }
        public TextView Subtitles { get; set; }
        public TextView Duration { get; set; }

        public ProjectViewAdapterViewHolder(View itemView, Action<ProjectViewAdapterClickEventArgs> clickListener,
                            Action<ProjectViewAdapterClickEventArgs> longClickListener) : base(itemView)
        {
            Title = itemView.FindViewById<TextView>(Resource.Id.title);
            Subtitles = itemView.FindViewById<TextView>(Resource.Id.subtitles);
            Duration = itemView.FindViewById<TextView>(Resource.Id.duration);

            itemView.Click += (sender, e) => clickListener(new ProjectViewAdapterClickEventArgs {
                View = itemView,
                Position = AdapterPosition
            });
            itemView.LongClick += (sender, e) => longClickListener(new ProjectViewAdapterClickEventArgs {
                View = itemView,
                Position = AdapterPosition
            });
        }
    }

    public class ProjectViewAdapterClickEventArgs : EventArgs
    {
        public View View { get; set; }
        public int Position { get; set; }
    }
}