using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using VSlice.Properties;

namespace VSlice
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string[] Args { get; set; }
        AutoUpdater _updater;
        /// -----------------------------------------------------------------------
        /// <summary>
        /// OnStartup
        /// </summary>
        /// -----------------------------------------------------------------------
        protected override void OnStartup(StartupEventArgs e)
        {

            Args = e.Args;
            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
            }

            if(e.Args.Length > 0 && e.Args[0] == "install")
            {
                RegisterUrlHandler();
                Application.Current.Shutdown();
                return;
            }

            _updater = new AutoUpdater("https://ostoolsinstall.blob.core.windows.net/tools/vSliceSetup.exe");
            _updater.WatchForUpdates(TimeSpan.FromHours(1));
            base.OnStartup(e);
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// RegisterUrlHandler - make it so vslice: urls are handled by this app
        /// </summary>
        /// -----------------------------------------------------------------------
        private void RegisterUrlHandler()
        {
            RegistryKey key = Registry.ClassesRoot.CreateSubKey("vslice");

            var currentAssembly = Assembly.GetExecutingAssembly();

            key.SetValue("", "URL:vSlice Target");
            key.SetValue("URL Protocol", "");
            var iconKey = key.CreateSubKey("DefaultIcon");
            iconKey.SetValue("","vSlice.exe,1");

            var commandKey = key.CreateSubKey("shell\\open\\command");
            commandKey.SetValue("", $"\"{currentAssembly.Location}\" \"%1\"");
        }
    }
}
