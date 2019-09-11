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
    class SpreadsheetDirItem : BaseTreeItem
    {
        public override string FullName
        {
            get => GetFullNameFromParent().ToString();
            set { }
        }

        // ******************************************************************************
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="file"></param>
        // ******************************************************************************
        public SpreadsheetDirItem(string name) : base(name, null) { }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Walk the parent tree to get the full name of this folder
        /// </summary>
        /// -----------------------------------------------------------------------------------
        protected StringBuilder GetFullNameFromParent(StringBuilder nameBuilder = null)
        {
            if(nameBuilder == null)
            {
                nameBuilder = new StringBuilder();
            }
            if(Parent != null)
            { 
                ((SpreadsheetDirItem)Parent)
                    .GetFullNameFromParent(nameBuilder)
                    .Append('/');
            }
            nameBuilder.Append(Name);
            return nameBuilder;
        }

        Dictionary<string, SpreadsheetDirItem> _childLookup = new Dictionary<string, SpreadsheetDirItem>();
        // ******************************************************************************
        /// <summary>
        /// Add a new item from the spreadsheet to this directory
        /// </summary>
        // ******************************************************************************
        internal void AddItem(string[] pathParts, IItemData item)
        {
            var addToMe = this;

            if(pathParts[0] != this.Name)
            {
                throw new ApplicationException("This path has an incorrect root: " + item.FullName);
            }

            for (int i = 1; i < pathParts.Length - 1; i++)
            {
                var thisDirName = pathParts[i];

                if (!addToMe._childLookup.TryGetValue(thisDirName, out var nextDir))
                {
                    nextDir = new SpreadsheetDirItem(thisDirName);
                    addToMe.AddChild(nextDir);
                    addToMe._childLookup.Add(thisDirName, nextDir);
                }

                addToMe = nextDir;
            }

            addToMe.AddContent(item.Name, item);
        }
    }
}
