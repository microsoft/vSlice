using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace VSlice
{
    public class AppModel : BaseModel
    {
        public event Action OnDataChanged;

        public DataTable ItemDetailTable { get; private set; }

        private ITreeItem _treeRoot;
        public ITreeItem TreeRoot
        {
            get => _treeRoot;
            set
            {
                _treeRoot = value;
                RaisePropertyChanged(nameof(TreeRoot));
            }
        }

        private Seed _selectedSeed;
        public Seed SelectedSeed
        {
            get => _selectedSeed;
            set
            {
                _selectedSeed = value;
                RaisePropertyChanged(nameof(SelectedSeed));
            }
        }

        private bool _readyToScan = true;
        public bool ReadyToScan
        {
            get => _readyToScan;
            set
            {
                _readyToScan = value;
                RaisePropertyChanged(nameof(ReadyToScan));
                RaisePropertyChanged(nameof(HideWhenNotScanning));
                RaisePropertyChanged(nameof(HideWhenScanning));
            }
        }


        string _scanLocation;
        public string Title => $"vSlice  v{Assembly.GetExecutingAssembly().GetName().Version.ToString()} - {_scanLocation}";


        private string[] _suggestedFilterValues = new string[0];
        public string[] SuggestedFilterValues
        {
            get => _suggestedFilterValues;
            set
            {
                _suggestedFilterValues = value;
                RaisePropertyChanged(nameof(SuggestedFilterValues));
            }
        }

        private string _clickInstructions = "";
        public string ClickInstructions
        {
            get => _clickInstructions;
            set
            {
                _clickInstructions = value;
                RaisePropertyChanged(nameof(ClickInstructions));
            }
        }

        private string _manualScanLocation = "";
        public string ManualScanLocation
        {
            get => _manualScanLocation;
            set
            {
                _manualScanLocation = value;
                RaisePropertyChanged(nameof(ManualScanLocation));
                RaisePropertyChanged(nameof(HasManualScanLocation));
            }
        }

        public bool HasManualScanLocation => !string.IsNullOrWhiteSpace(ManualScanLocation);

        private string _detailHeatmapSize = "";
        public string DetailHeatmapSize
        {
            get => _detailHeatmapSize;
            set
            {
                _detailHeatmapSize = value;
                RaisePropertyChanged(nameof(DetailHeatmapSize));
            }
        }

        private string _detailChildHeatmapInfo = "";
        public string DetailChildHeatmapInfo
        {
            get => _detailChildHeatmapInfo;
            set
            {
                _detailChildHeatmapInfo = value;
                RaisePropertyChanged(nameof(DetailChildHeatmapInfo));
            }
        }

        private string _detailSize = "";
        public string DetailSize
        {
            get => _detailSize;
            set
            {
                _detailSize = value;
                RaisePropertyChanged(nameof(DetailSize));
            }
        }

        private Visibility _showContentButtonVisibility = Visibility.Collapsed;
        public Visibility ShowContentButtonVisibility
        {
            get => _showContentButtonVisibility;
            set
            {
                _showContentButtonVisibility = value;
                RaisePropertyChanged(nameof(ShowContentButtonVisibility));
            }
        }

        private string _showCountText = "";
        public string ShowCountText
        {
            get => _showCountText;
            set
            {
                _showCountText = value;
                RaisePropertyChanged(nameof(ShowCountText));
            }
        }

        private string _showContentButtonText = "";
        public string ShowContentButtonText
        {
            get => _showContentButtonText;
            set
            {
                _showContentButtonText = value;
                RaisePropertyChanged(nameof(ShowContentButtonText));
            }
        }

        private string _childContentInfo = "";
        public string ChildContentInfo
        {
            get => _childContentInfo;
            set
            {
                _childContentInfo = value;
                RaisePropertyChanged(nameof(ChildContentInfo));
            }
        }

        private string _detailPath = "";
        public string DetailPath
        {
            get => _detailPath;
            set
            {
                _detailPath = value;
                RaisePropertyChanged(nameof(DetailPath));
            }
        }

        private string _sizeText = "";
        public string HighlightedSizeText
        {
            get => _sizeText;
            set
            {
                _sizeText = value;
                RaisePropertyChanged(nameof(HighlightedSizeText));
            }
        }

        private string _pathText = "";
        public string HighlightedPathText
        {
            get => _pathText;
            set
            {
                _pathText = value;
                RaisePropertyChanged(nameof(HighlightedPathText));
            }
        }

        private string _scanStatusText = "Drag & drop a folder, file, or URL  ... or click one of the scan buttons above";
        public string ScanStatusText
        {
            get => _scanStatusText;
            set
            {
                _scanStatusText = value;
                RaisePropertyChanged(nameof(ScanStatusText));
            }
        }

        private ITreeHandler _currentTreeHandler;
        public ITreeHandler CurrentTreeHandler
        {
            get => _currentTreeHandler;
            set
            {
                _currentTreeHandler = value;
                SelectedValueColumn = _currentTreeHandler?.SizeColumn;

                _currentContentItem = null;
                ShowContent();
                RaisePropertyChanged(nameof(CurrentTreeHandler));
                RaisePropertyChanged(nameof(ValueColumns));
                RaisePropertyChanged(nameof(HeatmapColumns));
            }
        }

        public string[] ValueColumns => CurrentTreeHandler?.ValueColumns;
        public string[] HeatmapColumns => ValueColumns?.Where(c => c != SelectedValueColumn).ToArray();
        public string SelectedValueColumn
        {
            get => CurrentTreeHandler?.SizeColumn;
            set
            {
                if (CurrentTreeHandler == null) return;
                CurrentTreeHandler.SizeColumn = value;
                if (value != null)
                {
                    SafeRecalculate(value, SelectedHeatmapColumn, GetFilters());
                    ShowContent();
                }
                OnDataChanged?.Invoke();
                RaisePropertyChanged(nameof(SelectedValueColumn));
                RaisePropertyChanged(nameof(HeatmapColumns));
            }
        }

        public string SelectedHeatmapColumn
        {
            get => CurrentTreeHandler?.HeatmapColumn;
            set
            {
                if (CurrentTreeHandler == null) return;
                CurrentTreeHandler.HeatmapColumn = value;
                SafeRecalculate(SelectedValueColumn, value, GetFilters());
                ShowContent();
                OnDataChanged?.Invoke();
                RaisePropertyChanged(nameof(SelectedHeatmapColumn));
                RaisePropertyChanged(nameof(RemoveHeatmapVisibility));
            }
        }

        public Visibility RemoveHeatmapVisibility => SelectedHeatmapColumn == null ? Visibility.Hidden : Visibility.Visible;

        public string[] AllColumns => CurrentTreeHandler?.DataColumnNames;
        public string[] FilterOperators => TreeFilter.FilterOperators;
        private TreeFilter _currentFilter = new TreeFilter();

        public string SelectedFilterColumn
        {
            get => _currentFilter?.ColumnName;
            set
            {
                _currentFilter.ColumnName = value;
                _currentFilter.IsValueColumn = ValueColumns.Contains(value);
                RaisePropertyChanged(nameof(SelectedFilterColumn));
                SuggestedFilterValues = CurrentTreeHandler.Root.GetCommonValues(value);
                OnFilterUpdate();
            }
        }

        public string SelectedFilterOperator
        {
            get => _currentFilter?.Operator;
            set
            {
                _currentFilter.Operator = value;
                RaisePropertyChanged(nameof(SelectedFilterOperator));
                OnFilterUpdate();
            }
        }

        public string FilterText
        {
            get => _currentFilter?.FilterText;
            set
            {
                _currentFilter.FilterText = value;
                RaisePropertyChanged(nameof(FilterText));
                RaisePropertyChanged(nameof(FilterTextStatusColor));
                OnFilterUpdate();
            }
        }
        public bool FilterIsCaseSensitive
        {
            get => _currentFilter.IsCaseSensitive;
            set
            {
                _currentFilter.IsCaseSensitive = value;
                RaisePropertyChanged(nameof(FilterIsCaseSensitive));
                OnFilterUpdate();
            }
        }

        public bool FilterIsValid => _currentFilter.IsValid;
        public Brush FilterTextStatusColor => _currentFilter.IsValidText ? Brushes.White : Brushes.Pink;
        public ObservableCollection<TreeFilter> StoredFilters { get; private set; } = new ObservableCollection<TreeFilter>();

        public ObservableCollection<Seed> Seeds { get; private set; } = new ObservableCollection<Seed>();
        
        public Visibility HideWhenNotScanning => ReadyToScan ? Visibility.Hidden : Visibility.Visible;
        public Visibility HideWhenScanning => ReadyToScan ? Visibility.Visible : Visibility.Hidden;

        private List<ITreeHandler> treeHandlers = new List<ITreeHandler>();
        private Timer _scanTimer;

        public Brush BackgroundColor { get; set; }
        /// -----------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        /// -----------------------------------------------------------------------
        public AppModel(Dispatcher dispatcher)
        {
            GetHandlersAsync(dispatcher);
            _scanTimer = new Timer(HandleScanTick, null, 0, 100);            
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Safely recalculate the tree
        /// </summary>
        /// -----------------------------------------------------------------------
        internal void SafeRecalculate(string valueColumn, string heatmapColumn,TreeFilter[] filters )
        {
            lock(this)
            {
                TreeRoot?.Recalculate(valueColumn, heatmapColumn, filters);
            }
        }
        /// -----------------------------------------------------------------------
        /// <summary>
        /// Store the filter in the list
        /// </summary>
        /// -----------------------------------------------------------------------
        internal void RememberFilter()
        {
            if (_currentFilter != null && _currentFilter.IsValid)
            {
                StoredFilters.Add(_currentFilter);
                ResetCurrentFilter();
            }
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Clear the current filter that is getting edited and recalculate the tree
        /// </summary>
        /// -----------------------------------------------------------------------
        public  void ClearCurrentFilter()
        {
            ResetCurrentFilter();
            SafeRecalculate(SelectedValueColumn, SelectedHeatmapColumn, GetFilters());
            OnDataChanged?.Invoke();
        }
        /// -----------------------------------------------------------------------
        /// <summary>
        /// Reset the current filter
        /// </summary>
        /// -----------------------------------------------------------------------
        private void ResetCurrentFilter()
        {
            _currentFilter = new TreeFilter();
            RaisePropertyChanged(nameof(SelectedFilterColumn));
            RaisePropertyChanged(nameof(SelectedFilterOperator));
            RaisePropertyChanged(nameof(FilterText));
            RaisePropertyChanged(nameof(FilterTextStatusColor));
            RaisePropertyChanged(nameof(FilterIsCaseSensitive));
            RaisePropertyChanged(nameof(FilterIsValid));
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Selete a filter in the list
        /// </summary>
        /// -----------------------------------------------------------------------
        internal void DeleteFilter(int deleteThisId)
        {
            foreach(var filter in StoredFilters.ToArray())
            {
                if(filter.Id == deleteThisId)
                {
                    StoredFilters.Remove(filter);
                    break;
                }
            }
            OnFilterUpdate();
        }


        /// -----------------------------------------------------------------------
        /// <summary>
        /// When the filter is updated, kick off a signal to recalculate the tree
        /// </summary>
        /// -----------------------------------------------------------------------
        void OnFilterUpdate(bool now = false)
        {
            _reCalculateAtCount = _count + 5;
            RaisePropertyChanged(nameof(FilterIsValid));
            RaisePropertyChanged(nameof(FilterTextStatusColor));
        }

        int _count;
        int _reCalculateAtCount;

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Update Scan information
        /// </summary>
        /// -----------------------------------------------------------------------
        private void HandleScanTick(object state)
        {
            _count++;
            if (_count > _reCalculateAtCount)
            {
                _count = 0;
                _reCalculateAtCount = int.MaxValue;
                SafeRecalculate(SelectedValueColumn, SelectedHeatmapColumn, GetFilters());
                ShowContent();
                OnDataChanged?.Invoke();
            }

            if (!ReadyToScan && CurrentTreeHandler != null)
            {
                ScanStatusText = "Scan progress: "
                    + CurrentTreeHandler.UnitsScannedSoFar.ToString("n0")
                    + Environment.NewLine + CurrentTreeHandler.CurrentItemLabel;
            }
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Get an array of filters for the current tree
        /// </summary>
        /// -----------------------------------------------------------------------
        private TreeFilter[] GetFilters()
        {
            var output = new List<TreeFilter>();
            output.Add(_currentFilter.Clone());
            output.AddRange(StoredFilters);
            return output.ToArray();
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Try to load each handler in it's own thread
        /// </summary>
        /// -----------------------------------------------------------------------
        private void GetHandlersAsync(Dispatcher dispatcher)
        {
            var treeInterface = typeof(ITreeHandler);
            var handlerTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => treeInterface.IsAssignableFrom(t));

            var settingsHandler = new PluginSettingsHandler();
            foreach (var handlerType in handlerTypes)
            {
                if(handlerType.Name == "ITreeHandler" 
                    || handlerType.Name == "BaseTreeHandler" 
                    || handlerType.Name == "BrokenTreeHandler")
                {
                    continue;
                }

                Task.Run(() =>
                {
                    try
                    {
                        var newHandler = (ITreeHandler)Activator.CreateInstance(handlerType, settingsHandler);
                        lock (treeHandlers)
                        {
                            treeHandlers.Add(newHandler);
                        }

                        var newSeeds = newHandler.GetSeeds();
                        dispatcher.BeginInvoke(new Action(() =>
                        {
                            lock (Seeds)
                            {
                                foreach (var seed in newSeeds)
                                {
                                    Seeds.Add(seed);
                                    if (SelectedSeed == null) SelectedSeed = seed;
                                }
                            }
                        }));

                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Failed to create handler: " + handlerType.Name);
                        Debug.WriteLine("  Error: " + e.Message);
                        lock(Seeds)
                        {
                            var errorName = "Unavailable: " + handlerType.Name;
                            Seeds.Add(new Seed() { Name = errorName, TreeHandler = new BrokenTreeHandler(errorName) });
                        }
                    }
                });
            }
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Add a new filter
        /// </summary>
        /// -----------------------------------------------------------------------
        internal void AddFilter(object p)
        {
            throw new NotImplementedException();
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Do something with a double-clicked item
        /// </summary>
        /// -----------------------------------------------------------------------
        internal void HandleItemDoubleClick(IItemData item)
        {

            CurrentTreeHandler.HandleItemDoubleClick(item);
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Spawn a scan based on the selected preset
        /// </summary>
        /// -----------------------------------------------------------------------
        internal void SpawnPresetScan()
        {
            SpawnScan(SelectedSeed);
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Spawn a scan based on the selected preset
        /// </summary>
        /// -----------------------------------------------------------------------
        internal void TryDrop(IDataObject data)
        {
            foreach (ITreeHandler handler in treeHandlers)
            {
                if (handler.TryHandleDrop(data, out var seed))
                {
                    StoredFilters.Clear();
                    ClearCurrentFilter();
                    SpawnScan(seed);
                    break;
                }
            }
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Spawn a scan based on what the user typed in
        /// </summary>
        /// -----------------------------------------------------------------------
        internal void SpawnManualScan()
        {
            bool handled = false;
            foreach (ITreeHandler handler in treeHandlers)
            {
                Seed seed;
                if (handler.TryHandleLocation(ManualScanLocation.Trim(), out seed))
                {
                    handled = true;
                    SpawnScan(seed);
                    break;
                }
            }

            if (!handled) throw new ApplicationException("Could not find a handler for this location: " + ManualScanLocation);
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Execute an asynchronous scan from the seed
        /// </summary>
        /// -----------------------------------------------------------------------
        public void SpawnScan(Seed seed)
        {
            RaisePropertyChanged(nameof(BackgroundColor));

            if (!ReadyToScan) 
            {
                throw new ApplicationException("Another scan is already in progress");
            }

            CurrentTreeHandler = seed.TreeHandler;
            CurrentTreeHandler.ValidateSeed(seed);
            if (!CurrentTreeHandler.HandlePreScan(seed))
            {
                return;
            }

            TreeRoot = null;

            // Clear out memory before we potentially try to load something big
            GC.Collect(2); 
            ReadyToScan = false;

            CurrentTreeHandler.StartScan(seed, () =>
            {
                TreeRoot = CurrentTreeHandler.Root;
                ClickInstructions = TreeRoot.ClickInstructions;
                RaisePropertyChanged(nameof(ValueColumns));
                RaisePropertyChanged(nameof(HeatmapColumns));
                RaisePropertyChanged(nameof(SelectedValueColumn));
                OnFilterUpdate();
                RaisePropertyChanged(nameof(AllColumns));
                ReadyToScan = true;
                _scanLocation = seed.Name;
                RaisePropertyChanged(nameof(Title));
            }, GetFilters());
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Cancel the current scan
        /// </summary>
        /// -----------------------------------------------------------------------
        internal void CancelScan()
        {
            CurrentTreeHandler?.CancelScan();
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Fill in highlight text
        /// </summary>
        /// -----------------------------------------------------------------------
        internal void HoverOn(ITreeItem treeItem)
        {
            HighlightedSizeText = CurrentTreeHandler.GetFormattedValue( treeItem.TotalValue);
            HighlightedPathText = treeItem.FullName;
        }

        ITreeItem _currentContentItem;
        const int ITEMCOUNT_INCREMENT = 50;
        int _maxItemsToShow = ITEMCOUNT_INCREMENT;

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Fill the data table with the content details
        /// </summary>
        /// -----------------------------------------------------------------------
        internal void ShowMoreContent()
        {
            if (_currentContentItem == null) return;
            _maxItemsToShow *= 2;
            ShowContent(_currentContentItem, showMore: true);
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// exclude a dirItem from the search
        /// </summary>
        /// -----------------------------------------------------------------------
        internal void Exclude(ITreeItem dirItem)
        {
            var filter = new TreeFilter();
            filter.ColumnName = StandardColumns.PATH;
            filter.FilterText = dirItem.FullName;
            filter.Operator = TreeFilter.Operators.DoesNotContain;
            StoredFilters.Add(filter);
            OnFilterUpdate();
        }


        /// -----------------------------------------------------------------------
        /// <summary>
        /// Fill the data table with the content details
        /// </summary>
        /// -----------------------------------------------------------------------
        internal void ShowContent(ITreeItem rootTreeItem = null, bool showMore = false)
        {
            if(rootTreeItem == null)
            {
                rootTreeItem = _currentContentItem;
                showMore = _maxItemsToShow > ITEMCOUNT_INCREMENT;
            }
            else
            {
                _currentContentItem = rootTreeItem;
                if(!showMore)
                {
                    _maxItemsToShow = ITEMCOUNT_INCREMENT;
                }
            }

            int specialColumnCount = 0;
            var newTable = new DataTable();
            if(CurrentTreeHandler == null|| CurrentTreeHandler.DataColumnNames == null)
            {
                DetailSize = "";
                DetailPath = "";
                DetailHeatmapSize = "";
                DetailChildHeatmapInfo = "";
                this.ItemDetailTable = newTable.DefaultView.ToTable();
                RaisePropertyChanged(nameof(ItemDetailTable));
                return;
            }

            newTable.Columns.Add(new DataColumn(StandardColumns.TAG) { DataType = typeof(IItemData) });
            newTable.Columns.Add(new DataColumn(SelectedValueColumn) { DataType = typeof(double) });
            if(SelectedHeatmapColumn != null && SelectedHeatmapColumn != SelectedValueColumn)
            {
                newTable.Columns.Add(new DataColumn(SelectedHeatmapColumn) { DataType = typeof(double) });
            }
            newTable.Columns.Add(new DataColumn(StandardColumns.NAME));
            specialColumnCount = newTable.Columns.Count;
            foreach (var columnName in CurrentTreeHandler.DataColumnNames)
            {
                // No need to show these columns
                if (columnName == SelectedValueColumn
                    || columnName == SelectedHeatmapColumn
                    || columnName == StandardColumns.NAME
                    || columnName == StandardColumns.COUNT
                    || columnName == CurrentTreeHandler.PathColumn
                    )
                {
                    continue;
                }

                var newColumn = new DataColumn(columnName);
                if(CurrentTreeHandler.ValueColumns.Contains(columnName))
                {
                    newColumn.DataType = typeof(double);
                }
                newTable.Columns.Add(newColumn);
            }

            ShowContentButtonVisibility = Visibility.Collapsed;
            var possibleShowCount = 0;

            if (rootTreeItem != null && rootTreeItem.TotalValue > 0)
            {
                // update our UI parameters
                possibleShowCount = showMore ? rootTreeItem.TotalItemCount : rootTreeItem.Content.Count;
                var filters = GetFilters();

                DetailSize = $"{SelectedValueColumn} in this node: {CurrentTreeHandler.GetFormattedValue(rootTreeItem.ContentValue)}";
                ChildContentInfo = $"{SelectedValueColumn} in {rootTreeItem.TotalItemCount} children: {CurrentTreeHandler.GetFormattedValue(rootTreeItem.TotalValue - rootTreeItem.ContentValue)}";
                DetailPath = "Details of " + rootTreeItem.FullName;
                if(SelectedHeatmapColumn != null)
                {
                    DetailHeatmapSize = $"{SelectedHeatmapColumn} in this node: {rootTreeItem.HeatmapContentValue}";
                    DetailChildHeatmapInfo = $"{SelectedHeatmapColumn} in {rootTreeItem.TotalItemCount} children: {rootTreeItem.HeatmapTotalValue - rootTreeItem.HeatmapContentValue}";
                }
                else
                {
                    DetailHeatmapSize = "";
                    DetailChildHeatmapInfo = "";
                }
                var contentQueue = new Queue<ITreeItem>();
                contentQueue.Enqueue(rootTreeItem);
                var possibleItems = new List<IItemData>();

                // Apply filters to get a list of all the items we can show
                while (contentQueue.Count > 0)
                {
                    var treeItem = contentQueue.Dequeue();
                    foreach (var contentItem in treeItem.Content.Values)
                    {
                        // Apply content filters
                        var keepItem = true;
                        foreach (var filter in filters)
                        {
                            if (!filter.ShouldAllow(contentItem))
                            {
                                keepItem = false;
                                break;
                            }
                        }
                        if (!keepItem) continue;
                        possibleItems.Add(contentItem);
                    }

                    if(showMore)
                    {
                        foreach (var item in treeItem.Children)
                        {
                            contentQueue.Enqueue(item);
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                // Pick the most significant of our content items to show
                var sortedList = possibleItems
                    .OrderByDescending(i => i.GetValue(SelectedValueColumn))
                    .Take(_maxItemsToShow);

                int displayedCount =0;
                foreach(var contentItem in sortedList)
                {
                    displayedCount++;
                    var itemValue = contentItem.GetValue(SelectedValueColumn);
                    var newRow = newTable.NewRow();
                    newRow[StandardColumns.TAG] = contentItem as IItemData;
                    newRow[SelectedValueColumn] = itemValue;
                    if(SelectedHeatmapColumn != null && SelectedHeatmapColumn != SelectedValueColumn)
                    {
                        newRow[SelectedHeatmapColumn] = contentItem.GetValue(SelectedHeatmapColumn);
                    }
                    newRow[StandardColumns.NAME] = contentItem.FullName.Substring(rootTreeItem.FullName.Length + 1);
                    newRow[StandardColumns.PATHDEPTH] = contentItem.GetValue(StandardColumns.PATHDEPTH);

                    for (int i = specialColumnCount; i < newTable.Columns.Count; i++)
                    {
                        var column = newTable.Columns[i];
                        if (column.DataType == typeof(double))
                        {
                            newRow[column] = contentItem.GetValue(column.ColumnName);
                        }
                        else
                        {
                            newRow[column] = contentItem.GetText(column.ColumnName);
                        }
                    }
                    newTable.Rows.Add(newRow);
                }
                newTable.DefaultView.Sort = SelectedValueColumn + " DESC";

                if(displayedCount < rootTreeItem.TotalItemCount)
                {
                    ShowContentButtonVisibility = Visibility.Visible;
                }
                ShowContentButtonText = displayedCount == rootTreeItem.LocalItemCount ? "Show Child Items" : "Show More Items";

            }
            else
            {
                DetailSize = "";
                DetailPath = "";
                DetailHeatmapSize = "";
                DetailChildHeatmapInfo = "";
            }
            ShowCountText = $"Showing {newTable.Rows.Count} of {possibleShowCount} items.";
            this.ItemDetailTable = newTable.DefaultView.ToTable(); 
            RaisePropertyChanged(nameof(ItemDetailTable));
        }
    }
}
