﻿using Android.App;
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
        private Project Project;
        private StagingArea StagingArea;
        private RecyclerView RecyclerView;
        private ProjectViewAdapter Adapter;

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

            StagingArea = new StagingArea(Settings.STAGING_AREA_DIR);
            Project = StagingArea.LoadRootDir();

            Adapter = new ProjectViewAdapter(Project.Index.Chunks);
            RecyclerView.SetAdapter(Adapter);
            RecyclerView.SetLayoutManager(new LinearLayoutManager(this));
            RecyclerView.AddItemDecoration(
                new DividerItemDecoration(RecyclerView.Context, DividerItemDecoration.Vertical));
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
                    if (Project.CompressedFilePath == null)
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

                case Resource.Id.action_reorder_mode:
                    break;

                case Resource.Id.action_split:
                    break;

                default:
                    return base.OnOptionsItemSelected(item);
            }

            return true;
        }

        public override void OnBackPressed()
        {
            Project.FlushCompressedState();
            Project.FlushIndex();
            FinishAffinity();
        }

        private void SaveChanges()
        {
            Project.UpdateCompressedFile();
            Toast.MakeText(this, Resource.String.changes_saved_successfully, ToastLength.Long);
        }

        private void SaveChangesAs()
        {
            Intent saveIntent = new Intent(Intent.ActionCreateDocument);
            saveIntent.AddCategory(Intent.CategoryOpenable);
            saveIntent.SetType("*/*");
            StartActivityForResult(Intent.CreateChooser(saveIntent, "..."), ACTIVITY_RESULT_SAVE_AS_DIALOG);
        }

        private static readonly int ACTIVITY_RESULT_SAVE_AS_DIALOG = 1;

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (requestCode == ACTIVITY_RESULT_SAVE_AS_DIALOG && resultCode == Result.Ok)
            {
                Project.CompressedFilePath = data.Data.ToString();
                SaveChanges();
            }
            else
            {
                base.OnActivityResult(requestCode, resultCode, data);
            }
        }
    }
}