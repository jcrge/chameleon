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
    public class ActivitySelectProject : AppCompatActivity
    {
        public static readonly string CHOSEN_NAME = "chosen_name";

        private ListView Projects;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_select_project);

            Projects = FindViewById<ListView>(Resource.Id.projects);

            List<string> names = Directory.GetFiles(Settings.ProjectsPath, "*.*")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToList();

            ArrayAdapter adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, names);
            Projects.Adapter = adapter;

            Projects.ItemClick += (s, e) => NameClicked(((TextView)e.View).Text);
        }

        private void NameClicked(string name)
        {
            Intent intent = new Intent();
            intent.PutExtra(CHOSEN_NAME, name);
            SetResult(Result.Ok, intent);
            Finish();
        }
    }
}