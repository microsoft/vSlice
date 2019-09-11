using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace VSlice
{

    /// ******************************************************************************
	/// <summary>
	/// Holds information about a RegistryValue or RegistryKey
	/// </summary>
	/// ******************************************************************************
    class RegistryItem : BaseTreeItem
	{
        // ******************************************************************************
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="file"></param>
        // ******************************************************************************
        public RegistryItem(string name, string fullName) : base(name, fullName) { }
    }
}
