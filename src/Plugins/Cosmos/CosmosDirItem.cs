using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace VSlice
{

    /// ******************************************************************************
	/// <summary>
	/// Holds information about a file or a directory
	/// </summary>
	/// ******************************************************************************
    class CosmosDirItem : BaseTreeItem
	{
        // ******************************************************************************
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="file"></param>
        // ******************************************************************************
        public CosmosDirItem(string name, string fullName) : base(name, fullName)
        {
            ClickInstructions = "Ctrl-Click to open folder in IE."; 
        }

        /// ---------------------------------------------------------------------------
        /// <summary>
        /// Shift click is a command window
        /// </summary>
        /// ---------------------------------------------------------------------------
        public override void DoShiftClick()
        {
            DoCtrlClick();
        }

        /// ---------------------------------------------------------------------------
        /// <summary>
        /// Control-Click is explorer
        /// </summary>
        /// ---------------------------------------------------------------------------
        public override void DoCtrlClick()
        {
            ProcessStartInfo newExplorerWindow = new ProcessStartInfo("explorer.exe", FullName + "\\");
            newExplorerWindow.WorkingDirectory = FullName + "\\";
            Process.Start(newExplorerWindow);
        }
    }
}
