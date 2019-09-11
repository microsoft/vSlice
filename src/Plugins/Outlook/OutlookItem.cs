using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace VSlice
{
    class OutlookItem: BaseTreeItem
    {
        /// ---------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        /// ---------------------------------------------------------------------------
        public OutlookItem(string name, string fullName) : base(name, fullName) { }

        /// ---------------------------------------------------------------------------
        /// <summary>
        /// In outlook, some items have the same name, so we have to have a 
        /// special AddContent Method
        /// </summary>
        /// ---------------------------------------------------------------------------
        public override void AddContent(string name, IItemData data)
        {
            if (name == null) name = "** UNKNOWN ITEM **";
            var decoratedName = name;
            int count = 1;
            while (Content.ContainsKey(decoratedName))
            {
                decoratedName = name + "_" + count.ToString("00000");
                count++;
            }
            base.AddContent(decoratedName, data);

        }
    }
}
