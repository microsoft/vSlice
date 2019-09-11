using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Outlook = Microsoft.Office.Interop.Outlook; 
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace VSlice
{
    public class OutlookTreeHandler : BaseTreeHandler
    {
        public override ColumnInfo Columns => OutlookDirectoryInfo.Columns;

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Constructor (create outlook objects)
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public OutlookTreeHandler(PluginSettingsHandler settingsHandler) : base(settingsHandler, OutlookDirectoryInfo.ITEMSIZE)
        {
            OutlookDirectoryInfo.Init();
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Return the list of possible seeds
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override IEnumerable<Seed> GetSeeds()
        {
            List<Seed> seeds = new List<Seed>();

            // Get a list of the stores on the local machine
            Dictionary<string, string> storeIdsAndNames = OutlookDirectoryInfo.GetStoreIdsAndNames();
            if (storeIdsAndNames == null)
            {
                seeds.Add(new Seed { Id = null, Name = "[Outlook Unavailable]", TreeHandler = this });
            }
            else
            {
                foreach (string key in storeIdsAndNames.Keys)
                {
                    seeds.Add(new Seed { Id = key, Name = "Outlook Store: " + storeIdsAndNames[key], TreeHandler = this });
                }
            }

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
            OutlookDirectoryInfo info = new OutlookDirectoryInfo(seed.Id);
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// handle pst files
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override bool TryHandleDrop(System.Windows.IDataObject dataObject, out Seed seed)
        {
            string[] fileNames = (string[])dataObject.GetData("FileNameW");
            string fileName = null;
            if (fileNames != null) fileName = fileNames[0];
            return TryHandleLocation(fileName, out seed);
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Handle pst files
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override bool TryHandleLocation(string fileName, out Seed seed)
        {
            if (fileName != null &&
                File.Exists(fileName) &&
                Path.GetExtension(fileName).ToLower() == ".pst")
            {
                // TODO: How do we handle pst files???
                //seed = new Seed() { Id = fileName, Name = fileName, TreeHandler = this };
                //return true;
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
            return () => ScanFolder(new OutlookDirectoryInfo(seed.Id), cancelToken);
        }

        // ******************************************************************************
        /// <summary>
        /// Returns a fully populate heirarchy for this forlder
        /// </summary>
        /// <param name="directoryInfo">Object representing the root folder to scan</param>
        /// <returns>Populated Heirarchy</returns>
        // ******************************************************************************
        OutlookItem ScanFolder(IDirectoryInfo directoryInfo, CancellationToken cancelToken)
        {
            CurrentItemLabel = directoryInfo.FullName;
            var thisDir = new OutlookItem(directoryInfo.Name, directoryInfo.FullName);
            if (cancelToken.IsCancellationRequested) return thisDir;

            try
            {
                // Total up the size of the content
                foreach (var file in directoryInfo.GetFiles())
                {
                    thisDir.AddContent(file.Name, file);
                    UnitsScannedSoFar += file.GetValue(OutlookDirectoryInfo.ITEMSIZE);
                    if (cancelToken.IsCancellationRequested) return thisDir;
                }

                // recurse into the child directories
                foreach (IDirectoryInfo dir in directoryInfo.GetDirectories())
                {
                    OutlookItem newDir = ScanFolder(dir, cancelToken);
                    thisDir.AddChild(newDir);
                }
            }
            catch (UnauthorizedAccessException) { }  // Ignore permissions problems
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
#if DEBUG
                Debugger.Break();
#endif
            }

            return thisDir;
        }
    }
}
