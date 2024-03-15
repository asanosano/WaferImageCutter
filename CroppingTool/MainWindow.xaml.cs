using IjhCommonUtility;
using OpenCvSharp;
using PseudoDefectMaker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using Path = System.IO.Path;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size;
using Window = System.Windows.Window;

namespace CroppingTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.FileSelector.TargetChanged += ViewImage;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ButtonOcr_Click(object sender, RoutedEventArgs e)
        {
            var rects = new List<Rect>() { 
                new Rect(4386, 0, 500, 256),
                new Rect(620, 0, 700, 256),
                new Rect(7691, 0, 500, 256)
            };
            this.FileSelector.GetAllFiles().ForEach(file =>
            {
                for (int i = 0; i < rects.Count; i++)
                {
                    new Mat(new Mat(file, ImreadModes.Grayscale), rects[i]).SaveImage(FileManager.GetRenamedPath_Add(file, $"_crop{i}"));
                }
            });
        }
        private void ViewImage(object sender, EventArgs e)
        {
            var img = new Mat(this.FileSelector.TargetFilePath);
            this.MainImageViewer.SetSourceImage(img);
            this.MainImageViewer.Draw();
        }

        private void ButtonPreProcess_Click(object sender, RoutedEventArgs e)
        {
            var path = @"I:\DevWork\ocr\20230127010138_stitched_crop.tif";
            var dir = @"I:\DevWork\ocr";
            var roiRect = new Rect(42, 229, 1347, 175);
            var labelingAreaTh = 1000;
            var boxSize = new Size(1200, 120);
            var charSize = new Size(85, 120);
            var charInterval = 74.4;
            var charBox1 = new Rect(0, 0, 160, 120);
            var offset = 0;
            var files = Directory.GetFiles(dir);
            var dir2 = dir + "_processed";
            Directory.CreateDirectory(dir2);
            foreach (var file in files)
            {
                var rotated = new MeshDistortion().Rotate(new Mat(file, ImreadModes.Grayscale));
                var rotated2 = new Mat();
                Cv2.Rotate(rotated, rotated2, RotateFlags.Rotate180);
                rotated2.SaveImage(Path.Combine(dir2, Path.GetFileName(file)));
                var roiMat = MatFunctions.RoiToMat(rotated2, roiRect);
                var th = roiMat.Mean()[0] * 0.7;
                //Cv2.Threshold(roiMat, roiMat, th, 255, ThresholdTypes.BinaryInv);
                Cv2.Threshold(roiMat, roiMat, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
                MatFunctions.ShowImage(roiMat, "src");
                var rate = 0.5;
                var tmp = roiMat.Resize(Size.Zero, rate, rate, InterpolationFlags.Area);
                roiMat = tmp.Resize(roiMat.Size());
                Cv2.Threshold(roiMat, roiMat, 20, 255, ThresholdTypes.Binary);

                Cv2.MorphologyEx(roiMat, roiMat, MorphTypes.Erode, null);
                Cv2.MorphologyEx(roiMat, roiMat, MorphTypes.Dilate, null);
                byte[,] filter = { { 0 }, { 0 }, { 0 }, { 0 }, { 1 }, { 0 }, { 0 }, { 0 }, { 0 } };
                var filtSize = filter.GetLength(0);
                var filt = new Mat(filtSize, 1, MatType.CV_8UC1, filter).Repeat(1, filtSize);
                //Cv2.MorphologyEx(roiMat, roiMat, MorphTypes.Dilate, null);
                MatFunctions.ShowImage(roiMat, "src_");
                Cv2.MorphologyEx(roiMat, roiMat, MorphTypes.Dilate, filt, null, 4);
                Cv2.MorphologyEx(roiMat, roiMat, MorphTypes.Erode, filt, null, 4);
                //Cv2.Resize(roiMat, roiMat, new OpenCvSharp.Size(roiMat.Width, roiMat.Height / 5));
                MatFunctions.ShowImage(roiMat, "src_2");
                var cc2 = Cv2.ConnectedComponentsEx(roiMat);
                var blobs = cc2.Blobs.Skip(1);
                blobs = blobs.Where(b => b.Area > labelingAreaTh);
                blobs.ToList().ForEach(b=>Trace.WriteLine($"{b.Area}, {b.Rect}"));
                var midPoint = blobs.OrderBy(b => b.Top + b.Height / 2).Skip(blobs.Count() / 2).First().Rect.Center();
                var midBlob3 = blobs.OrderBy(b => Math.Abs(b.Rect.Y - midPoint.Y)).Take(3).OrderBy(b => b.Rect.X);
                var midBlob = midBlob3.Skip(1).First();
                var orgBox = MatFunctions.RectFromCenterPoint(new Point(midBlob.Left+midBlob.Width/2 + charInterval*2.5+offset, midBlob.Top+midBlob.Height/2), boxSize);
                var boxes = new List<Rect>();
                for(int i = 0; i < 16; i++)
                {
                    boxes.Add(new Rect(orgBox.X + (int)(i * charInterval), orgBox.Y, charSize.Width, charSize.Height));
                }
                foreach (var box in boxes)
                {
                    var r = box.Add(roiRect.Location);
                    //rotated2.Rectangle(r, new Scalar(255), 2);
                    var tmpPath = FileManager.GetRenamedPath_Add(file, $"_X{r.X}_Y{r.Y}");
                    var savePath = FileManager.GetRenamedPathAnotherDir_New(tmpPath, "cut");
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                    new Mat(rotated2, r).SaveImage(savePath);
                }

                //var reduced = roiMat.Reduce(ReduceDimension.Row, ReduceTypes.Avg, -1);
                //MatFunctions.ShowImage(reduced.Repeat(100, 1), "reduced");
                //var threshed = reduced.Threshold(140, 255, ThresholdTypes.BinaryInv|ThresholdTypes.Otsu);
                //MatFunctions.ShowImage(threshed.Repeat(100, 1), "threshed", 1, false);

                //var cc = Cv2.ConnectedComponentsEx(threshed);
                //var blobs = cc.Blobs.Skip(1);
                //foreach (var blob in blobs)
                //{
                //    var r = MatFunctions.RectFromCenterPoint(blob.Rect.Center(), new Size(blob.Rect.Width + 10, blob.Rect.Height + 10));
                //    //var r = new Rect(blob.Left, roiRect.Y, blob.Width, roiRect.Height);
                //    rotated2.Rectangle(r.Add(roiRect.Location), new Scalar(255), 2);
                //}
                MatFunctions.ShowImage(rotated2);
            }
            //MatFunctions.ShowImage(rotated2);
        }
    }
}
