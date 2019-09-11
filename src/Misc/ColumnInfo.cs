using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSlice
{
    public class ColumnInfo
    {
        public Dictionary<string, int> ColumnLookup { get; private set; } = new Dictionary<string, int>();

        public string[] AllColumns { get; private set; }
        public string[ ] ValueColumns { get; private set; }

        public string PathColumn { get; private set; }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public ColumnInfo(string[] additionalValueColumns, string[] additionalNameColumns)
        {
            PathColumn = StandardColumns.PATH;
            var fullColumnList = new List<string>();
            fullColumnList.Add(PathColumn);
            fullColumnList.Add(StandardColumns.NAME);
            fullColumnList.Add(StandardColumns.COUNT);
            fullColumnList.Add(StandardColumns.PATHDEPTH);

            var allValueColumns = new List<string>
            {
                StandardColumns.COUNT,
                StandardColumns.PATHDEPTH
            };
            ColumnLookup.Add(PathColumn, ColumnLookup.Count);
            
            if (additionalValueColumns != null)
            {
                fullColumnList.AddRange(additionalValueColumns);
                allValueColumns.AddRange(additionalValueColumns);
                foreach (var name in additionalValueColumns)
                {
                    ColumnLookup.Add(name, ColumnLookup.Count);
                }
            }

            if (additionalNameColumns != null)
            {
                fullColumnList.AddRange(additionalNameColumns);
                foreach (var name in additionalNameColumns)
                {
                    ColumnLookup.Add(name, ColumnLookup.Count);
                }
            }

            AllColumns = fullColumnList.ToArray();
            ValueColumns = allValueColumns.ToArray();
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        /// -----------------------------------------------------------------------------------
        private ColumnInfo() { }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// The spreadsheet can't adjust column order, so we need this to all for a custom
        /// column info arrangement
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public static ColumnInfo CreateFromFixedColumns(string[] allColumnsOrdered, string[] valueColumns, string pathColumn)
        {
            var newColumnInfo = new ColumnInfo();
            newColumnInfo.PathColumn = pathColumn;
            foreach(var item in allColumnsOrdered)
            {
                newColumnInfo.ColumnLookup.Add(item, newColumnInfo.ColumnLookup.Count);
            }
            newColumnInfo.ColumnLookup[StandardColumns.PATH] = newColumnInfo.ColumnLookup[pathColumn];

            // Set up value columns
            var allValueColumns = new List<string>
            {
                StandardColumns.COUNT,
                StandardColumns.PATHDEPTH
            };
            allValueColumns.AddRange(valueColumns);
            newColumnInfo.ValueColumns = allValueColumns.ToArray();

            // Set up all columns
            var allColumns = new List<string> { StandardColumns.NAME, StandardColumns.COUNT, StandardColumns.PATHDEPTH };
            allColumns.AddRange(allColumnsOrdered);
            newColumnInfo.AllColumns = allColumns.ToArray();

            return newColumnInfo;
        }
    }
}
