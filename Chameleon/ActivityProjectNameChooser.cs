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
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;

namespace Chameleon
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class ActivityProjectNameChooser : AppCompatActivity
    {
        public static readonly string CHOSEN_NAME = "chosen_name";

        private EditText NameField;
        private Button OkButton;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_project_name_chooser);

            NameField = FindViewById<EditText>(Resource.Id.name);
            OkButton = FindViewById<Button>(Resource.Id.ok);

            OkButton.Enabled = false;
            OkButton.Click += (s, e) => OkClicked();
            NameField.TextChanged += (s, e) => OkButton.Enabled = IsValidFilename(NameField.Text);
        }

        private void OkClicked()
        {
            string name = NameField.Text;

            Intent intent = new Intent();
            intent.PutExtra(CHOSEN_NAME, name);
            SetResult(Result.Ok, intent);
            Finish();
        }

        private static bool IsValidFilename(string name)
        {
            if (name.Length == 0 || name.Length > 50)
            {
                return false;
            }

            if (File.Exists(Settings.GetPathForProject(name)))
            {
                return false;
            }

            for (int i = 0; i < name.Length; i++)
            {
                if ("*/:<>?\\|+[]".IndexOf(name[i]) != -1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}