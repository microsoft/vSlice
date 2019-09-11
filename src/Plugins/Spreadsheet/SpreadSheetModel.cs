using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace VSlice
{
    /// -------------------------------------------------------------------------------------
    /// <summary>
    /// Model for dealing with spreadsheet data
    /// </summary>
    /// -------------------------------------------------------------------------------------
    public class SpreadSheetModel : INotifyPropertyChanged
    {
        public string[] ColumnNames { get; private set; }
        private char[] _pathDelimiters = new char[] { '/', '\\', '|' };

        private int _selectedPathColumn = -1;
        public int SelectedPathColumn
        {
            get => _selectedPathColumn;
            set
            {
                _selectedPathColumn = value;
                EvaluatePotentialPathColumn(_selectedPathColumn);
                RaisePropertyChanged(nameof(SelectedPathColumn));
            }
        }

        bool _hasPathError = false;
        public Brush PathStatusColor => _hasPathError ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Transparent);



        public string SelectedPathColumnName => (SelectedPathColumn > -1) ? ColumnNames[SelectedPathColumn] : null;


        public int PathColumnIndex => _selectedPathColumn;

        private int _sizeColumnId = -1;
        public string SizeColumn
        {
            get
            {
                return (_sizeColumnId < 0) ? null : ColumnNames[_sizeColumnId];
            }
            set
            {
                _sizeColumnId = GetColumnIndex(value);
                EvaluatePotentialValueColumn(_sizeColumnId);
                RaisePropertyChanged(nameof(SizeColumn));
            }
        }

        private string _doubleClickTemplate = "https://microsoft.visualstudio.com/_git/os?_a=history&path={{PATH}}&version=GBofficial%2Frsmaster";
        public string DoubleClickTemplate
        {
            get => _doubleClickTemplate;
            set
            {
                _doubleClickTemplate = value;
                RaisePropertyChanged(nameof(DoubleClickTemplate));
            }
        }


        public bool IsReady => SizeColumn != null  && !_hasValueError && !_hasPathError;

        bool _hasValueError = false;
        public Brush ValueStatusColor => _hasValueError ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Transparent);

        public List<string[]> SampleRows { get; private set; }

        public int BATCHSIZE = 20;
        public IEnumerable<string[]> RowBatches
        {
            get
            {
                using (var reader = new StreamReader(_spreadsheetPath))
                {
                    reader.ReadLine();
                    string line;
                    var batch = new string[BATCHSIZE];
                    int pointer = 0;
                    while ((line = reader.ReadLine()) != null)
                    {
                        batch[pointer++] = line;
                        if(pointer >= BATCHSIZE)
                        {
                            yield return batch;
                            batch = new string[BATCHSIZE];
                            pointer = 0;
                        }
                    }
                    yield return batch;
                }
            }
        }

        long _dataStart;
        string _spreadsheetPath;
        /// -------------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        /// -------------------------------------------------------------------------------------
        public SpreadSheetModel(string spreadSheetPath)
        {
            _spreadsheetPath = spreadSheetPath;

            using (var _reader = new StreamReader(spreadSheetPath))
            {
                var topLine = _reader.ReadLine();
                ColumnNames = topLine.Split(new char[] { '\t' });
                _dataStart = topLine.Length + 1;
            }

            RaisePropertyChanged(nameof(ColumnNames));
            RaisePropertyChanged(nameof(SampleRows));
        }

        public string[] TextColumns { get; set; }
        public string[] ValueColumns { get; set; }

        Task _sampleFillTask;

        /// -------------------------------------------------------------------------------------
        /// <summary>
        /// Start filling sample rows
        /// </summary>
        /// -------------------------------------------------------------------------------------
        public void FillSampleRows()
        {
            string line;
            _sampleFillTask = Task.Run(() =>
            {
                var sampleRows = new List<string[]>();
                using (var reader = new StreamReader(_spreadsheetPath))
                {
                    reader.ReadLine();
                    while (sampleRows.Count < 100000 && (line = reader.ReadLine()) != null)
                    {
                        sampleRows.Add(line.Split('\t'));
                    }
                }

                var textColumns = new List<int>();
                var valueColumns = new List<int>();
                
                for(int i = 0; i < ColumnNames.Length; i++)
                {
                    bool hasText = false;
                    bool hasNumbers = false;
                    foreach(var row in sampleRows)
                    {
                        if (row.Length < ColumnNames.Length) continue;
                        if(!string.IsNullOrEmpty(row[i]))
                        {
                            if(!double.TryParse(row[i], out var val))
                            {
                                hasText = true;
                                break;
                            }
                            else
                            {
                                hasNumbers = true;
                            }
                        }
                    }
                    if(hasText || !hasNumbers)
                    {
                        textColumns.Add(i);
                    }
                    else
                    {
                        valueColumns.Add(i);
                    }
                }

                TextColumns = textColumns.Select(i => ColumnNames[i]).ToArray();
                ValueColumns = valueColumns.Select(i => ColumnNames[i]).ToArray();
                RaisePropertyChanged(nameof(TextColumns));
                RaisePropertyChanged(nameof(ValueColumns));

                SampleRows = sampleRows.Take(20).ToList();
                RaisePropertyChanged(nameof(SampleRows));
            });
        }

        /// -------------------------------------------------------------------------------------
        /// <summary>
        /// See if a column can be a path column.  All sample paths must be unique
        /// </summary>
        /// -------------------------------------------------------------------------------------
        internal void EvaluatePotentialPathColumn(int columnId)
        {
            _hasPathError = false;
            if (SampleRows == null) return;

            var paths = new HashSet<string>();
            foreach (var row in SampleRows)
            {
                if(paths.Contains(row[columnId]))
                {
                    _hasPathError = true;
                    break;
                }
                paths.Add(row[columnId]);
            }

            RaisePropertyChanged(nameof(IsReady));
            RaisePropertyChanged(nameof(PathStatusColor));
        }

        /// -------------------------------------------------------------------------------------
        /// <summary>
        /// See if a column cn be a value column
        /// </summary>
        /// -------------------------------------------------------------------------------------
        internal void EvaluatePotentialValueColumn(int columnId)
        {
            _hasValueError = false;
            if (SampleRows == null) return;

            foreach (var row in SampleRows)
            {
                if (!string.IsNullOrEmpty(row[columnId]))
                {
                    if (!double.TryParse(row[columnId], out var dvalue))
                    {
                        _hasValueError = true;
                        break;
                    }
                }
            }
                
            RaisePropertyChanged(nameof(IsReady));
            RaisePropertyChanged(nameof(ValueStatusColor));
        }

        /// -------------------------------------------------------------------------------------
        /// <summary>
        /// Get the index for the specified column name
        /// </summary>
        /// -------------------------------------------------------------------------------------
        public int GetColumnIndex(string columnName)
        {
            var columnId = 0;
            for (int i = 0; i < ColumnNames.Length; i++)
            {
                if (ColumnNames[i] == columnName)
                {
                    columnId = i;
                    break;
                }
            }

            return columnId;
        }

        /// -------------------------------------------------------------------------------------
        /// <summary>
        /// Open a url based on the path when an item is double-clicked.
        /// </summary>
        /// -------------------------------------------------------------------------------------
        internal void HandleItemDoubleClick(string path)
        {
            if (DoubleClickTemplate == null) return;
            var url = DoubleClickTemplate.Replace("{{PATH}}", WebUtility.UrlEncode(path));
            Process.Start(url);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// -------------------------------------------------------------------------------------
        /// <summary>
        /// RaisePropertyChanged
        /// </summary>
        /// -------------------------------------------------------------------------------------
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// -------------------------------------------------------------------------------------
        /// <summary>
        /// Get Path Parts
        /// </summary>
        /// -------------------------------------------------------------------------------------
        internal string[] GetPathParts(string[] item)
        {
            var path = item[PathColumnIndex];
            if (path == "") path = "(root)";
            if(!_pathDelimiters.Contains(path[0]))
            {
                path = "(root)/" + path;
            }
            return path.Split(_pathDelimiters);
        }
    }
}
