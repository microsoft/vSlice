using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VSlice
{
    /// <summary>
    /// Spreadsheet version of a tree handler where one column has a path
    /// </summary>
    public class SpreadsheetTreeHandler : BaseTreeHandler
    {
        ColumnInfo _columnInfo;
        public override ColumnInfo Columns => _columnInfo;

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        /// -----------------------------------------------------------------------------------   
        public SpreadsheetTreeHandler(PluginSettingsHandler settingsHandler) : base(settingsHandler, StandardColumns.COUNT) { }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Return the list of possible heirarchy seeds
        /// </summary>
        /// -----------------------------------------------------------------------------------   
        public override IEnumerable<Seed> GetSeeds()
        {
            var seeds = new List<Seed>();

            seeds.Add(new Seed() { Id = "Spreadsheet", Name = "Spreadsheet", TreeHandler = this });
            return seeds;
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// The seed is the data item that points to the top of the tree.  Make sure 
        /// this is a valid directory.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override void ValidateSeed(Seed seed)
        {
            if (seed.Name == "Spreadsheet")
            {
                throw new ApplicationException("To scan a spreadsheet, drag and drop a .tsv file into vSlice.  It should have at least one column with path information.");
            }
        }

        private SpreadSheetModel _spreadSheetModel;

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// If the data object can be handled hand back a seed to it.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override bool TryHandleDrop(System.Windows.IDataObject dataObject, out Seed seed)
        {
            return TryHandleLocation(Utilities.GetFileNameFromDropObject(dataObject), out seed);
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// If the data object can be handled hand back a seed to it.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override bool TryHandleLocation(string fileName, out Seed seed)
        {
            if (fileName != null && System.IO.File.Exists(fileName) && fileName.ToLower().EndsWith(".tsv"))
            {
                seed = new Seed() { Id = fileName, Name = fileName, TreeHandler = this };
                return true;
            }
            seed = null;
            return false;
        }

        class SpreadsheetSettings
        {
            public string PathColumn { get; set; }
            public string SizeColumn { get; set; }
            public string DoubleClickTemplate { get; set; }
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Get spreadsheet details from the user
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override bool HandlePreScan(Seed seed)
        {
            _spreadSheetModel = new SpreadSheetModel(seed.Id);

            var chooser = new ColumnChooser(_spreadSheetModel);
            var settings = SettingsHandler.GetSettings<SpreadsheetSettings>("SpreadSheetHandler");
            if (settings == null) settings = new SpreadsheetSettings();
            if (_spreadSheetModel.ColumnNames.Contains(settings.PathColumn))
            {
                _spreadSheetModel.SelectedPathColumn = _spreadSheetModel.GetColumnIndex(settings.PathColumn);
            }
            if (settings.SizeColumn != null)
            {
                _spreadSheetModel.SizeColumn = settings.SizeColumn;
            }
            if (settings.DoubleClickTemplate != null)
            {
                _spreadSheetModel.DoubleClickTemplate = settings.DoubleClickTemplate;
            }

            if ( chooser.ShowDialog().Value)
            {
                var pathColumn = _spreadSheetModel.ColumnNames[_spreadSheetModel.SelectedPathColumn];
                _columnInfo = ColumnInfo.CreateFromFixedColumns(_spreadSheetModel.ColumnNames, _spreadSheetModel.ValueColumns, pathColumn);
                SizeColumn = _spreadSheetModel.SizeColumn;

                settings.PathColumn = pathColumn;
                settings.SizeColumn = _spreadSheetModel.SizeColumn;
                settings.DoubleClickTemplate = _spreadSheetModel.DoubleClickTemplate;
                SettingsHandler.SaveSettings("SpreadSheetHandler", settings);
                return true;
            }
            return false;
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Set up a recursive directory search to scan the tree information
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override Func<ITreeItem> GetCancelableScanFunction(Seed seed, CancellationToken cancelToken)
        {
            return () => ScanFolder(cancelToken);
        }

        // ******************************************************************************
        /// <summary>
        /// Returns a fully populate heirarchy for this forlder
        /// </summary>
        // ******************************************************************************
        SpreadsheetDirItem ScanFolder(CancellationToken cancelToken)
        {
            var head = new SpreadsheetDirItem("(root)");

            var processedRows = ProcessRows(_spreadSheetModel.RowBatches, cancelToken);

            foreach(var rowItem in processedRows)
            {
                if (rowItem == null) continue;
                head.AddItem(rowItem.PathParts, rowItem);
            }

            return head;
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Process rows in parallel
        /// </summary>
        /// -----------------------------------------------------------------------------------
        private List<ColumnarItemData> ProcessRows(IEnumerable<string[]>  rowProvider, CancellationToken cancelToken )
        {
            
            var results = new ConcurrentBag<ColumnarItemData[]>();
            var options = new ParallelOptions();
            options.CancellationToken = cancelToken;
            var minSize = _spreadSheetModel.ColumnNames.Length;
            Parallel.ForEach(rowProvider, options, batch =>
            {
                var work = new ColumnarItemData[batch.Length];
                for(int i = 0; i < work.Length; i++)
                {
                    var rawRow = batch[i];
                    if (rawRow == null) break;
                    var splitRow = rawRow.Split('\t');
                    if (splitRow.Length < minSize)
                    {
                        continue;
                    }

                    var pathParts = _spreadSheetModel.GetPathParts(splitRow);

                    work[i] = new ColumnarItemData(pathParts, _columnInfo.ColumnLookup, splitRow);
                }
                results.Add(work);
                UnitsScannedSoFar += work.Length;
            });

            var output = new List<ColumnarItemData>();
            foreach(var batch in results)
            {
                output.AddRange(batch);
            }
            return output;
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// override this if you want to handle double-clicked items
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public override void HandleItemDoubleClick(IItemData item)
        {
            _spreadSheetModel.HandleItemDoubleClick(item.FullName.Replace("(root)/",""));
        }

    }
}
