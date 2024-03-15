using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static DkUtil.DkImageViewer;

namespace DkUtil
{
    /// <summary>
    /// DkImageViewer.xaml の相互作用ロジック
    /// </summary>
    public partial class DkImageViewer : System.Windows.Controls.UserControl
    {
        public enum SourceInstanceModes { Orginal, Commmon }
        public enum ImageRotationAngles { Deg0 = 0, Deg90 = 90, Deg180 = 180, Deg270 = 270 }
        public SourceInstanceModes SourceInstanceMode { get; private set; } = SourceInstanceModes.Orginal;


        public BitmapScalingMode EnlargedImageBitmapScalingMode { get; set; } = BitmapScalingMode.NearestNeighbor;
        public BitmapScalingMode ReducedImageBitmapScalingMode { get; set; } = BitmapScalingMode.Fant;

        public string Title
        {
            set
            {
                TitleText.Text = value;
                if (value == null || value == "")
                {
                    TitleText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TitleText.Visibility = Visibility.Visible;
                }
                TitleText.Background = new SolidColorBrush(Colors.Blue);
                TitleText.Foreground = new SolidColorBrush(Colors.White);
            }
        }
        public Visibility BaseToolPanelVisibility
        {
            get
            {
                MainStackPanel.Children.Remove(BaseToolPanel);
                return this.BaseToolPanel.Visibility;
            }
            set
            {

                if (MainStackPanel.Children.Contains(BaseToolPanel))
                {
                    if (value == Visibility.Hidden)
                    {
                        MainStackPanel.Children.Remove(BaseToolPanel);
                    }
                }
                else
                {
                    if (value == Visibility.Visible)
                    {
                        MainStackPanel.Children.Add(BaseToolPanel);
                    }
                }
                this.BaseToolPanel.Visibility = value;
            }
        }
        //public double DisplayMagnificationRate { get; private set; } = 1.0;
        public ReactivePropertySlim<double> DisplayMagnificationRate { get; set; } = new ReactivePropertySlim<double>(1.0);

        int dispRect_SrcSx;
        int dispRect_SrcSy;
        int dispRect_SrcWx;
        int dispRect_SrcWy;

        bool dispImageChanging = false;

        System.Windows.Point mouseDownPoint = new();

        double[] DefaultDisplayMagnificationRates { get; set; } = { 0.005, 0.01, 0.02, 0.05, 0.1, 0.25, 0.5, 1.0, 2, 4, 8 };


        class DisplayMagnificationRateItem
        {
            public double Rate;

            public DisplayMagnificationRateItem(double rate)
            {
                Rate = rate;
            }
            public override string ToString()
            {
                return $" x {Rate} ";
            }
            public static List<DisplayMagnificationRateItem> MakeDisplayMagnificationRateItemList(double[] array)
            {
                List<DisplayMagnificationRateItem> list = new();

                Array.Sort(array);
                Array.Reverse(array);
                foreach (var t in array)
                {
                    DisplayMagnificationRateItem newItem = new(t);
                    list.Add(newItem);
                }
                return list;
            }
        }
        class ShapeInfo
        {
            public System.Windows.Point Point;
            public Shape Shape;
            public ShapeInfo(System.Windows.Point point, Shape shape)
            {
                Point = point;
                Shape = shape;
            }
        }
        List<ShapeInfo> shapeInfoList = new();

        public event EventHandler SelectRegionSet;
        protected void OnSelectRegionSet(EventArgs e)
        {
            if (SelectRegionSet != null)
                SelectRegionSet(this, e);
        }
        public System.Windows.Rect SelectRegion;
        public enum SelectRegionModes { Rectangle, Ellipse }


        private Mat sourceMat = new();

