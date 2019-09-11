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
    class FileDirItem : BaseTreeItem
	{
        // ******************************************************************************
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="file"></param>
        // ******************************************************************************
        public FileDirItem(string name, string fullName) : base(name, fullName)
        {
            ClickInstructions = "Shift-Click for cmd window, Ctrl-Click for Explorer"; 
        }

        /// ---------------------------------------------------------------------------
        /// <summary>
        /// Shift click is a command window
        /// </summary>
        /// ---------------------------------------------------------------------------
        public override void DoShiftClick()
        {
            ProcessStartInfo newConsoleWindow = new ProcessStartInfo("cmd.exe", "/K TITLE " + FullName);
            newConsoleWindow.WorkingDirectory = FullName;
            Process.Start(newConsoleWindow);
        }

        /// ---------------------------------------------------------------------------
        /// <summary>
        /// Control-Click is explorer
        /// </summary>
        /// ---------------------------------------------------------------------------
        public override void DoCtrlClick()
        {
            var startExplorer = new ProcessStartInfo("explorer", FullName);
            startExplorer.WorkingDirectory = FullName;
            Process.Start(startExplorer);
        }
    }
}
