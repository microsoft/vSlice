using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VSlice
{
    /// -----------------------------------------------------------------------
    /// <summary>
    /// Class to help with auto updating.  
    /// How to use:
    /// Next to the install on the server, there should be a file with the same
    /// name as the install file with ".ver" tagged on the end.  The contents
    /// should be a plain multipart version string with exactly four parts:
    /// 
    ///     5.3.0.0
    ///  
    /// or... put a url pointing to the updated install location:
    /// 
    ///     http://install.myserver.com/tools/myInstaller.exe
    ///     
    /// </summary>
    /// -----------------------------------------------------------------------
    class AutoUpdater
    {
        string _installUrl;
        string _versionUrl;
        Task _checkForUpdates;
        Assembly _callingAssembly;

        /// -----------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="installUrl">Pointer to a file that installs your app.</param>
        /// <param name="versionNameId">Text that identifies the name part of the version. 
        /// Expecting something like MyApp=3.2.1.0</param>
        /// -----------------------------------------------------------------------
        public AutoUpdater(string installUrl)
        {
            _installUrl = installUrl;
            _versionUrl = _installUrl + ".ver";
            _callingAssembly = Assembly.GetCallingAssembly();
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// WatchForUpdates - Set up a background task to check the server for 
        /// updates on a regular interval.
        /// </summary>
        /// -----------------------------------------------------------------------
        public void WatchForUpdates(TimeSpan interval)
        {
            _checkForUpdates = Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var latestVersion = _installUrl;
                        // keep following url forwards until we get a real version
                        while (latestVersion.ToLower().Contains("http"))
                        {
                            latestVersion = GetLatestVersion();
                        }

                        if (VersionIsGreater(latestVersion))
                        {
                            var result = System.Windows.MessageBox.Show(
                                $"There is a newer version of {_callingAssembly.GetName().Name} available.  Install it?",
                                _callingAssembly.GetName().Name,
                                MessageBoxButton.YesNo);
                            if (result == MessageBoxResult.Yes)
                            {
                                Process.Start(_installUrl);
                                System.Environment.Exit(0);
                            }
                        }

                    }
                    catch { }
                    finally
                    {
                        Task.Delay(interval).Wait();
                    }
                }
            });
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// VersionIsGreater - true if one ver
        /// </summary>
        /// -----------------------------------------------------------------------
        private bool VersionIsGreater(string serverVersion)
        {
            var parts = serverVersion.Split('.');
            if (parts.Length != 4)
            {
                Debug.WriteLine("version format is wrong: " + serverVersion);
                return false;
            }
            var myVersion = _callingAssembly.GetName().Version;
            Debug.WriteLine($"Comparing versions:  Local={myVersion}, Server={serverVersion}");
            if (int.Parse(parts[0]) > myVersion.Major) return true;
            if (int.Parse(parts[1]) > myVersion.Minor) return true;
            if (int.Parse(parts[2]) > myVersion.Build) return true;
            if (int.Parse(parts[3]) > myVersion.Revision) return true;
            return false;
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Check the install location to see the latest version
        /// </summary>
        /// -----------------------------------------------------------------------
        private string GetLatestVersion()
        {
            Debug.WriteLine("Checking for server version at: " + _versionUrl);
            var versionToReturn = "0.0.0.0";
            string versionFileContents;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(_versionUrl);

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    versionFileContents = reader.ReadToEnd().Trim();
                }

                if (versionFileContents.ToLower().Contains("http")) return versionFileContents;

                var versionParts = versionFileContents.Split('.');
                if (versionParts.Length == 4) versionToReturn = versionFileContents;
                else Debug.WriteLine("Rejecting bad version from the server: " + versionFileContents);
            }
            catch(Exception e)
            {
                Debug.WriteLine("There was an error checking the version: " + e.ToString());
            }

            return versionToReturn;
        }
    }
}