        public DkImageViewer()
        {
            InitializeComponent();

            foreach (var t in DisplayMagnificationRateItem.MakeDisplayMagnificationRateItemList(DefaultDisplayMagnificationRates))
            {
                ComboBoxDisplayMagnificationRate.Items.Add(t);
            }

            SetDisplayMagnificationRate(1.0);

            foreach (var t in Enum.GetValues(typeof(BitmapScalingMode)))
            {
                ComboBoxEnlargedImageBitmapScalingMode.Items.Add(t);
                ComboBoxReducedImageBitmapScalingMode.Items.Add(t);
            }
            ComboBoxEnlargedImageBitmapScalingMode.SelectedItem = EnlargedImageBitmapScalingMode;
            ComboBoxReducedImageBitmapScalingMode.SelectedItem = ReducedImageBitmapScalingMode;


            foreach (var t in Enum.GetValues(typeof(ImageRotationAngles)))
            {
                ComboBoxImageRotationAngle.Items.Add(t);

            }
            ComboBoxImageRotationAngle.SelectedItem = ImageRotationAngles.Deg0;

            foreach (var t in Enum.GetValues(typeof(SelectRegionModes)))
            {
                ComboBoxSelectRegionMode.Items.Add(t);

            }
            ComboBoxSelectRegionMode.SelectedItem = SelectRegionModes.Rectangle;

            //  ExToolPanel.Visibility = Visibility.Collapsed;
        }

        public bool SetSourceImage(Mat newSourceMat)
        {
            return SetSourceImage(newSourceMat, SourceInstanceModes.Orginal);
        }

        public bool SetSourceImage(Mat newSourceMat, SourceInstanceModes newSourceInstanceMode)
        {
            if (SourceInstanceMode == SourceInstanceModes.Orginal)
            {
                if (!sourceMat.IsDisposed)
                {
                    sourceMat.Dispose();
                }
            }

            if (newSourceInstanceMode == SourceInstanceModes.Orginal)
            {
                sourceMat = newSourceMat.Clone();  // これでよいか？
            }
            else
            {
                sourceMat = newSourceMat;
            }

            return true;
        }
        //public OpenCvSharp.Rect GetRectangle()
        //{
        //    this.GetDispShape
        //}
        public double SetDisplayMagnificationRate(double targetRate)
        {
            //　降順ソート済と仮定
            DisplayMagnificationRateItem minItem = (DisplayMagnificationRateItem)ComboBoxDisplayMagnificationRate.Items[^1];
            DisplayMagnificationRateItem selectItem = minItem;

            if (targetRate < (double)minItem.Rate)
            {
                ComboBoxDisplayMagnificationRate.SelectedIndex = 0;
                selectItem = minItem;
            }

            foreach (var t in ComboBoxDisplayMagnificationRate.Items)
            {
                DisplayMagnificationRateItem item = (DisplayMagnificationRateItem)t;
                if (item.Rate <= targetRate)
                {
                    selectItem = (DisplayMagnificationRateItem)item;
                    break;
                }
            }
            ComboBoxDisplayMagnificationRate.SelectedItem = selectItem;
            DisplayMagnificationRate.Value = ((DisplayMagnificationRateItem)ComboBoxDisplayMagnificationRate.SelectedItem).Rate;

            return selectItem.Rate;
        }

        public System.Windows.Point GetCenter()
        {
            return new System.Windows.Point(dispRect_SrcSx + dispRect_SrcWx / 2, dispRect_SrcSy + dispRect_SrcWy / 2);
        }
        public System.Windows.Point SetCenter(System.Windows.Point p)
        {
            return SetCenter(p.X, p.Y);
        }


        public System.Windows.Point SetCenter(double cx, double cy)
        {
            dispImageChanging = true;
            ScrollViewerImage.ScrollToHorizontalOffset(cx * DisplayMagnificationRate.Value - ScrollViewerImage.ViewportWidth / 2.0);
            ScrollViewerImage.ScrollToVerticalOffset(cy * DisplayMagnificationRate.Value - ScrollViewerImage.ViewportHeight / 2.0);
            dispImageChanging = false;

            DrawImage();
            return new System.Windows.Point(cx, cy); //　実際の中心を示すように変更するよ
        }

