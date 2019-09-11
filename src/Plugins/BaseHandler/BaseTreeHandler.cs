using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace VSlice
{
    public static class StandardColumns
    {
        public const string COUNT = "Item_Count";
        public const string PATH = "__Path";
        public const string PATHDEPTH = "__PathDepth";
        public const string NAME = "__Name";
        public const string TAG = "_TAG";
    }

    public abstract class BaseTreeHandler : ITreeHandler
    {

        /// <summary>
        /// Set this during the scan to indicate how far we've gone
        /// </summary>
        public double UnitsScannedSoFar { get; protected set; }


        /// <summary>
        /// Set this during the scan to indicate what item we just scanned
        /// </summary>
        public string CurrentItemLabel { get; protected set; }

        /// <summary>
        /// Columns available for showing values
        /// </summary>
        public string[] ValueColumns
        {
            get
            {
                return Columns?.ValueColumns;
            }
        }

        /// <summary>
        /// Columns available for showing values
        /// </summary>
        public string[] DataColumnNames
        {
            get
            {
                return Columns?.AllColumns;
            }
        }

        /// <summary>
        /// Access to column information for the plugin
        /// </summary>
        public abstract ColumnInfo Columns { get; }


        /// <summary>
        /// Selected value column to display for tree size data
        /// </summary>
        public virtual string SizeColumn { get; set; }

        /// <summary>
        /// Selected column to use for heatmap colors
        /// </summary>
        public virtual string HeatmapColumn { get; set; }

        /// <summary>
        /// Selected value column to display for tree size data
        /// </summary>
        public virtual string PathColumn => Columns.PathColumn;

        /// <summary>
        /// WHen the scan is completed, this should be populated
        /// </summary>
        public ITreeItem Root { get; protected set; }

        private CancellationTokenSource _cancelTokenSource;
        private Task _workerTask;
        protected PluginSettingsHandler SettingsHandler { get; set; }
        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public BaseTreeHandler(PluginSettingsHandler settingsHandler, string defaultSizeColumn)
        {
            SizeColumn = defaultSizeColumn;
            SettingsHandler = settingsHandler;
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// See if we can handle a drag/dropped item.   If so, then give us back a seed
        /// from which to start a scan.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public virtual bool TryHandleDrop(IDataObject dataObject, out Seed seed)
        {
            seed = null;
            return false;
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// See if we can handle a named location.   If so, then give us back a seed
        /// from which to start a scan.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public virtual bool TryHandleLocation(string locationName, out Seed seed)
        {
            seed = null;
            return false;
        }

        double unitDivisor = 1000000.0;
        string unitLetter = "M";
        double _lastRootCheck = -1;
        /// ---------------------------------------------------------------------------
        /// <summary>
        /// Format the size to a string
        /// </summary>
        /// ---------------------------------------------------------------------------
        public virtual string GetFormattedValue(double value)
        {
            if(Root.TotalValue != _lastRootCheck)
            {
                RecalcuateFormatting();
                _lastRootCheck = Root.TotalValue;
            }
            double scaledValue = value / unitDivisor;
            if (scaledValue > 100) return scaledValue.ToString("0,0 ") + unitLetter;
            if (scaledValue > .1) return scaledValue.ToString(".0 ") + unitLetter;
            if (scaledValue == 0) return "0 " + unitLetter;
            return scaledValue.ToString("0.000 ") + unitLetter;
        }

        /// ---------------------------------------------------------------------------
        /// <summary>
        /// Refigure how to format the size value
        /// </summary>
        /// ---------------------------------------------------------------------------
        public void RecalcuateFormatting()
        {
            if (Root != null)
            {
                var microSize = Root.TotalValue / 1000;
                if (microSize > 100000000)
                {
                    unitDivisor = 1000000000.0;
                    unitLetter = "G";
                }
                else if (microSize > 100000)
                {
                    unitDivisor = 1000000.0;
                    unitLetter = "M";
                }
                else if (microSize > 100)
                {
                    unitDivisor = 1000.0;
                    unitLetter = "K";
                }
                else
                {
                    unitDivisor = 1;
                    unitLetter = "";
                }
            }
        }




        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Returns a list of default seed values for use with this tree handler. 
        /// e.g.:  Current drive letters for the file system plugin
        /// There should be at least one seed so that the scanner appears in the 
        /// initial drop-down.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public abstract IEnumerable<Seed> GetSeeds();

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Throw an exception if we can't handle this seed
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public abstract void ValidateSeed(Seed seed);

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// This is called from the main ui thread so that you can interact with the 
        /// user if needed.  Return false if the scan should not proceed.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public virtual bool HandlePreScan(Seed seed) => true;

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Start a new scan if one is not running
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public Task StartScan(Seed seed, Action onCompleted, TreeFilter[] filters)
        {
            UnitsScannedSoFar = 0;
            if(_workerTask != null && !_workerTask.IsCompleted)
            {
                throw new ApplicationException("Error: a current scan task is active.");
            }
            _cancelTokenSource = new CancellationTokenSource();
            var getTree = GetCancelableScanFunction(seed, _cancelTokenSource.Token);
            _workerTask = new Task(() =>
            {
                Root = getTree();
                Root.Recalculate(SizeColumn, null, filters);
                onCompleted();
            });

            _workerTask.Start();
            return _workerTask;
        }


        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Return a function that can produce the tree. 
        /// 
        /// The function should:
        ///     - Respond to cancelation requests through the token provided
        ///     - population UnitsScannedSoFar and CurrentItemLabel to indicate progress
        ///     - Return the root of the tree when finished
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public abstract Func<ITreeItem> GetCancelableScanFunction(Seed seed, CancellationToken cancelToken);

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Cancel any currently running scan
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public virtual void CancelScan()
        {
            _cancelTokenSource.Cancel();
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// override this if you want to handle double-clicked items
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public virtual void HandleItemDoubleClick(IItemData item)
        {
        }
    }
}
