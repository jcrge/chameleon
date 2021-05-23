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
        private IMenu Menu;

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
                    ToggleSelection(entry, position);
                    break;

                case RecyclerViewMode.Normal:
                    Intent intent = new Intent();
                    intent.SetComponent(new ComponentName(Application.Context,
                        Java.Lang.Class.FromType(typeof(ChunkActivity)).Name));
                    intent.PutExtra(ChunkActivity.INPUT_CHUNK_ID, entry.Chunk.Id);
                    StartActivityForResult(intent, ACTIVITY_RESULT_VIEW_CHUNK);
                    break;
            }
        }

        private void EntryLongClicked(int position, SelectableChunkEntry entry)
        {
            switch (Mode)
            {
                case RecyclerViewMode.Selection:
                    ToggleSelection(entry, position);
                    break;

                case RecyclerViewMode.Normal:
                    Mode = RecyclerViewMode.Selection;
                    ToggleSelection(entry, position);
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

        private void ToggleSelection(SelectableChunkEntry entry, int position)
        {
            entry.Selected = !entry.Selected;
            Adapter.NotifyItemChanged(position);

            UpdateMenu(Menu);
        }

        private void UpdateMenu(IMenu menu)
        {
            int selectedCount = Entries.Where(e => e.Selected).Count();
            menu.FindItem(Resource.Id.action_delete).SetVisible(selectedCount > 0);
            menu.FindItem(Resource.Id.action_split).SetVisible(selectedCount == 1);
        }

        private void DeleteClicked()
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle(GetString(Resource.String.confirm_delete_chunks_title));
            alert.SetMessage(GetString(Resource.String.confirm_delete_chunks_message));
            alert.SetPositiveButton(GetString(Android.Resource.String.Ok), (s, e) =>
            {
                for (int i = Entries.Count - 1; i >= 0; i--)
                {
                    SelectableChunkEntry entry = Entries[i];
                    if (entry.Selected)
                    {
                        Project.DeleteChunk(entry.Chunk.Id);
                        Entries.RemoveAt(i);
                        Adapter.NotifyItemRemoved(i);
                    }
                }

                Mode = RecyclerViewMode.Normal;
            });
            alert.SetNegativeButton(GetString(Android.Resource.String.Cancel), (s, e) =>
            {
            });
            alert.Create().Show();
        }

        private void SplitClicked()
        {
            string id = Entries.Find(e => e.Selected).Chunk.Id;
            Intent intent = new Intent();
            intent.SetComponent(new ComponentName(Application.Context,
                Java.Lang.Class.FromType(typeof(MidpointChooserActivity)).Name));
            intent.PutExtra(MidpointChooserActivity.INPUT_CHUNK_ID, id);
            StartActivityForResult(intent, ACTIVITY_RESULT_SPLIT);
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
            UpdateMenu(menu);
            Menu = menu;
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
                    DeleteClicked();
                    break;

                case Resource.Id.action_split:
                    SplitClicked();
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
        private static readonly int ACTIVITY_RESULT_SPLIT = 2;
        private static readonly int ACTIVITY_RESULT_VIEW_CHUNK = 3;

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (requestCode == ACTIVITY_RESULT_SAVE_AS_DIALOG && resultCode == Result.Ok)
            {
                Project.Name = data.GetStringExtra(ActivityProjectNameChooser.CHOSEN_NAME);
                SaveChanges();
            }
            else if (requestCode == ACTIVITY_RESULT_SPLIT && resultCode == Result.Ok)
            {
                Mode = RecyclerViewMode.Normal;

                // Otra instancia de Project ha actualizado el índice, por lo que volvemos
                // a cargarlo.
                Project = StagingArea.LoadRootDir();

                string id = data.GetStringExtra(MidpointChooserActivity.OUTPUT_CHUNK_ID);
                int pos = Entries.FindIndex(e => e.Chunk.Id == id);

                Entries.RemoveAt(pos);
                Entries.Insert(pos, new SelectableChunkEntry(Project.Index.Chunks[pos]));
                Adapter.NotifyItemChanged(pos);

                Entries.Insert(pos + 1, new SelectableChunkEntry(Project.Index.Chunks[pos + 1]));
                Adapter.NotifyItemInserted(pos + 1);
            }
            else if (requestCode == ACTIVITY_RESULT_VIEW_CHUNK && resultCode == Result.Ok)
            {
                // Otra instancia de Project ha actualizado el índice, por lo que volvemos
                // a cargarlo.
                Project = StagingArea.LoadRootDir();

                string id = data.GetStringExtra(ChunkActivity.OUTPUT_CHUNK_ID);
                int pos = Entries.FindIndex(e => e.Chunk.Id == id);

                Entries[pos] = new SelectableChunkEntry(Project.Index.Chunks[pos]);
                Adapter.NotifyItemChanged(pos);
            }
            else
            {
                base.OnActivityResult(requestCode, resultCode, data);
            }
        }
    }
}