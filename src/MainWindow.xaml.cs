using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Reflection;
using System.Windows.Interop;
using System.Windows.Threading;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Data;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System.Net;
using ICSharpCode.SharpZipLib.Core;
using System.Threading.Tasks;

namespace VSlice
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private AppModel _appModel = new AppModel(Dispatcher.CurrentDispatcher);


        /// -------------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        /// -------------------------------------------------------------------------------------
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = _appModel;
            _appModel.OnDataChanged += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    TheChart.Rerender();
                });
            };

            TheChart.OnContentClicked += (treeItem) =>
            {
                _appModel.ShowContent(treeItem);
            };
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Safely call a method and show a user error if it fails
        /// </summary>
        /// -----------------------------------------------------------------------
        void SafeCall(Action callMe)
        {
            try
            {
                callMe();
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Scan a drive
        /// </summary>
        /// -----------------------------------------------------------------------
        private void scanButton_Click(object sender, RoutedEventArgs e)
        {
            SafeCall(() => _appModel.SpawnPresetScan());
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Handle drop event
        /// </summary>
        /// -----------------------------------------------------------------------
        private void piechartCanvas_Drop(object sender, DragEventArgs e)
        {
            SafeCall(() => _appModel.TryDrop(e.Data));
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Handle a manual scan
        /// </summary>
        /// -----------------------------------------------------------------------
        private void scanButtonManual_Click(object sender, RoutedEventArgs e)
        {
            SafeCall(() => _appModel.SpawnManualScan());
        }

        Task _urlLoadTask;

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Window loaded
        /// </summary>
        /// -----------------------------------------------------------------------
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var args = ((App)App.Current).Args;
                if (args.Length > 0 && args[0].StartsWith("vslice:"))
                {
                    var url = "http://" + args[0].Split(new[] { ':' }, 2)[1];
                    _urlLoadTask = Task.Run(() => ViewPackedFile(url));
                }
            }
            catch (Exception err)
            {
                Debug.WriteLine(err.ToString());
            }

        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Unpack a .zip file and view the .tsv inside
        /// </summary>
        /// -----------------------------------------------------------------------
        private void ViewPackedFile(string url)
        {
            var blobName = Path.GetFileName(WebUtility.UrlDecode(url));
            var tempFileName = Path.Combine(Path.GetTempPath(), blobName);
            var outFolder = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(blobName));
            var data = (byte[])null;

            Dispatcher.BeginInvoke((Action)(() =>
            {
                _appModel.ReadyToScan = false;
                _appModel.ScanStatusText = "Downloaded 0KB from vslice package: " + url;
            }));

            try
            {
                using (WebClient client = new WebClient())
                {
                    var downloadTask = client.DownloadDataTaskAsync(url);
                    var totalDownloaded = 0L;
                    var lastMemory = GC.GetTotalMemory(false);
                    while (!downloadTask.IsCompleted)
                    {
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            var currentMemory = GC.GetTotalMemory(false);
                            var downloadSize = currentMemory - lastMemory;
                            if (downloadSize < 0) downloadSize = 0;
                            totalDownloaded += downloadSize;
                            lastMemory = currentMemory;
                            _appModel.ScanStatusText = $"Downloaded {totalDownloaded / 1024} KB from vslice package: " + url;
                        }));
                        Task.Delay(100).Wait();
                    }

                    data = downloadTask.Result;
                }

            }
            catch(Exception e)
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    var errorText = e.InnerException != null ? e.InnerException.Message : e.Message;
                    _appModel.ScanStatusText = $"Failed to download package: {errorText}";
                }));
                return;
            }

            Dispatcher.BeginInvoke((Action)(() =>
            {
                _appModel.ScanStatusText = $"Unpacking vslice package...";
            }));

            ZipFile zipFile = null;
            var targetFile = (string)null;
            try
            {

                var inputStream = new MemoryStream(data);
                zipFile = new ZipFile(inputStream);
                foreach (ZipEntry zipEntry in zipFile)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue;           // Ignore directories
                    }
                    String entryFileName = zipEntry.Name;
                    // to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
                    // Optionally match entrynames against a selection list here to skip as desired.
                    // The unpacked length is available in the zipEntry.Size property.

                    byte[] buffer = new byte[4096];     // 4K is optimum
                    Stream zipStream = zipFile.GetInputStream(zipEntry);

                    var fullZipToPath = Path.Combine(outFolder, entryFileName);
                    var directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                    // of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }

                    if(zipEntry.Name.ToLower().EndsWith(".tsv"))
                    {
                        targetFile = fullZipToPath;
                    }
                }
            }
            finally
            {
                if (zipFile != null)
                {
                    zipFile.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zipFile.Close(); // Ensure we release resources
                }
            }

            if (targetFile != null)
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    _appModel.ReadyToScan = true;
                    _appModel.TryDrop(new DataObject("FileNameW", new[] { targetFile }));

                }));
            }
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Handle a manual scan
        /// </summary>
        /// -----------------------------------------------------------------------
        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            _appModel.CancelScan();
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Show details for this tree item
        /// </summary>
        /// -----------------------------------------------------------------------
        private void HandleItemHover(ITreeItem treeItem)
        {
            _appModel.HoverOn(treeItem);
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Show content for this tree item
        /// </summary>
        /// -----------------------------------------------------------------------
        private void HandleContentClick(ITreeItem treeItem)
        {
            //_appModel.ShowContent(treeItem);
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Show content for this tree item
        /// </summary>
        /// -----------------------------------------------------------------------
        private void FixGeneratedGridColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.IsReadOnly = true;
            if(e.PropertyName == StandardColumns.TAG)
            {
                e.Column.Visibility = Visibility.Collapsed;
            }
            else if(e.PropertyName == _appModel.SelectedValueColumn)
            {
                e.Column.SortDirection = System.ComponentModel.ListSortDirection.Descending;
                var rightStyle = new Style { TargetType = typeof(DataGridCell) };
                rightStyle.Setters.Add(new Setter(Control.HorizontalAlignmentProperty, HorizontalAlignment.Right));
                e.Column.CellStyle = rightStyle;
            }
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Do something when an item is double-clicked in the content view
        /// </summary>
        /// -----------------------------------------------------------------------
        private void HandleItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            var rowView = grid?.SelectedCells[0].Item as DataRowView;
            var row = rowView.Row;
            _appModel.HandleItemDoubleClick(row[StandardColumns.TAG] as IItemData);
        }


        /// -----------------------------------------------------------------------
        /// <summary>
        /// Add a new filter
        /// </summary>
        /// -----------------------------------------------------------------------
        private void AddFilterClicked(object sender, RoutedEventArgs e)
        {
            _appModel.RememberFilter();  
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Delete one of the filters
        /// </summary>
        /// -----------------------------------------------------------------------
        private void DeleteFilterClicked(object sender, RoutedEventArgs e)
        {
            _appModel.DeleteFilter((int)(sender as Button).Tag);
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Show more details on the seletected child
        /// </summary>
        /// -----------------------------------------------------------------------
        private void ShowChildContentClicked(object sender, RoutedEventArgs e)
        {
            _appModel.ShowMoreContent();
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Show more details on the seletected child
        /// </summary>
        /// -----------------------------------------------------------------------
        private void ExcludeFolder(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var parent = menuItem?.Parent as ContextMenu;
            var dirItem = parent?.Tag as ITreeItem;

            _appModel.Exclude(dirItem);
            TheChart.Rerender();
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Un-apply the heatmap
        /// </summary>
        /// -----------------------------------------------------------------------
        private void RemoveHeatmap(object sender, RoutedEventArgs e)
        {
            _appModel.SelectedHeatmapColumn = null;
        }

        /// -----------------------------------------------------------------------
        /// <summary>
        /// Clear the filter we are currently editing
        /// </summary>
        /// -----------------------------------------------------------------------
        private void ClearFilterClicked(object sender, RoutedEventArgs e)
        {
            _appModel.ClearCurrentFilter();
        }
    }
}
