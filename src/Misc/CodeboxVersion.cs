using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using VSlice.Codebox;

namespace VSlice
{
    public class CodeboxVersion
    {
        public string CodeBoxVersion { get; private set; }
        public string ThisAssemblyVersion { get; private set; }
        public DateTime ReleaseDate { get; private set; }
        public string Description { get; private set; }

        /// ----------------------------------------------------------------------------------
        /// <summary>
        /// Compares the version of the calling assembly with the specified app in codebox.  
        /// If the default version on codebox is newer than the calling assembly's version,
        /// then a filled-in CodeboxVersion is returned.  Otherwise [null] is returned.
        /// 
        /// Note: Likely reasons for thrown exceptions:
        ///       - codebox is down
        ///       - bad application name
        /// </summary>
        /// ----------------------------------------------------------------------------------
        public static CodeboxVersion GetNewerVersion(string applicationName)
        {
            CodeboxVersion newerVersion = null;

            Codebox.ReleaseService codebox = new ReleaseService();
            codebox.Credentials = System.Net.CredentialCache.DefaultCredentials;

            DataTable releases = codebox.GetReleasesByProjectName(applicationName).Tables[0];

            var releaseData =
                from release in releases.AsEnumerable()
                where release.Field<bool>("DefaultRelease") == true
                select new
                           {
                               Version = release.Field<string>("Version"),
                               ReleaseDate = release.Field<DateTime>("ReleaseDate"),
                               Description = release.Field<string>("Description")
                           };

            // If there is no default release found, we abort the check.
            if (!releaseData.Any())
            {
                return null;
            }

            string[] networkVersion = releaseData.First().Version.Split('.');

            Assembly assembly = Assembly.GetCallingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string thisAssemblyVersion = fvi.ProductVersion;
            string[] myVersion = thisAssemblyVersion.Split('.');

            for (int i = 0; i < 4 && i < networkVersion.Length && i < myVersion.Length; i++)
            {
                int networkNumber = int.Parse(networkVersion[i]);
                int myNumber = int.Parse(myVersion[i]);
                if (networkNumber < myNumber) break;
                if (networkNumber > myNumber)
                {
                    newerVersion = new CodeboxVersion()
                                       {
                                           CodeBoxVersion = releaseData.First().Version,
                                           ThisAssemblyVersion = thisAssemblyVersion,
                                           Description = releaseData.First().Description,
                                           ReleaseDate = releaseData.First().ReleaseDate
                                       };
                    break;
                }
            }

            return newerVersion;
        }
    }
}
