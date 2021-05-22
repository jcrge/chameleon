using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.FloatingActionButton;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace Chameleon
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class ProjectActivity : AppCompatActivity
    {
        private enum RecyclerViewMode { Selection, Normal }

        private Project Project;
        private RecyclerView RecyclerView;
        private ProjectViewAdapter Adapter;
        private List<SelectableChunkEntry> Entries;
        private RecyclerViewMode Mode;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_project);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += (s, e) => NewChunk();

            RecyclerView = FindViewById<RecyclerView>(Resource.Id.chunks);

            Project = StagingArea.LoadRootDir();

            Entries = Project.Index.Chunks.Select(c => new SelectableChunkEntry(c)).ToList();
            Mode = RecyclerViewMode.Normal;
            if (savedInstanceState != null)
            {
                bool[] selectedRows = savedInstanceState.GetBooleanArray(STATE_SELECTED_ROWS);
                for (int i = 0; i < selectedRows.Length; i++)
                {
                    Entries[i].Selected = selectedRows[i];
                }

                Mode = (RecyclerViewMode)savedInstanceState.GetInt("RECYCLER_VIEW_MODE");
            }

            Adapter = new ProjectViewAdapter(Entries);
            RecyclerView.SetAdapter(Adapter);
            RecyclerView.SetLayoutManager(new LinearLayoutManager(this));
            RecyclerView.AddItemDecoration(
                new DividerItemDecoration(RecyclerView.Context, DividerItemDecoration.Vertical));

            Adapter.ItemClick += (s, e) => EntryClicked(e.Position, Entries[e.Position]);
            Adapter.ItemLongClick += (s, e) => EntryLongClicked(e.Position, Entries[e.Position]);
        }

        private static readonly string STATE_SELECTED_ROWS = "SELECTED_ROWS";
        private static readonly string STATE_RECYCLER_VIEW_MODE = "RECYCLER_VIEW_MODE";
        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutBooleanArray(STATE_SELECTED_ROWS, Entries.Select(e => e.Selected).ToArray());
            outState.PutInt(STATE_RECYCLER_VIEW_MODE, (int)Mode);
        }

        private void EntryClicked(int position, SelectableChunkEntry entry)
        {
            switch (Mode)
            {
                case RecyclerViewMode.Selection:
                    entry.Selected = !entry.Selected;
                    Adapter.NotifyItemChanged(position);
                    break;
            }
        }

        private void EntryLongClicked(int position, SelectableChunkEntry entry)
        {
            switch (Mode)
            {
                case RecyclerViewMode.Selection:
                    entry.Selected = !entry.Selected;
                    Adapter.NotifyItemChanged(position);
                    break;

                case RecyclerViewMode.Normal:
                    Mode = RecyclerViewMode.Selection;
                    entry.Selected = true;
                    Adapter.NotifyItemChanged(position);
                    break;
            }
        }

        public override void OnBackPressed()
        {
            switch (Mode)
            {
                case RecyclerViewMode.Selection:
                    for (int i = 0; i < Entries.Count; i++)
                    {
                        if (Entries[i].Selected)
                        {
                            Entries[i].Selected = false;
                            Adapter.NotifyItemChanged(i);
                        }
                    }
                    Mode = RecyclerViewMode.Normal;
                    break;

                case RecyclerViewMode.Normal:
                    base.OnBackPressed();
                    break;
            }
        }

        private async void NewChunk()
        {
            FileResult result = await FilePicker.PickAsync();
            if (result == null)
            {
                return;
            }

            try
            {
                Project.AppendChunk(result.FullPath);
                int pos = Project.Index.Chunks.Count - 1;

                Entries.Insert(pos, new SelectableChunkEntry(Project.Index.Chunks.Last()));

                Adapter.NotifyItemInserted(pos);
                RecyclerView.ScrollToPosition(pos);
            }
            catch (IOException)
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetTitle(GetString(Resource.String.error_appending_chunk_alert_title));
                alert.SetMessage(GetString(Resource.String.error_appending_chunk_alert_message));
                alert.SetPositiveButton(GetString(Android.Resource.String.Ok), (s, e) => { });
                alert.Create().Show();
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_project, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_save:
                    if (Project.Name == null)
                    {
                        SaveChangesAs();
                    }
                    else
                    {
                        SaveChanges();
                    }
                    break;

                case Resource.Id.action_save_as:
                    SaveChangesAs();
                    break;

                case Resource.Id.action_close:
                    break;

                case Resource.Id.action_delete:
                    break;

                case Resource.Id.action_split:
                    break;

                default:
                    return base.OnOptionsItemSelected(item);
            }

            return true;
        }

        private void SaveChanges()
        {
            Project.UpdateCompressedFile();
            Toast.MakeText(this, Resource.String.changes_saved_successfully, ToastLength.Long).Show();
        }

        private void SaveChangesAs()
        {
            Intent intent = new Intent(this, typeof(ActivityProjectNameChooser));
            StartActivityForResult(intent, ACTIVITY_RESULT_SAVE_AS_DIALOG);
        }

        private static readonly int ACTIVITY_RESULT_SAVE_AS_DIALOG = 1;

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (requestCode == ACTIVITY_RESULT_SAVE_AS_DIALOG && resultCode == Result.Ok)
            {
                Project.Name = data.GetStringExtra(ActivityProjectNameChooser.CHOSEN_NAME);
                SaveChanges();
            }
            else
            {
                base.OnActivityResult(requestCode, resultCode, data);
            }
        }
    }
}