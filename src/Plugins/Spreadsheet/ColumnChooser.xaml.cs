using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VSlice
{
    /// <summary>
    /// Interaction logic for ColumnChooser.xaml
    /// </summary>
    public partial class ColumnChooser : Window
    {
        SpreadSheetModel _dataModel;
        public ColumnChooser(SpreadSheetModel dataModel)
        {
            InitializeComponent();

            _dataModel = dataModel;
            this.DataContext = dataModel;

            int i = 0;
            foreach(var column in dataModel.ColumnNames)
            {
                this.SampleData.Columns.Add(new DataGridTextColumn() { Header = column, Binding = new Binding($"[{i}]") });
                i++;
            }
        }

        protected override void OnContentRendered(EventArgs e)
        {
            _dataModel.FillSampleRows();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if(DialogResult == null)
            {
                DialogResult = false;
            }
            base.OnClosing(e);
        }


        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            var data = grid.SelectedItem as string[];
            _dataModel.HandleItemDoubleClick(data[_dataModel.PathColumnIndex]);
        }
    }
}
