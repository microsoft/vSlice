using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VSlice
{
    /// <summary>
    /// Interaction logic for StarburstChart.xaml
    /// </summary>
    public partial class StarburstChart : UserControl
    {
        public event Action<ITreeItem> OnItemHover;
        public event Action<ITreeItem> OnContentClicked;

        public static ContextMenu GetItemContextMenu(DependencyObject obj)
        {
            return (ContextMenu)obj.GetValue(ItemContextMenuProperty);
        }
        public static void SetItemContextMenu(DependencyObject obj, ContextMenu value)
        {
            obj.SetValue(ItemContextMenuProperty, value);
        }
        // Using a DependencyProperty as the backing store for AllowOnlyString. This enables animation, styling, binding, etc...  
        public static readonly DependencyProperty ItemContextMenuProperty =
        DependencyProperty.RegisterAttached("AllowOnlyString", typeof(ContextMenu), typeof(StarburstChart));


        public ITreeItem TreeRoot
        {
            get=> (ITreeItem)GetValue(TreeRootProperty);
            set { SetValue(TreeRootProperty, value); }
        }

        public static readonly DependencyProperty TreeRootProperty = DependencyProperty.Register(
                nameof(TreeRoot),
                typeof(ITreeItem),
                typeof(StarburstChart),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.None,
                    new PropertyChangedCallback(OnTreeRootChanged),
                    new CoerceValueCallback(OnCoerceTreeRoot)
                ),
                null
            );


        int _maxRingDepth = 8;
        DateTime _lastRenderTime;

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public StarburstChart()
        {
            InitializeComponent();
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Rerender the tree if the root signals any kind of change
        /// </summary>
        /// -----------------------------------------------------------------------------------
        private static void OnTreeRootChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as StarburstChart).RenderTree((ITreeItem)e.NewValue);
        }
        private static object OnCoerceTreeRoot(DependencyObject d, object baseValue)
        {
            (d as StarburstChart).RenderTree((ITreeItem)baseValue);
            return baseValue;
        }


        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Redraws all of the pie pieces based on the new Tree
        /// </summary>
        /// -----------------------------------------------------------------------------------
        private void RenderTree(ITreeItem tree)
        {
            var highlightedData = _selectedPiece?.Tag;
            _selectedPiece = null;
            TheCanvas.Children.Clear();

            if (tree == null) return;
            _lastRenderTime = DateTime.Now;

            double size = TheCanvas.ActualWidth;
            if (size > TheCanvas.ActualHeight) size = TheCanvas.ActualHeight;

            double centerDiameter = size * .25;
            double ringThickness = ((size - centerDiameter) / 2) / _maxRingDepth * .98;
            AddPieces(tree, false, TheCanvas.ActualWidth / 2, TheCanvas.ActualHeight / 2, 0, 0, centerDiameter/2, ringThickness, 359.9, 0);
            if(highlightedData != null)
            {
                SelectPieceWithTag(highlightedData as ITreeItem);
            }
        }

        delegate Brush ColorProvider(ITreeItem colorMe, bool isContent);

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Recursive method to add Pieces to the current pie graph
        /// </summary>
        /// -----------------------------------------------------------------------------------
        private void AddPieces(ITreeItem root, bool isContent, double x, double y, int depth, double innerRadius, double outerRadius, double ringThickness, double angularSize, double rotationAngle)
        {
            var minAngle = 50.0 / outerRadius;
            var newPiece = new PiePiece(this);
            newPiece.Tag = root;
            newPiece.IsContent = isContent;
            newPiece.Radius = outerRadius;
            newPiece.InnerRadius = innerRadius;
            newPiece.WedgeAngle = angularSize;
            newPiece.CentreX = x;
            newPiece.CentreY = y;
            newPiece.RotationAngle = rotationAngle;

            if(root.HeatmapBucket != null)
            {
                var heatmapRatio = (root.HeatmapBucket.Value - 50)/ 50.0;
                if (heatmapRatio < -1) heatmapRatio = -1;
                if (heatmapRatio > 1) heatmapRatio = 1;

                byte red, green, blue;

                // Red means  >= 3 standard deviations above average.
                // Blue means >= 3 standard deviations below average
                if(heatmapRatio < 0)
                {
                    red = (byte)(127 + heatmapRatio * 127);
                    green = (byte)(127 + heatmapRatio * 127);
                    blue = (byte)(127 + heatmapRatio * 60);
                }
                else
                {
                    red = (byte)(127 + heatmapRatio * 127);
                    green = (byte)(127 - heatmapRatio * 127);
                    blue = (byte)(127 - heatmapRatio * 127);
                }


                newPiece.Stroke = new SolidColorBrush(Color.FromRgb((byte)(red/2), (byte)(green/2), blue));
                if(isContent)
                {
                    red = (byte)(255 * .7 + red * .3);
                    green = (byte)(255 * .7 + green * .3);
                    blue = (byte)(255 * .7 + blue * .3);
                }
                newPiece.Fill = new SolidColorBrush(Color.FromRgb(red, green, blue));
            }
            else
            {
                newPiece.Stroke = new SolidColorBrush(Colors.ForestGreen);
                newPiece.StrokeThickness = 1.5;
                if (isContent) newPiece.Fill = new SolidColorBrush(Colors.LightGreen);
                else newPiece.Fill = new SolidColorBrush(Color.FromRgb(0, (byte)(depth * 15 + 60), 0));
            }

            TheCanvas.Children.Add(newPiece);

            if (depth >= _maxRingDepth - 1 || isContent) return;

            SortedList<ITreeItem, long> sortedList = new SortedList<ITreeItem, long>();

            double subAngle = (angularSize * root.ContentValue / root.TotalValue);
            
            if(subAngle > minAngle)
            {
                AddPieces(root, true, x, y, depth + 1, outerRadius, outerRadius + ringThickness, ringThickness, subAngle, rotationAngle);
            }

            rotationAngle += subAngle;

            var sortedChildren = root.Children.ToArray();
            Array.Sort(sortedChildren);
            foreach (ITreeItem child in sortedChildren)
            {
                subAngle = root.TotalValue > 0 ? (angularSize * child.TotalValue / root.TotalValue) : 0;
                if (subAngle < minAngle) continue;
                AddPieces(child, false, x, y, depth + 1, outerRadius, outerRadius + ringThickness, ringThickness, subAngle, rotationAngle);
                rotationAngle += subAngle;
            }
        }

        PiePiece _selectedPiece = null;

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// highlight this piepiece by putting at the end of the child list.
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public void MoveToLast(PiePiece peice)
        {
            if(TheCanvas.Children.Contains(peice))
            {
                TheCanvas.Children.Remove(peice);
                TheCanvas.Children.Add(peice);
                OnItemHover?.Invoke(peice.Tag as ITreeItem);
            }
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// ReRender the chart
        /// </summary>
        /// -----------------------------------------------------------------------------------
        internal void Rerender()
        {
            RenderTree(TreeRoot);
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        ///HandleItemDoubleClick
        /// </summary>
        /// -----------------------------------------------------------------------------------
        internal void HandleItemDoubleClick(PiePiece piePiece)
        {
            if (_selectedPiece != null)
            {
                _selectedPiece.Selected = false;
                _selectedPiece = null;
            }

            _selectedPiece = _previousPiece;
            _previousPiece = null;
            if (piePiece.Tag == TreeRoot && !piePiece.IsContent)
            {
                if (TreeRoot.Parent != null)
                {
                    TreeRoot = TreeRoot.Parent;
                }
                else
                {
                    if (_selectedPiece != null)
                    {
                        _selectedPiece.Selected = true;
                    }
                }
            }
            else if (!piePiece.IsContent)
            {
                TreeRoot = (piePiece.Tag as ITreeItem);
            }
        }

        PiePiece _previousPiece;
        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// HandleItemClick
        /// </summary>
        /// -----------------------------------------------------------------------------------
        internal void HandleItemClick(PiePiece piePiece)
        {
            var pieceData = piePiece.Tag as ITreeItem;

            if (ShiftIsPressed)
            {
                pieceData?.DoShiftClick();
            }
            else if (CtrlIsPressed)
            {
                pieceData?.DoCtrlClick();
            }
            else if (AltIsPressed)
            {
                pieceData?.DoAltClick();
            }
            else
            {
                if(_selectedPiece != null)
                {
                    _selectedPiece.Selected = false;
                    _previousPiece = _selectedPiece;
                }
                _selectedPiece = piePiece;
                _selectedPiece.Selected = true;
                Task.Run(()=> { OnContentClicked?.Invoke(pieceData); });
               
            }
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// HandleItemRightClick
        /// </summary>
        /// -----------------------------------------------------------------------------------
        internal void HandleItemRightClick(PiePiece piePiece)
        {
            var pieceData = piePiece.Tag as ITreeItem;

            var menu = GetItemContextMenu(this);
            if (menu != null)
            {
                menu.Tag = pieceData;
                menu.IsOpen = true;
            }

        }


        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// highlight the piece that corresponds to the tree Item
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public void SelectPieceWithTag(ITreeItem tagData)
        {
            foreach(var child in TheCanvas.Children)
            {
                var piece = child as PiePiece;
                if (piece == null) continue;
                if(piece.Tag == tagData)
                {
                    if(_selectedPiece != null)
                    {
                        _selectedPiece.Selected = false;
                    }
                    _selectedPiece = piece;
                    _selectedPiece.Selected = true;
                    break;
                }
            }
        }

        bool ShiftIsPressed => Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        bool CtrlIsPressed=> Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        bool AltIsPressed=> Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Redraw the PieChart when the size changes
        /// </summary>
        /// -----------------------------------------------------------------------------------
        private void OnGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var minDimension = Math.Min(e.NewSize.Width, e.NewSize.Height);

            _maxRingDepth = (int)(Math.Sqrt(minDimension / 5));
            if (_maxRingDepth < 3) _maxRingDepth = 3;
            if (_maxRingDepth > 10) _maxRingDepth = 10;

            RenderTree(TreeRoot);
        }
    }
}
