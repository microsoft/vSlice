using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;
using Microsoft.BI.Cosmos.IO;
using System.Threading.Tasks;

namespace VSlice
{
    /// <summary>
    /// Cosmos tree handler
    /// </summary>
    public class CosmosTreeHandler : BaseTreeHandler
    {
        public override ColumnInfo Columns => CosmosDirectoryInfo.Columns;

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        /// -----------------------------------------------------------------------------------   
        public CosmosTreeHandler(PluginSettingsHandler settingsHandler) : base(settingsHandler, CosmosDirectoryInfo.STREAMSIZE) { }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Return the list of possible seeds
        /// </summary>
        /// -----------------------------------------------------------------------------------   
        public override IEnumerable<Seed> GetSeeds()
        {
            List<Seed> seeds = new List<Seed>();

            seeds.Add(new Seed() {Id = "Cosmos", Name = "Cosmos", TreeHandler = this});
            return seeds;
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// The seed is the data item that points to the top of the tree.  Make sure 
        /// this is a valid directory.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override void ValidateSeed(Seed seed)
        {
            if(seed.Id == "Cosmos") return;

            //if (!CosmosDirectory.Exists(seed.Id)) throw new DirectoryNotFoundException("Cosmos Directory does not exist: " + seed.Id);

        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// If the data object can be handled hand back a seed to it.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override bool TryHandleDrop(System.Windows.IDataObject dataObject, out Seed seed)
        {
            MemoryStream urlStream = (MemoryStream)dataObject.GetData("UniformResourceLocatorW");
            string url = null;
            if (urlStream != null)
            {
                byte[] buffer = new byte[urlStream.Length];
                urlStream.Read(buffer, 0, (int)urlStream.Length);
                url = System.Text.Encoding.Unicode.GetString(buffer);
            }

            if(url == null)
            {
                url = Utilities.GetFileNameFromDropObject(dataObject);
            }

            return TryHandleLocation(url, out seed);
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// If the data object can be handled hand back a seed to it.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override bool TryHandleLocation(string url, out Seed seed)
        {
            if (url != null && url.ToLower().Contains("cosmos"))
            {
                if (url.Contains("/Legacy/"))
                {
                    url = Regex.Replace(url, @"Legacy\/.*?\/", "cosmos/");
                }
                
                url = url.TrimEnd('\0');

                if(!url.EndsWith("/"))
                {
                    url = url + "/";
                }

                seed = new Seed() { Id = url, Name = url, TreeHandler = this };
                return true;
            }
            seed = null;
            return false;
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Set up a recursive directory search to scan the tree information
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override Func<ITreeItem> GetCancelableScanFunction(Seed seed, CancellationToken cancelToken)
        {
            return () => ScanFolder(new CosmosDirectoryInfo(seed.Id), cancelToken);
        }

        // ******************************************************************************
        /// <summary>
        /// Returns a fully populate heirarchy for this forlder
        /// </summary>
        /// <param name="directoryInfo">Object representing the root folder to scan</param>
        /// <returns>Populated Heirarchy</returns>
        // ******************************************************************************
        CosmosDirItem ScanFolder(IDirectoryInfo directoryInfo, CancellationToken cancelToken)
        {
            CurrentItemLabel = directoryInfo.FullName;
            CosmosDirectoryInfo cosmosDirectoryInfo = (CosmosDirectoryInfo) directoryInfo;
            CosmosDirItem thisDir = new CosmosDirItem(directoryInfo.Name, directoryInfo.FullName);
            //thisDir.TotalValue = Math.Max(0, cosmosDirectoryInfo.CosmosSize);
            if (cancelToken.IsCancellationRequested) return thisDir;
            double totalFolderSize = 0;

            try
            {
                // Total up the size of the content
                foreach (var file in directoryInfo.GetFiles())
                {
                    thisDir.AddContent(file.Name, file);
                    var fileSize = Math.Max(0, file.GetValue(CosmosDirectoryInfo.STREAMSIZE));
                    UnitsScannedSoFar += fileSize;
                    totalFolderSize += fileSize;
                    file.SetValue(CosmosDirectoryInfo.STREAMSIZE, fileSize);
                    if (cancelToken.IsCancellationRequested) return thisDir;
                }

                // recurse into the child directories
                foreach (IDirectoryInfo dir in directoryInfo.GetDirectories())
                {
                    CosmosDirItem newDir = ScanFolder(dir, cancelToken);
                    thisDir.AddChild(newDir);
                }
            }
            catch (UnauthorizedAccessException) { }  // Ignore permissions problems
            catch (Exception e)
            {
#if DEBUG
                Debugger.Break();
#endif
                Debug.WriteLine("Error: " + e.Message);
            }

            //thisDir.TotalValue = totalFolderSize;

            return thisDir;
        }

    }
}
