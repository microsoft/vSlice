using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace VSlice
{
    /// <summary>
    /// File System version of a tree handler
    /// </summary>
    public class RegistryTreeHandler : BaseTreeHandler
    {
        public override ColumnInfo Columns => RegistryDirectoryInfo.Columns;

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public RegistryTreeHandler(PluginSettingsHandler settingsHandler) : base(settingsHandler, RegistryDirectoryInfo.ITEMSIZE) { }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Return the list of possible seeds
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override IEnumerable<Seed> GetSeeds()
        {
            List<Seed> seeds = new List<Seed>();

            // Get a list of the current logical drives
            foreach (RegistryHive hive in RegistryHive.GetValues(typeof(RegistryHive)))
            {
                switch (hive.ToString())
                {
                    //We don't need to add the 2 deprecated HIVES of dynData & Performance.
                    case "DynData":
                    case "PerformanceData":
                        break;
                    default:
                        seeds.Add(new Seed { Id = hive.ToString(), Name = "Registry Hive: " + hive.ToString(), TreeHandler = this });
                        break;
                }
            }
            return seeds;
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// The seed is the data item that points to the top of the tree.  Since the registry
        /// roots are well known and accessed using the microsoft.win32.Registry object we
        /// don't need to verify that it exists, but we could check to see if we have read access.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override void ValidateSeed(Seed seed)
        {
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// regedit doesn't support draging registrykeys, but if there is something else that
        /// could drop a registryKey then we would want to handle the drop event.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override bool TryHandleLocation(string dataObject, out Seed seed)
        {
            // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft SQL Server 2005 Redist
            if(dataObject.ToLower().StartsWith("hkey"))
            {
                seed = new Seed(){Id = dataObject, Name = dataObject, TreeHandler = this};
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
            return () => ScanRegistry(new RegistryDirectoryInfo(seed.Id), cancelToken);
        }

        // ******************************************************************************
        /// <summary>
        /// Returns a fully populate heirarchy for this key
        /// </summary>
        /// <param name="directoryInfo">Object representing the root key to scan</param>
        /// <returns>Populated Heirarchy</returns>
        // ******************************************************************************
        RegistryItem ScanRegistry(IDirectoryInfo registryInfo, CancellationToken cancelToken)
        {
            CurrentItemLabel = registryInfo.FullName;
            RegistryItem thisKey = new RegistryItem(registryInfo.Name, registryInfo.FullName);
            if (cancelToken.IsCancellationRequested) return thisKey;

            try
            {
                //Total up the size of the content
                foreach (var registryItem in registryInfo.GetFiles())
                {
                    
                    thisKey.AddContent(registryItem.Name, registryItem);
                    UnitsScannedSoFar += registryItem.GetValue(RegistryDirectoryInfo.ITEMSIZE);
                    if (cancelToken.IsCancellationRequested) return thisKey;
                }

                // recurse into the sub keys
                foreach (IDirectoryInfo key in registryInfo.GetDirectories())
                {
                    RegistryItem newKey = ScanRegistry(key, cancelToken);
                    thisKey.AddChild(newKey);
                }
            }
            //Ignore permission problems, since if a standare user runs the tool then there 
            // will be KEYS that they dont't have access to but we want to skip pass and process
            // the rest of the hive anyways.
            catch (SecurityException) { }
            catch (Exception e)
            {
#if DEBUG
                Debugger.Break();
#endif
                Debug.WriteLine("Error: " + e.Message);

            }

            return thisKey;
        }
    }
}
