using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Windows;
using IWshRuntimeLibrary;
using System.Threading.Tasks;

namespace VSlice
{
    /// <summary>
    /// File System version of a tree handler
    /// </summary>
    public class FileTreeHandler : BaseTreeHandler
    {
        public override ColumnInfo Columns => FileDirectoryInfo.ColumnInfo;

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        /// -----------------------------------------------------------------------------------   
        public FileTreeHandler(PluginSettingsHandler settingsHandler) : base(settingsHandler, FileDirectoryInfo.FILESIZE){}

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Return the list of possible seeds
        /// </summary>
        /// -----------------------------------------------------------------------------------   
        public override IEnumerable<Seed> GetSeeds()
        {
            List<Seed> seeds = new List<Seed>();

            // Get a list of the current logical drives
            foreach (string drive in Directory.GetLogicalDrives())
            {
                seeds.Add(new Seed { Id = drive, Name = "Local Disk Drive: " + drive, TreeHandler = this });
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
            if (!Directory.Exists(seed.Id)) throw new DirectoryNotFoundException("Directory does not exist: " + seed.Id);
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// If the data object can be handled hand back a seed to it.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override bool TryHandleDrop(System.Windows.IDataObject dataObject, out Seed seed)
        {
            string fileName = Utilities.GetFileNameFromDropObject(dataObject);
            return TryHandleLocation(fileName, out seed);
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// If the data object can be handled hand back a seed to it.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override bool TryHandleLocation(string fileName, out Seed seed)
        {
            if (fileName != null && Directory.Exists(fileName))
            {
                seed = new Seed() { Id = fileName, Name = fileName, TreeHandler = this };
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
            return () => ScanFolder(new FileDirectoryInfo(seed.Id), cancelToken);
        }

        // ******************************************************************************
        /// <summary>
        /// Returns a fully populate heirarchy for this forlder
        /// </summary>
        /// <param name="directoryInfo">Object representing the root folder to scan</param>
        /// <returns>Populated Heirarchy</returns>
        // ******************************************************************************
        FileDirItem ScanFolder(IDirectoryInfo directoryInfo, CancellationToken cancelToken)
        {
            CurrentItemLabel = directoryInfo.FullName;
            FileDirItem thisDir = new FileDirItem(directoryInfo.Name, directoryInfo.FullName);
            if (cancelToken.IsCancellationRequested) return thisDir;

            try
            {
                // Total up the size of the content
                foreach (var file in directoryInfo.GetFiles())
                {
                    thisDir.AddContent(file.Name, file);
                    UnitsScannedSoFar += file.GetValue(FileDirectoryInfo.FILESIZE);
                    if (cancelToken.IsCancellationRequested) return thisDir;
                }

                // recurse into the child directories
                foreach (IDirectoryInfo dir in directoryInfo.GetDirectories())
                {
                    FileDirItem newDir = ScanFolder(dir, cancelToken);
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

            return thisDir;
        }
    }
}
