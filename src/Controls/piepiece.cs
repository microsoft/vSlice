using System;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace VSlice
{

    /// <summary>
    /// A pie piece shape
    /// </summary>
    public class PiePiece : Shape
    {
        public bool IsContent { get; set; }

        #region dependency properties


        /// <summary>
        /// The radius of this pie piece
        /// </summary>
        public double Radius { get; set; }

        /// <summary>
        /// The distance to 'push' this pie piece out from the centre.
        /// </summary>
        public double PushOut { get; set; }

        /// <summary>
        /// The inner radius of this pie piece
        /// </summary>
        public double InnerRadius { get; set; }

        double _wedgeAngle;
        /// <summary>
        /// The wedge angle of this pie piece in degrees
        /// </summary>
        public double WedgeAngle
        {
            get => _wedgeAngle;//return(double)GetValue(WedgeAngleProperty); }
            set
            {
                _wedgeAngle = value;
                this.Percentage = (value / 360.0);
            }
        }


        /// <summary>
        /// The rotation, in degrees, from the Y axis vector of this pie piece.
        /// </summary>
        public double RotationAngle { get; set; }

        /// <summary>
        /// The X coordinate of centre of the circle from which this pie piece is cut.
        /// </summary>
        public double CentreX { get; set; }

        /// <summary>
        /// The Y coordinate of centre of the circle from which this pie piece is cut.
        /// </summary>
        public double CentreY { get; set; }

        /// <summary>
        /// The percentage of a full pie that this piece occupies.
        /// </summary>
        public double Percentage { get; set; }

        /// <summary>
        /// The value that this pie piece represents.
        /// </summary>
        public double PieceValue { get; set; }


        #endregion

        protected override Geometry DefiningGeometry
        {
            get
            {
                // Create a StreamGeometry for describing the shape
                StreamGeometry geometry = new StreamGeometry();
                geometry.FillRule = FillRule.EvenOdd;
                

                using (StreamGeometryContext context = geometry.Open())
                {
                    DrawGeometry(context);
                }

                // Freeze the geometry for performance benefits
                geometry.Freeze();

                return geometry;
            }
        }

        StarburstChart _parentChart;

        private bool _highlighted = false;
        public bool Highlighted
        {
            get => _highlighted;
            set
            {
                _highlighted = value;

                if(_highlighted)
                {
                    _regularStrokeBrush = Stroke;
                    Stroke = new SolidColorBrush(Colors.White);
                    StrokeThickness = 3;
                }
                else
                {
                    Stroke = _regularStrokeBrush;
                    StrokeThickness = 1.5;
                }

                _parentChart.MoveToLast(this);
            }
        }

        Brush _originalFill = Brushes.Black;
        private bool _selected = false;
        public bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                if(_selected)
                {
                    _originalFill = Fill;
                    Fill =  new SolidColorBrush(Color.FromRgb(220, 255, 220));
                }
                else
                {
                    Fill = _originalFill;
                }               
            }
        }

        Brush _regularStrokeBrush;

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public PiePiece(StarburstChart parentChart)
        {
            _parentChart = parentChart;
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Draws the pie piece
        /// </summary>
        /// -----------------------------------------------------------------------------------
        private void DrawGeometry(StreamGeometryContext context)
        {
            Point startPoint = new Point(CentreX, CentreY);

            Point innerArcStartPoint = PieUtils.ComputeCartesianCoordinate(RotationAngle, InnerRadius);
            innerArcStartPoint.Offset(CentreX, CentreY);

            Point innerArcEndPoint = PieUtils.ComputeCartesianCoordinate(RotationAngle + WedgeAngle, InnerRadius);
            innerArcEndPoint.Offset(CentreX, CentreY);

            Point outerArcStartPoint = PieUtils.ComputeCartesianCoordinate(RotationAngle, Radius);
            outerArcStartPoint.Offset(CentreX, CentreY);

            Point outerArcEndPoint = PieUtils.ComputeCartesianCoordinate(RotationAngle + WedgeAngle, Radius);
            outerArcEndPoint.Offset(CentreX, CentreY);

            bool largeArc = WedgeAngle > 180.0;

            if (PushOut > 0)
            {
                Point offset = PieUtils.ComputeCartesianCoordinate(RotationAngle + WedgeAngle / 2, PushOut);
                innerArcStartPoint.Offset(offset.X, offset.Y);
                innerArcEndPoint.Offset(offset.X, offset.Y);
                outerArcStartPoint.Offset(offset.X, offset.Y);
                outerArcEndPoint.Offset(offset.X, offset.Y);

            }

            Size outerArcSize = new Size(Radius, Radius);
            Size innerArcSize = new Size(InnerRadius, InnerRadius);

            
            context.BeginFigure(innerArcStartPoint, true, true);
            context.LineTo(outerArcStartPoint, true, true);
            context.ArcTo(outerArcEndPoint, outerArcSize, 0, largeArc, SweepDirection.Clockwise, true, true);
            context.LineTo(innerArcEndPoint, true, true);
            context.ArcTo(innerArcStartPoint, innerArcSize, 0, largeArc, SweepDirection.Counterclockwise, true, true);
        }

        bool _waitingLeftClick = false;
        bool _waitingRightClick = false;

        TimeSpan DoubleClickInterval = TimeSpan.FromSeconds(.25);


        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// OnMouseLeftButtonDown
        /// </summary>
        /// -----------------------------------------------------------------------------------
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _waitingLeftClick = true;
            _lastClickCount = e.ClickCount;
            base.OnMouseLeftButtonDown(e);
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// OnMouseRightButtonDown
        /// </summary>
        /// -----------------------------------------------------------------------------------
        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            _waitingRightClick = true;
            base.OnMouseRightButtonDown(e);
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Run something on the dispatcher in parallel
        /// </summary>
        /// -----------------------------------------------------------------------------------
        void DispatchAndForget(Action doThis)
        {
            Task.Run(async () =>
            {
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    doThis();
                }));
            });
        }

        int _lastClickCount = 0;
        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// OnMouseLeftButtonUp
        /// </summary>
        /// -----------------------------------------------------------------------------------
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (_waitingLeftClick)
            {
                if (_lastClickCount > 1)
                {
                    DispatchAndForget(() => { _parentChart.HandleItemDoubleClick(this); });
                }
                else
                {
                    DispatchAndForget(() => { _parentChart.HandleItemClick(this); });
                }

                _waitingLeftClick = false;
            }
            base.OnMouseLeftButtonUp(e);
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// OnMouseRightButtonUp
        /// </summary>
        /// -----------------------------------------------------------------------------------
        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            if (_waitingRightClick)
            {
                DispatchAndForget(() => { _parentChart.HandleItemRightClick(this); });
                _waitingRightClick = false;
            }
            base.OnMouseRightButtonUp(e);
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// OnMouseEnter
        /// </summary>
        /// -----------------------------------------------------------------------------------
        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
        {
            Highlighted = true;           
            base.OnMouseEnter(e);
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// OnMouseLeave
        /// </summary>
        /// -----------------------------------------------------------------------------------
        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            _waitingLeftClick = false;
            Highlighted = false;
            base.OnMouseLeave(e);
        }
    }
}
