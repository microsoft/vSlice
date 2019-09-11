using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.BI.Cosmos.IO;
using System.Management.Automation;
using VcClient;
using System.Management.Automation.Runspaces;

namespace VSlice
{

    // ******************************************************************************
    /// <summary>
    /// Use this instead of a DirectoryInfo Object
    /// </summary>
    // ******************************************************************************
    public class CosmosDirectoryInfo : IDirectoryInfo
    {
        #region fields and properties

        public string Name { get; private set; }
        public string FullName { get; private set; }
        private List<IItemData> files;
        private List<IItemData> directories;
        public double CosmosSize { get; set; }

        private bool scanned = false;

        #endregion

        public static ColumnInfo Columns { get; private set; } = new ColumnInfo(new[] { STREAMSIZE }, null);
        public const string STREAMSIZE = "StreamSize";

        // ******************************************************************************
        /// <summary>
        /// String path Constructor
        /// </summary>
        /// <param name="path">Name of the folder to scan</param>
        // ******************************************************************************
        public CosmosDirectoryInfo(string path)
        {
            Name = Path.GetFileName(path);
            FullName = path;
            CosmosSize = ScanDirectory(path, out files, out directories);
        }

        public CosmosDirectoryInfo(string path, double size)
        {
            Name = Path.GetFileName(path);
            FullName = path;
            CosmosSize = size;
        }

        // ******************************************************************************
        /// <summary>
        /// Interface for DirectoryInfo.GetFiles()
        /// </summary>
        // ******************************************************************************
        public IItemData[] GetFiles()
        {
            if (!scanned) ScanDirectory(FullName, out files, out directories);
            return files.ToArray();
        }

        // ******************************************************************************
        /// <summary>
        /// Replacement for DirectoryInfo.GetDirectories()
        /// </summary>
        /// <returns>Array of ISliceDirectoryInfo objects</returns>
        // ******************************************************************************
        public IDirectoryInfo[] GetDirectories()
        {
            if (!scanned)
            {
                ScanDirectory(FullName, out files, out directories);
            }

            CosmosDirectoryInfo[] array = new CosmosDirectoryInfo[directories.Count];
            for (int i = 0; i < directories.Count; i++)
            {
                array[i] = new CosmosDirectoryInfo(directories[i].FullName, directories[i].GetValue(STREAMSIZE));
            }

            return array;
        }

        // ******************************************************************************
        /// <summary>
        /// Scans the current directory for files and other directories
        /// </summary>
        /// <returns>Array of SliceCosmosDirectoryInfo objects</returns>
        // ******************************************************************************
        long ScanDirectory(string path, out List<IItemData> newFiles, out List<IItemData> directories)
        {
            scanned = true;
            Debug.WriteLine("Scanning " + path);


            directories = new List<IItemData>();
            newFiles = new List<IItemData>();
            long totalSize = 0;
            if(path == "Cosmos")
            {
                MessageBox.Show("To scan a Cosmos directory, drag and drop the URL for that folder from the address bar in Internet Explorer." +
                    "  (You must have already cached your Cosmos credentials on the local computer using scope.exe.)",
                                                         "Cosmos Scan");
                return 0;
            }

            bool retry = true;
            while (retry)
            {
                try
                {

                    RunspaceConfiguration rsc = RunspaceConfiguration.Create();
                    using(Runspace rs = RunspaceFactory.CreateRunspace(rsc))
                    {
                        rs.Open();
                        using (RunspaceInvoke scriptInvoker = new RunspaceInvoke())
                        {
                            //Debug.Write(scriptInvoker.Invoke("[IntPtr]::Size")[0].ToString());
                            scriptInvoker.Invoke("Import-Module cosmos");
                            Collection<PSObject> cosmosMetadatas = scriptInvoker.Invoke(String.Format("Get-CosmosStream {0}", path));
                            foreach (PSObject cosmosMetadata in cosmosMetadatas)
                            {
                                Debug.Write(".");

                                long cosmosFileSize;
                                long.TryParse(cosmosMetadata.Properties["Length"].Value.ToString(), out cosmosFileSize);
                                cosmosFileSize = Math.Max(0, cosmosFileSize);
                                totalSize += cosmosFileSize;

                                string fullName = cosmosMetadata.Properties["StreamName"].Value.ToString();//.TrimEnd('/');
                                var fileInfo = new ColumnarItemData(fullName.Split('\\', '/'), Columns.ColumnLookup);
                                fileInfo.SetValue(STREAMSIZE, cosmosFileSize);

                                bool isDirectory;
                                bool.TryParse(cosmosMetadata.Properties["IsDirectory"].Value.ToString(), out isDirectory);

                                if (isDirectory)
                                {
                                    directories.Add(fileInfo);
                                }
                                else
                                {
                                    newFiles.Add(fileInfo);
                                }
                            }
                            Debug.WriteLine("");
                            retry = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                    MessageBoxResult result = MessageBox.Show("Cosmos Error!  Retry?\r\n\r\n Error: " + e.ToString(),
                                                              "Cosmos Error", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No) retry = false;
                }
            }

            return totalSize;
        }

    }

}
