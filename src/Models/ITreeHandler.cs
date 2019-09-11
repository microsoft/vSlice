using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Data;

namespace VSlice
{
    /// <summary>
    /// Represents a plugin interface for reading trees
    /// </summary>
    public interface ITreeHandler
    {
        /// <summary>
        /// The seed is the token used to indicate the root of the tree.
        /// This method should throw if the token is invalid
        /// </summary>
        void ValidateSeed(Seed seed);

        /// <summary>
        /// Use this to expose the scan progress to the UI
        /// </summary>
        double UnitsScannedSoFar { get;}

        /// <summary>
        /// WHen the scan is completed, this should be populated
        /// </summary>
        ITreeItem Root { get; }

        /// <summary>
        /// Provides the UI with the name of the current item being scanned
        /// </summary>
        string CurrentItemLabel { get; }

        /// <summary>
        /// Returns a list of default seed values for use with this tree handler. 
        /// e.g.:  Current drive letters for the file system plugin
        /// </summary>
        IEnumerable<Seed> GetSeeds();

        /// <summary>
        /// See if we can handle a drag/dropped item.   If so, then give us back a seed
        /// from which to start a scan.
        /// </summary>
        bool TryHandleDrop(System.Windows.IDataObject dataObject, out Seed seed);

        /// <summary>
        /// See if we can handle a named location manually typed in by the user.   
        /// If so, then give us back a seed from which to start a scan.
        /// </summary>
        bool TryHandleLocation(string locationName, out Seed seed);

        /// <summary>
        /// This is called from the main ui thread so that you can interact with the 
        /// user if needed.  Return false if the scan should not proceed.
        /// </summary>
        bool HandlePreScan(Seed seed);

        /// <summary>
        /// Begin scanning the tree
        /// </summary>
        Task StartScan(Seed seed, Action onCompleted, TreeFilter[] filters);

        /// <summary>
        /// Signals any operating scans by this worker to stop
        /// </summary>
        void CancelScan();

        /// <summary>
        /// Column Names available for displaying values
        /// </summary>
        string[] ValueColumns { get; }

        /// <summary>
        /// Name of column to display for values in the tree
        /// </summary>
        string SizeColumn { get; set; }

        /// <summary>
        /// Name of column to use for heatmap colors
        /// </summary>
        string HeatmapColumn { get; set; }

        /// <summary>
        /// Name of column to display for values in the tree
        /// </summary>
        string PathColumn { get;  }

        /// <summary>
        /// The names of available detail columns to display for this object
        /// </summary>
        string[] DataColumnNames { get; }

        /// <summary>
        /// Get an easty-to-read version of the value
        /// </summary>
        string GetFormattedValue(double value);

        /// <summary>
        /// If a content row is double-clicked
        /// </summary>
        void HandleItemDoubleClick(IItemData item);
    }
}
