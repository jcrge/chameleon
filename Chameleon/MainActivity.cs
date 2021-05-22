using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Xamarin.Essentials;
using Android.Content;
using System.Threading.Tasks;
using AndroidX.Core.App;
using Android;
using System.IO;
using Android.Widget;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace Chameleon
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private Button CreateProjectButton;
        private Button OpenProjectButton;
        private Button RecoverProjectButton;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            CreateProjectButton = FindViewById<Button>(Resource.Id.create_project_button);
            OpenProjectButton = FindViewById<Button>(Resource.Id.open_project_button);
            RecoverProjectButton = FindViewById<Button>(Resource.Id.recover_project_button);

            CreateProjectButton.Click += (s, e) => CreateProjectClicked();
            OpenProjectButton.Click += (s, e) => OpenProjectClicked();
            RecoverProjectButton.Click += (s, e) => RecoverProjectClicked();
        }

        protected override void OnResume()
        {
            base.OnResume();
            RecoverProjectButton.Enabled = ProjectReady();
        }

        private void CreateProjectClicked()
        {
            ConfirmDiscardSession(() =>
            {
                StagingArea.PrepareNewProject();
                StartProjectActivity();
            });
        }

        private static readonly int ACTIVITY_RESULT_PROJECT_SELECTED = 1;

        private void OpenProjectClicked()
        {
            Intent intent = new Intent(this, typeof(ActivitySelectProject));
            StartActivityForResult(intent, ACTIVITY_RESULT_PROJECT_SELECTED);
        }

        private void ProjectNameSelected(string name)
        {
            ConfirmDiscardSession(() =>
            {
                bool failed = false;
                try
                {
                    StagingArea.UncompressProject(name);
                    if (!ProjectReady())
                    {
                        failed = true;
                    }
                }
                catch (IOException)
                {
                    failed = true;
                }

                if (!failed)
                {
                    StartProjectActivity();
                }
                else
                {
                    StagingArea.Clean();
                    RecoverProjectButton.Enabled = false;

                    AlertDialog.Builder alert = new AlertDialog.Builder(this);
                    alert.SetTitle(GetString(Resource.String.error_uncompressing_project_alert_title));
                    alert.SetMessage(GetString(Resource.String.error_uncompressing_project_alert_message));
                    alert.SetPositiveButton(GetString(Android.Resource.String.Ok), (s, e) => { });
                    alert.Create().Show();
                }
            });
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (requestCode == ACTIVITY_RESULT_PROJECT_SELECTED && resultCode == Result.Ok)
            {
                ProjectNameSelected(data.GetStringExtra(ActivitySelectProject.CHOSEN_NAME));
            }
            else
            {
                base.OnActivityResult(requestCode, resultCode, data);
            }
        }

        private void RecoverProjectClicked()
        {
            StartProjectActivity();
        }

        private void StartProjectActivity()
        {
            StartActivity(
                new Intent(this, typeof(ProjectActivity)),
                ActivityOptions.MakeSceneTransitionAnimation(this).ToBundle());
        }

        private void ConfirmDiscardSession(Action next)
        {
            bool unsavedChanges;
            try
            {
                Project p = StagingArea.LoadRootDir();
                unsavedChanges = p.UnsavedChanges;
            }
            catch (StagingAreaNotReadyException)
            {
                unsavedChanges = false;
            }

            if (!unsavedChanges)
            {
                next();
            }
            else
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetTitle(GetString(Resource.String.discard_staging_area_alert_title));
                alert.SetMessage(GetString(Resource.String.discard_staging_area_alert_message));
                alert.SetPositiveButton(GetString(Android.Resource.String.Ok), (s, e) =>
                {
                    next();
                });
                alert.SetNegativeButton(GetString(Android.Resource.String.Cancel), (s, e) =>
                {
                });
                alert.Create().Show();
            }
        }

        private bool ProjectReady()
        {
            try
            {
                StagingArea.LoadRootDir();
                return true;
            }
            catch (StagingAreaNotReadyException)
            {
                return false;
            }
        }
	}
}
