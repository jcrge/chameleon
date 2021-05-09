using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chameleon
{
    class CompressedStateInfo
    {
        public string Path;
        public bool UnsavedChanges;

        public CompressedStateInfo()
        {
            Path = null;
            UnsavedChanges = false;
        }
    }
}