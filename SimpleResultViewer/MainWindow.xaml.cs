using IjhCommonUtility;
using OpenCvSharp;
using OpenCvSharp.ImgHash;
using OpenCvSharp.Text;
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
using Wafer;
using Rect = OpenCvSharp.Rect;

namespace SimpleResultViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //ウエハ読み込み
            var srcDirWafer = this.TextBoxSrc1.Text;
            var imgPathWafer = Directory.GetFiles(srcDirWafer, "*.tif").First();
            var imgWafer = new Mat(imgPathWafer, ImreadModes.Grayscale);
            var csvPathWafer = Directory.GetFiles(srcDirWafer, "*.csv").First();
            var resultsWafer = File.ReadAllLines(csvPathWafer).Skip(1).Select(c=>new ResultClass(c)).ToList();
            var colorWafer = Color.FromRgb(255, 255, 0);
            this.MainImageViewer1.SetSourceImage(imgWafer);
            resultsWafer.ForEach(r => this.MainImageViewer1.AddRectangle(r.Rect.ToInt(), colorWafer));
            this.MainImageViewer1.Draw();
            //チップ読み込み
            var srcDirChip = this.TextBoxSrc1.Text;
            var imgPathChip = Directory.GetFiles(srcDirChip, "*.tif").First();
            var imgChip = new Mat(imgPathChip, ImreadModes.Grayscale);
            var csvPathChip = Directory.GetFiles(srcDirChip, "*.csv").First();
            var resultsChip = File.ReadAllLines(csvPathChip).Skip(1).Select(c => new ResultClass(c)).ToList();
            var colorChip = Color.FromRgb(0, 255, 255);
            this.MainImageViewer2.SetSourceImage(imgChip);
            resultsChip.ForEach(r => this.MainImageViewer2.AddRectangle(r.Rect.ToInt(), colorChip));
            this.MainImageViewer2.Draw();

        }

        private void ButtonOcr_Click(object sender, RoutedEventArgs e)
        {
            var imgPath = @"I:\DevWork\results\20230127111327\20230127111327_OCR.tif";
            imgPath = @"I:\DevWork\20230127111327_OCR_processed4.tif";
            var img = new Mat(imgPath, ImreadModes.Grayscale);
            var ocr = OpenCvSharp.Text.OCRTesseract.Create(
            @"I:\DevWork\settings\tessdata",
            "eng",
                        "01234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            ocr.Run(img, out var txt, out var rects, out var components, out var confidenceVals);
            Trace.WriteLine($"{txt}, {rects}, ");

            for (int i = 0; i < rects.Length; i++) { img.Rectangle(rects[i], new Scalar(125), 2); img.PutText(components[i], rects[i].TopLeft.Add(new OpenCvSharp.Point(1,-2)), HersheyFonts.HersheyPlain,1.2, 120, 2); }
            Cv2.ImShow("a", img);
            Cv2.WaitKey();
        }

    }
}
