using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VSlice
{
    public interface IItemData
    {
        /// <summary>
        /// Short name of this item
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Full path name 
        /// </summary>
        string FullName { get;  }

        /// <summary>
        /// Get a numeric value from this item
        /// </summary>
        double GetValue(string columnName);

        /// <summary>
        /// Sat a numeric value from this item
        /// </summary>
        void SetValue(string columnName, object newValue);

        /// <summary>
        /// Get a text value from this item
        /// </summary>
        string GetText(string columnName);
    }

    /// -----------------------------------------------------------------------------------
    /// <summary>
    /// Columnar Item data handling.  Fast and compact
    /// </summary>
    /// -----------------------------------------------------------------------------------
    public class ColumnarItemData : IItemData
    {
        public string Name => PathParts[PathParts.Length - 1];
        public string FullName => string.Join("/", PathParts);

        public string[] DisplayData { get; set; }
        public string[] PathParts { get; internal set; }

        private Dictionary<string, int> _columnLookup;
        public ColumnarItemData(string[ ] pathParts, Dictionary<string, int> columnLookup, string[] rowData = null)
        {
            PathParts = pathParts;
            _columnLookup = columnLookup;
            DisplayData = rowData ?? new string[columnLookup.Count];
        }

        public double GetValue(string columnName)
        {
            var datum = GetText(columnName);
            if (string.IsNullOrEmpty(datum) || datum == "0") return 0;
            if(double.TryParse(datum, out var returnMe))
            {
                return Math.Abs(returnMe);
            }
            return 0;
        }

        public string GetText(string columnName)
        {
            switch(columnName)
            {
                case StandardColumns.COUNT: return "1";
                case StandardColumns.NAME: return Name;
                case StandardColumns.PATH: return FullName;
                case StandardColumns.PATHDEPTH: return (PathParts.Length-2).ToString();
                case null: return null;
            }

            if (!_columnLookup.TryGetValue(columnName, out var columnIndex))
            {
                return null;
            }

            return DisplayData[columnIndex];
        }

        public void SetValue(string columnName, object newValue)
        {
            DisplayData[_columnLookup[columnName]] = newValue.ToString();
        }
    }

}