        public System.Windows.Point SetLeftTop(System.Windows.Point p)
        {
            return SetLeftTop(p.X, p.Y);
        }
        public System.Windows.Point SetLeftTop(double left, double top)
        {
            dispImageChanging = true;
            ScrollViewerImage.ScrollToHorizontalOffset(left * DisplayMagnificationRate.Value);
            ScrollViewerImage.ScrollToVerticalOffset(top * DisplayMagnificationRate.Value);
            dispImageChanging = false;

            DrawImage();
            return new System.Windows.Point(left, top); //　実際の座標を示すように変更するよ
        }
        public static void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            var callback = new DispatcherOperationCallback(obj =>
            {
                ((DispatcherFrame)obj).Continue = false;
                return null;
            });
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, callback, frame);
            Dispatcher.PushFrame(frame);
        }
        private void DrawImage()
        {
            if (dispImageChanging)
            {
                return ;
            }
            if (sourceMat.Empty())
            {
                return ;
            }
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(DrawImage));
                return;
            }
            GridImage.Width = sourceMat.Width * DisplayMagnificationRate.Value;
            GridImage.Height = sourceMat.Height * DisplayMagnificationRate.Value;
            ScrollViewerImage.UpdateLayout();

            if (ScrollViewerImage.ViewportWidth == 0) //暫定
            {
                return ;
            }
            dispRect_SrcWx = (int)(ScrollViewerImage.ViewportWidth / DisplayMagnificationRate.Value);
            dispRect_SrcWy = (int)(ScrollViewerImage.ViewportHeight / DisplayMagnificationRate.Value);

            dispRect_SrcWx = Math.Min(dispRect_SrcWx, (int)(sourceMat.Width));
            dispRect_SrcWy = Math.Min(dispRect_SrcWy, (int)(sourceMat.Height));

            dispRect_SrcSx = (int)(ScrollViewerImage.HorizontalOffset / DisplayMagnificationRate.Value);
            dispRect_SrcSy = (int)(ScrollViewerImage.VerticalOffset / DisplayMagnificationRate.Value);

            dispRect_SrcSx = Math.Min(dispRect_SrcSx, sourceMat.Width - dispRect_SrcWx);
            dispRect_SrcSy = Math.Min(dispRect_SrcSy, sourceMat.Height - dispRect_SrcWy);

            if (DisplayMagnificationRate.Value >= 1.0)
            {
                RenderOptions.SetEdgeMode(ImageMain, EdgeMode.Aliased);
                RenderOptions.SetBitmapScalingMode(ImageMain, EnlargedImageBitmapScalingMode);

                using (Mat displayMat = new(sourceMat, new OpenCvSharp.Rect(dispRect_SrcSx, dispRect_SrcSy, dispRect_SrcWx, dispRect_SrcWy)))
                {
                    BitmapSource dispBitmap = BitmapSourceConverter.ToBitmapSource(displayMat);

                    ImageMain.Width = dispRect_SrcWx * DisplayMagnificationRate.Value;
                    ImageMain.Height = dispRect_SrcWy * DisplayMagnificationRate.Value;

                    ImageMain.Margin = new Thickness(dispRect_SrcSx * DisplayMagnificationRate.Value, dispRect_SrcSy * DisplayMagnificationRate.Value, 0, 0);
                    ImageMain.Source = dispBitmap;
                }
            }
            else
            {
                RenderOptions.SetEdgeMode(ImageMain, EdgeMode.Unspecified);
                RenderOptions.SetBitmapScalingMode(ImageMain, ReducedImageBitmapScalingMode);
                using (Mat displayMat = new(sourceMat, new OpenCvSharp.Rect(dispRect_SrcSx, dispRect_SrcSy, dispRect_SrcWx, dispRect_SrcWy)))
                {
                    using (Mat tmMat = new())
                    {
                        int tmpWx = (int)(dispRect_SrcWx * DisplayMagnificationRate.Value);
                        int tmpWy = (int)(dispRect_SrcWy * DisplayMagnificationRate.Value);
                        Cv2.Resize(displayMat, tmMat, new OpenCvSharp.Size(tmpWx, tmpWy));

                        //BitmapSource dispBitmap = BitmapSourceConverter.ToBitmapSource(displayMat); //縮小前の画像がセットされてる（動作が重い）_asano
                        BitmapSource dispBitmap = BitmapSourceConverter.ToBitmapSource(tmMat);

                        ImageMain.Width = tmpWx;
                        ImageMain.Height = tmpWy;

                        ImageMain.Margin = new Thickness(dispRect_SrcSx * DisplayMagnificationRate.Value, dispRect_SrcSy * DisplayMagnificationRate.Value, 0, 0);
                        ImageMain.Source = dispBitmap;
                    }
                }
            }

            Console.WriteLine($"{DateTime.Now} drawImageEnd");
            return ;
        }


        public bool AddRectangle(OpenCvSharp.Rect rect, Color color, int strokeThickness = 1, DoubleCollection StrokeDashArray = null)
        {
            return AddRectangle(rect.X, rect.Y, rect.Width, rect.Height, color, strokeThickness, StrokeDashArray);
        }

        public bool AddRectangle(double sx, double sy, double wx, double wy, Color color, int strokeThickness = 1, DoubleCollection StrokeDashArray = null)
        {
            Rectangle rect = new();
            rect.Stroke = new SolidColorBrush(color);
            rect.StrokeThickness = strokeThickness;
            rect.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            rect.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            rect.StrokeDashArray = StrokeDashArray;

            rect.Width = wx;
            rect.Height = wy;

            ShapeInfo shapInfo = new(new System.Windows.Point(sx, sy), rect);
            shapeInfoList.Add(shapInfo);

            (Shape dispShape, System.Windows.Point point) = GetDispShape(shapInfo, DisplayMagnificationRate.Value);

            //Canvas.SetLeft(dispShape, sx * DisplayMagnificationRate);
            //Canvas.SetTop(dispShape, sy * DisplayMagnificationRate);

            Canvas.SetLeft(dispShape, point.X);
            Canvas.SetTop(dispShape, point.Y);

            CanvasShape.Children.Add(dispShape);
            return true;
        }
        (Shape dispShape, System.Windows.Point point) GetDispShape(ShapeInfo shapeInfo, double displayMagnificationRate)
        {
            Shape orgShape = shapeInfo.Shape;
            Type type = orgShape.GetType();
            Shape dispShape = (Shape)Activator.CreateInstance(type);

            dispShape.Stroke = orgShape.Stroke;
            dispShape.StrokeThickness = orgShape.StrokeThickness;
            dispShape.HorizontalAlignment = orgShape.HorizontalAlignment;
            dispShape.VerticalAlignment = orgShape.VerticalAlignment;
            dispShape.StrokeDashArray = orgShape.StrokeDashArray;

            if (DisplayMagnificationRate.Value < 1)
            {
                //dispShape.StrokeThickness = orgShape.StrokeThickness / DisplayMagnificationRate.Value; //Rectの線がぶっとくなってしまうのでコメントアウト _asano
                dispShape.Width = (orgShape.Width + dispShape.StrokeThickness * 2) * DisplayMagnificationRate.Value;
                dispShape.Height = (orgShape.Height + dispShape.StrokeThickness * 2) * DisplayMagnificationRate.Value;
            }
            else
            {
                dispShape.Width = (orgShape.Width + dispShape.StrokeThickness * 2) * DisplayMagnificationRate.Value;
                dispShape.Height = (orgShape.Height + dispShape.StrokeThickness * 2) * DisplayMagnificationRate.Value;
            }

            System.Windows.Point point = new((shapeInfo.Point.X - dispShape.StrokeThickness) * displayMagnificationRate, (shapeInfo.Point.Y - dispShape.StrokeThickness) * displayMagnificationRate);

            return (dispShape, point);
        }
        public void ClearShapes()
        {
            shapeInfoList.Clear();
            CanvasShape.Children.Clear();

        }
        bool DrawShapes()
        {
            CanvasShape.Children.Clear();

            foreach (var shapInfo in shapeInfoList)
            {
                (Shape dispShape, System.Windows.Point point) = GetDispShape(shapInfo, DisplayMagnificationRate.Value);

                Canvas.SetLeft(dispShape, point.X);
                Canvas.SetTop(dispShape, point.Y);
                CanvasShape.Children.Add(dispShape);
            }
            return true;
        }

        public bool Draw()
        {
            DrawImage();
            DrawShapes();
            //DoEvents();
            //GC.Collect(2, GCCollectionMode.Forced);
            //GC.WaitForPendingFinalizers();
            return true;
        }

        private void ScrollViewerImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw();
        }

        private void ScrollViewerImage_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Draw();
        }

        private void ComboBoxDisplayMagnificationRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Windows.Point p = GetCenter();
            DisplayMagnificationRate.Value = ((DisplayMagnificationRateItem)ComboBoxDisplayMagnificationRate.SelectedItem).Rate;
            SetCenter(p);
        }

        private void ComboBoxEnlargedImageBitmapScalingMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EnlargedImageBitmapScalingMode = (BitmapScalingMode)ComboBoxEnlargedImageBitmapScalingMode.SelectedItem;
            DrawImage();
        }

        private void ComboBoxReducedImageBitmapScalingMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReducedImageBitmapScalingMode = (BitmapScalingMode)ComboBoxReducedImageBitmapScalingMode.SelectedItem;
            DrawImage();

        }

        public void SelectRegionClear()
        {
            CanvasSelectRegion.Children.Clear();
        }
        private void ImageMain_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Point p = e.GetPosition(ImageMain);

            int cx = (int)(dispRect_SrcSx + p.X / DisplayMagnificationRate.Value);
            int cy = (int)(dispRect_SrcSy + p.Y / DisplayMagnificationRate.Value);

            string scrollViewerInfo = $"{ScrollViewerImage.HorizontalOffset:F0} {ScrollViewerImage.VerticalOffset:F0} {ScrollViewerImage.ViewportWidth:F0} {ScrollViewerImage.ViewportHeight:F0}";
            TextInfo.Text = $"({cx,5},{cy,5}) {sourceMat.Get<byte>(cy, cx)}"; //要修正

            if (rectDrawing)
            {
                var vector = p - mouseDownPoint;

                double th = 3;
                if (vector.X < th && vector.X > -th)
                {
                    return;
                }
                if (vector.Y < th && vector.Y > -th)
                {
                    return;
                }

                double dd = th - 2;
                CanvasSelectRegion.Children.Clear();

                double sx = mouseDownPoint.X;
                double wx = vector.X;

                if (wx > 0)
                {
                    wx -= dd;
                }
                else
                {
                    sx = p.X + dd;
                    wx = -wx - dd;
                }

                double sy = mouseDownPoint.Y;
                double wy = vector.Y - dd;
                if (wy > 0)
                {
                    wy -= dd;
                }
                else
                {
                    sy = p.Y + dd;
                    wy = -wy - dd;
                }
                double dispRate = DisplayMagnificationRate.Value;

                switch (ComboBoxSelectRegionMode.SelectedItem)
                {
                    case SelectRegionModes.Rectangle:
                        Rectangle rect = new();
                        rect.Stroke = new SolidColorBrush(Colors.Red);
                        rect.StrokeThickness = 1;
                        rect.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        rect.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                        rect.Width = wx;
                        rect.Height = wy;

                        Canvas.SetLeft(rect, dispRect_SrcSx * dispRate + sx);
                        Canvas.SetTop(rect, dispRect_SrcSy * dispRate + sy);
                        CanvasSelectRegion.Children.Add(rect);
                        TextInfo.Text = $"({(int)sx},{(int)sy}  {(int)wx} {(int)wy}) {vector.Length}";
                        break;
                    case SelectRegionModes.Ellipse:
                        Ellipse ellipse = new();
                        ellipse.Stroke = new SolidColorBrush(Colors.Red);
                        ellipse.StrokeThickness = 1;
                        ellipse.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        ellipse.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                        ellipse.Width = wx;
                        ellipse.Height = wy;

                        Canvas.SetLeft(ellipse, dispRect_SrcSx * dispRate + sx);
                        Canvas.SetTop(ellipse, dispRect_SrcSy * dispRate + sy);
                        CanvasSelectRegion.Children.Add(ellipse);
                        TextInfo.Text = $"({(int)sx},{(int)sy}  {(int)wx} {(int)wy}) {vector.Length}";
                        break;
                    default:
                        break;
                }
            }
        }

        private void IimageMain_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                if (System.Windows.Input.Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    /// 拡大・縮小
                    switch (e.ChangedButton)
                    {
                        case MouseButton.Left:
                            ZoomFunc(1);
                            break;
                        case MouseButton.Right:
                            ZoomFunc(-1);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void ImageMain_MouseUp(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now} <<imageMain_MouseUp");
        }

        private void ImageMain_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"{e.Delta}");
            if(Keyboard.Modifiers == ModifierKeys.Control) 
            {
                if (e.Delta < 0) ZoomFunc(-1);
                else ZoomFunc(1);
            }
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ZoomFunc(-1);
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ZoomFunc(1);
        }

        public void ZoomFunc(int value)
        {
            //System.Windows.Point p = new (dispRect_SrcSx + dispRect_SrcWx / 2, dispRect_SrcSy + dispRect_SrcWy / 2);

            int newIndex = ComboBoxDisplayMagnificationRate.SelectedIndex - value; // リストが降順のため
            newIndex = Math.Min(newIndex, ComboBoxDisplayMagnificationRate.Items.Count - 1);
            newIndex = Math.Max(newIndex, 0);
            if (newIndex != ComboBoxDisplayMagnificationRate.SelectedIndex)
            {
                ComboBoxDisplayMagnificationRate.SelectedIndex = newIndex;
            }
        }

        private void ImageMain_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Point p = e.GetPosition(ImageMain);

            if (e.ClickCount == 1)
            {
                if (System.Windows.Input.Keyboard.Modifiers == ModifierKeys.None)
                {
                    mouseDownPoint = p;
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.ScrollAll;
                }
            }
            else if (e.ClickCount > 1)
            {
                if (System.Windows.Input.Keyboard.Modifiers == ModifierKeys.Control)
                {
                    SetCenter(dispRect_SrcSx + p.X / DisplayMagnificationRate.Value, dispRect_SrcSy + p.Y / DisplayMagnificationRate.Value);
                }
            }
        }

        private void ImageMain_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.OverrideCursor = null;

            if (System.Windows.Input.Keyboard.Modifiers == ModifierKeys.None)
            {
                System.Windows.Point p = e.GetPosition(ImageMain);

                if (mouseDownPoint == p)
                {
                    return;
                }
                var vector = mouseDownPoint - p;

                SetLeftTop(dispRect_SrcSx + vector.X / DisplayMagnificationRate.Value, dispRect_SrcSy + vector.Y / DisplayMagnificationRate.Value);
            }
        }

        bool rectDrawing = false;
        private void ImageMain_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            mouseDownPoint = e.GetPosition(ImageMain);
            rectDrawing = true;
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now} >>imageMain_MouseRightButtonDown");
        }

        private void ImageMain_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            rectDrawing = false;
            System.Windows.Point p = e.GetPosition(ImageMain);

            if (mouseDownPoint == p)
            {
                CanvasSelectRegion.Children.Clear();
            }
            else
            {
                try
                {
                    var rect = (Rectangle)CanvasSelectRegion.Children[0];
                    double left = Canvas.GetLeft(rect) / DisplayMagnificationRate.Value;
                    double top = Canvas.GetTop(rect) / DisplayMagnificationRate.Value;
                    double width = rect.Width / DisplayMagnificationRate.Value;
                    double height = rect.Height / DisplayMagnificationRate.Value;
                    Debug.WriteLine($"  {left}  {top} {width} {height}");
                    SelectRegion = new System.Windows.Rect(left, top, width, height);
                    //  SelectRegionEventArg eventarg = new SelectRegionEventArg(rect.ver)
                    OnSelectRegionSet(EventArgs.Empty);
                }
                catch { 
                }
            }
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now} <<imageMain_MouseRightButtonUp");

        }
        public class SelectRegionEventArg : EventArgs
        {
            public int SX;
            public int SY;
            public int WX;
            public int WY;

            public SelectRegionEventArg(int sx, int sy, int wx, int wy)
            {
                SX = sx;
                SY = sy;
                WX = wx;
                WY = wy;
            }
        }
        RotateTransform imageRotateTransform = new();

        private void ComboBoxImageRotationAngle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // LayoutTransform layoutTransform;
            // imageRotateTransform.CenterX = dispRect_SrcSx * DisplayMagnificationRate + dispRect_SrcWx * DisplayMagnificationRate/2.0;
            // imageRotateTransform.CenterY = dispRect_SrcSy * DisplayMagnificationRate + dispRect_SrcWy* DisplayMagnificationRate/2.0;
            imageRotateTransform.Angle = (double)(ImageRotationAngles)ComboBoxImageRotationAngle.SelectedItem;
            GridMain.LayoutTransform = imageRotateTransform;
        }
        private void CheckBoxShowScrollBar_Changed(object sender, RoutedEventArgs e)
        {
            if (CheckBoxShowScrollBar.IsChecked == true)
            {
                ScrollViewerImage.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                ScrollViewerImage.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
            else
            {
                ScrollViewerImage.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                ScrollViewerImage.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
        }
        public void SaveScreen(string fileName)
        {
            try
            {
                BitmapSource bm = CopyScreen();

                BitmapEncoder en;
                switch (System.IO.Path.GetExtension(fileName).ToLower())
                {
                    case ".bmp":
                        en = new BmpBitmapEncoder();
                        break;
                    case ".jpg":
                        en = new JpegBitmapEncoder();
                        break;
                    case ".png":
                        en = new PngBitmapEncoder();
                        break;
                    default:
                        return;
                }

                en.Frames.Add(BitmapFrame.Create(bm));

                using (FileStream fs = File.Open(fileName, FileMode.Create))
                {
                    en.Save(fs);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return;
        }
        public BitmapSource CopyScreen()
        {
            int imageRotationAngle = 0; // 暫定　回転未対応

            int w;
            int h;

            switch (imageRotationAngle)
            {
                case 0:
                    w = (int)ImageMain.ActualWidth;
                    h = (int)ImageMain.ActualHeight;
                    break;
                case 90:
                    h = (int)ImageMain.ActualWidth;
                    w = (int)ImageMain.ActualHeight;
                    break;
                case 180:
                    w = (int)ImageMain.ActualWidth;
                    h = (int)ImageMain.ActualHeight;
                    break;
                default:
                    h = (int)ImageMain.ActualWidth;
                    w = (int)ImageMain.ActualHeight;
                    break;
            }
            return CopyScreen(w, h);
        }
        public BitmapSource CopyScreen(int ww, int hh)
        {
            // 暫定　回転未対応など
            bool scrollBarVisiblity = true;
            bool toolBarVisiblity = true;
            int imageRotationAngle = 0;

            int scrollbaarWidrh = scrollBarVisiblity ? 16 : 0;  // 確認
            int offsetX = 0;
            int offsetY = toolBarVisiblity ? 20 : 0;
            int w;
            int h;
            switch (imageRotationAngle)
            {
                case 0:
                    w = ww;
                    h = hh;
                    break;
                case 90:
                    offsetX += scrollbaarWidrh;
                    h = ww;
                    w = hh;
                    break;
                case 180:
                    offsetX += scrollbaarWidrh;
                    offsetY += scrollbaarWidrh;
                    w = ww;
                    h = hh;
                    break;
                default:
                    offsetY += scrollbaarWidrh;
                    h = ww;
                    w = hh;
                    break;
            }
            BitmapSource bitmapSource;
            using (var screenBmp = new System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (var bmpGraphics = System.Drawing.Graphics.FromImage(screenBmp))
                {
                    System.Windows.Point p = this.PointToScreen(new System.Windows.Point(offsetX, offsetY));
                    bmpGraphics.CopyFromScreen((int)p.X, (int)p.Y, 0, 0, screenBmp.Size);
                    bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                       screenBmp.GetHbitmap(),
                       IntPtr.Zero,
                       Int32Rect.Empty,
                       BitmapSizeOptions.FromEmptyOptions());
                }
            }
            return bitmapSource;

        }

    }
}
