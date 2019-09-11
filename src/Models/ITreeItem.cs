using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VSlice
{
    /// <summary>
    /// Represents a node in a tree.  The value of each node represents the summation of
    /// all the sub nodes.
    /// </summary>
    public interface ITreeItem : IComparable<ITreeItem>
    {
        string Name { get; }
        string FullName { get; }
        double ContentValue { get; }
        double TotalValue { get; }
        double HeatmapContentValue { get; }
        double HeatmapTotalValue { get; }
        int? HeatmapBucket { get;  }

        int TotalItemCount { get; }
        int LocalItemCount { get; }
        ITreeItem Parent { get; set; }
        List<ITreeItem> Children { get; }

        void AddContent(string name, IItemData data);
        void AddChild(ITreeItem child);
        Dictionary<string, IItemData> Content { get; }

        void DoShiftClick();
        void DoCtrlClick();
        void DoAltClick();
        string ClickInstructions { get; }

        void Recalculate(string valueColumnName, string heatmapColumnName, TreeFilter[] filters);
        string[] GetCommonValues(string value);
    }
}
