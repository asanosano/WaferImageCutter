using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Path = System.IO.Path;
using Size = OpenCvSharp.Size;
using Window = System.Windows.Window;
using IjhCommonUtility;
using Rect = OpenCvSharp.Rect;
using File = System.IO.File;
using RICommonWinUtility;
using MathNet.Numerics;
using Point = OpenCvSharp.Point;
using System.Runtime.Serialization;
using NetMQ.Sockets;
using NetMQ;
using static Wafer.WaferScoreCalculateClass;
using static Wafer.ProcessAndStitch;
using WaferImageCutter;

namespace Wafer
{
    [DataContract]
    public class AppProperty
    {
        [DataMember]
        public string TargetDir = "";
        [DataMember]
        public string CompareDir1 = "";
        [DataMember]
        public string CompareDir2 = "";

    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string PARAM_PATH = "waferMergeSetting.xml";
        private string APP_PROPERTY_PATH = "appProperty.xml";
        private AppProperty appProperty = new AppProperty();
        public MainWindow()
        {
            InitializeComponent();
        }

        #region テスト用
        private void ButtonStitch_Click(object sender, RoutedEventArgs e)
        {
            var srcDir = $@"E:\asano\Imgs\work\resize0.1result\corrected\20221028030854";
            var savePath = @"E:\asano\Imgs\work\panoresult\panoblended.tif";
            var files = Directory.GetFiles(srcDir, "*").OrderBy(f => f).ToList();
            var imgs = files.Select(p => new Mat(p, ImreadModes.Grayscale)).ToList();
            new ProcessAndStitch().StitchImages(imgs).stitched.SaveImage(savePath);
            imgs.ForEach(im => im.Dispose());
            return;
        }

        private void ButtonCorrect_Click(object sender, RoutedEventArgs e)
        {
            var srcDir = $@"E:\asano\Imgs\resize0.1\20221028030854";
            var files = Directory.GetFiles(srcDir, "*.tif").OrderBy(f => f).ToList();
            var csvPath = $@"E:\asano\Imgs\resize0.1\20221028030854\20221028030854_i08_Y1040000.csv";
            files.ForEach(f => new ProcessAndStitch().CorrectBrightness(f, csvPath));
        }
        private void ButtonCorrect_MakeProfile_Click(object sender, RoutedEventArgs e)
        {
            var srcImg = $@"E:\ウエハ撮像\20230118_暗視野撮像\20240117111711_6RR1000001_\20240117111711_6RR1000001__i08.tif";
            var saveDir = $@".\Settings";
            new ProcessAndStitch().CorrectBrightness_SaveProfile(srcImg, saveDir);
        }

        private void ButtonFukugen_Click(object sender, RoutedEventArgs e)
        {
            var str = @"20221028030854_i8_Y1040000_i191_X5720_Y6240_W640_H640.tif";
            var s = str.Split('_');
            var rect = new Rect(int.Parse(s[4]), int.Parse(s[5]), int.Parse(s[6]), int.Parse(s[7]));
        }

        private void ButtonCutAll_Click(object sender, RoutedEventArgs e)
        {
            var dir = @"E:\asano\Imgs\src2\wafer03";
            var dir2 = @"E:\asano\Imgs\work\cutAll\wafer03";
            new ProcessAndStitch().CutToImg(dir, dir2, 0.5);

        }

        private void ButtonKatamuki_Click(object sender, RoutedEventArgs e)
        {
            //var dirs = Directory.GetDirectories(@"I:\ウエハ撮像\残差大ワーク");
            //foreach (var dir in dirs) new ZenmenCut().Katamuki(dir);

        }

        private void ButtonTest_Click(object sender, RoutedEventArgs e)
        {
            return;
        }

        private void ButtonGan_Click(object sender, RoutedEventArgs e)
        {
            var srcDir = @"I:\DevWork\correctedResults\20230705104028_P0804_20_\movefile";
            var dstDir = @"I:\DevWork\correctedResults\20230705104028_P0804_20_\movefile3";
            var resizedDir = @"I:\DevWork\correctedResults\20230705104028_P0804_20_\movefile_resize";
            var size = new Size(320, 320);
            var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<annotation>\r\n  <folder>I:\\Develop\\データセット\\ウエハ\\320train_GAN_20230714\\OK\\_Output\\image\\シミ2</folder>\r\n  <filename>20221028030854_i8_Y1040000_i199_X2080_Y6760_W640_H640.tif</filename>\r\n  <size>\r\n    <width>320</width>\r\n    <height>320</height>\r\n    <depth>1</depth>\r\n  </size>\r\n  <object>\r\n    <bndbox>\r\n      <xmin>81</xmin>\r\n      <ymin>93</ymin>\r\n      <xmax>205</xmax>\r\n      <ymax>181</ymax>\r\n    </bndbox>\r\n    <name>Ibutsu</name>\r\n    <difficult>0</difficult>\r\n  </object>\r\n</annotation>";
            Directory.CreateDirectory(dstDir);
            Directory.CreateDirectory(resizedDir);
            var files = Directory.GetFiles(srcDir, "*.tif");
            var imgs = files.Select(f => new Mat(f, ImreadModes.Grayscale)).ToList();
            var mergeParam = new WaferMergeParam();
            DataContractReaderWriter.ReadXml_WithoutException(mergeParam, PARAM_PATH);
            var Z = new WaferInspectByDL(mergeParam.WaferInspectParamPath);
            var gans = Z.GetDlResult_Mat(imgs, size, 22, 1000);
            for (int i = 0; i < files.Length; i++)
            {
                var name = Path.GetFileNameWithoutExtension(files[i]);
                var dstPath = Path.Combine(dstDir, name + ".png");
                gans[i].SaveImage(dstPath);
                using var resized = imgs[i].Resize(size, 0, 0, InterpolationFlags.Area);
                resized.SaveImage(Path.Combine(resizedDir, name + ".png"));
                File.WriteAllText(Path.Combine(resizedDir, name + ".xml"), xml);
                (var diff, var rects) = Z.GetDetectPosition_Asano(resized, gans[i]);
                var show = MatFunctions.MakeScoreRectsInImage(diff, rects, new Scalar(255, 255, 0));
                MatFunctions.ShowImage(resized, "img");
                MatFunctions.ShowImage(gans[i], "gan");
                MatFunctions.ShowImage(show, Path.GetFileName(files[i]));
            }
        }
        private void WaferParamsRead()
        {
            var dir = @"C:\Users\r00526430\source\repos\WaferImageCutter\WaferImageCutter\bin\Debug\net7.0-windows\Settings\waferParams";
            var xmls = Directory.GetFiles(dir, "*.xml");
            foreach (var xml in xmls)
            {
                var p = new ChipScoreParameter(xml);
                p.IgnoreAreas.Add(new Rect(10, 11, 12, 13));
                p.Save();
            }
        }
        #endregion
        #region 検査アプリ
        [DataContract]
        public class WaferMergeParam
        {
            [DataMember]
            public string BrightnessProfilePath = "";
            [DataMember]
            public string InspectResultDir = "";
            [DataMember]
            public string MatchingTemplateImgPath = "";
            [DataMember]
            public double ResizeRate = 0.1;
            [DataMember]
            public double SkewCorrectParam_XPerY = 0;
            [DataMember]
            public Size CropSize = new Size(100, 100);
            [DataMember]
            public string WaferParamsDir = "";
            [DataMember]
            public string WaferInspectParamPath = "";

        }
        private async void ButtonSimpleProcess_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //DataContractReaderWriter.WriteXml_WithoutException(new WaferInspectParam(), "waferInspectSetting.xml");

                var mergeParam = new WaferMergeParam();
                DataContractReaderWriter.ReadXml_WithoutException(mergeParam, PARAM_PATH);
                var resizeRate = mergeParam.ResizeRate;
                var profilePath = mergeParam.BrightnessProfilePath;
                var templateImgPath = mergeParam.MatchingTemplateImgPath;
                var cropSize = mergeParam.CropSize;
                var skewParam = mergeParam.SkewCorrectParam_XPerY;

                var rootDir = this.TextBoxTargetDir.Text;
                var dir = rootDir;
                if (!Directory.Exists(dir))
                {
                    MessageBox.Show($"ディレクトリが存在しません：{dir}");
                    return;
                }

                //ファイル名順に並べると、撮像右側から並ぶ前提
                var files = Directory.GetFiles(dir, "*.tif").OrderBy(f => f).ToList();
                if (files.Count == 0)
                {
                    MessageBox.Show($".tifファイルが存在しません：{dir}");
                    return;
                }

                await Task.Run(() =>
                {
                    var resultDir = FileManager.GetRenamedPath_New(rootDir, "連結後画像");
                    Directory.CreateDirectory(resultDir);
                    var Z = new ProcessAndStitch();

                    //明るさ補正と画像合成
                    //var workName = Path.GetFileName(dir);
                    var tmpName = Path.GetFileName(files[0]);
                    var workName = tmpName.Split('_')[1];//zantei
                    var correcteds = new List<Mat>();
                    int fileCount = 0;
                    foreach (var file in files)
                    {
                        fileCount++;
                        Trace.WriteLine($"{file}, {DateTime.Now}");
                        using var src = new Mat(file, ImreadModes.Grayscale);
                        Trace.WriteLine($"read, {DateTime.Now}");
                        if (src.Empty()) continue;//暫定
                        using var resized = resizeRate != 1 ? src.Resize(Size.Zero, resizeRate, resizeRate, InterpolationFlags.Area)
                                                                               : src;
                        var corrected = Z.CorrectBrightness(resized, profilePath);
                        //corrected.SaveImage(Path.Combine(resultDir, Path.GetFileName(FileManager.GetRenamedPath_Add(file, "_corrected"))));
                        correcteds.Add(corrected);
                        Dispatcher.Invoke(() => this.Title = $"処理中... {fileCount}/{files.Count}");
                    }
                    Trace.WriteLine($"StitchImages..., {DateTime.Now}");
                    (var mergedImg, var locations) = Z.StitchImages(correcteds, resizeRate);

                    //中心位置算出
                    var center = new OpenCvSharp.Point(0, 0);
                    if (resizeRate < 0.3)//画像が巨大すぎるとエラーになるので、、
                    {
                        Trace.WriteLine($"GetCenter..., {DateTime.Now}");
                        center = Z.GetCenterPlace(mergedImg);
                        Trace.WriteLine($"Center:{center}");
                        //File.WriteAllLines(Path.Combine(waferResultDir, "center.csv"), new List<string>() { $"{center.X},{center.Y}" });
                    }

                    //傾き算出・補正
                    //ウエハアライメントマーク位置から傾き算出　さらに短冊Yずれを考慮
                    (var kakudo, _, _, _, _) = Z.Katamuki(dir, templateImgPath);
                    (var intercept, var slope) = Fit.Line(locations.Select(l => (double)l.X).ToArray(), locations.Select(l => (double)l.Y).ToArray());
                    var kakudo_offset = Math.Atan(slope) * 180 / Math.PI;//degree
                    var katamuki = kakudo - kakudo_offset;
                    var tmpMatrix = Cv2.GetRotationMatrix2D(center, -katamuki, 1.0);
                    using var tiltCorrectedImg = new Mat();
                    Cv2.WarpAffine(mergedImg, tiltCorrectedImg, tmpMatrix, mergedImg.Size(), InterpolationFlags.Linear, BorderTypes.Default);
                    //double skewParam_XPerY = 5.0 / 3250;
                    using var skewCorrectedImg = Z.CorrectSkew(tiltCorrectedImg, skewParam);

                    //中心から切り抜き
                    var cropRect = MatFunctions.RectFromCenterPoint(center, cropSize);
                    using var croppedImg = MatFunctions.RoiToMat(skewCorrectedImg, cropRect);
                    croppedImg.SaveImage(Path.Combine(resultDir, $"{workName}_連結.tif"));
                    correcteds.ForEach(c => c.Dispose());
                    mergedImg.Dispose();
                });
                MessageBox.Show($"終了しました。");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void ButtonAllProcess_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var paramPath = "waferMergeSetting.xml";
                //DataContractReaderWriter.WriteXml_WithoutException(new WaferInspectParam(), "waferInspectSetting.xml");

                var inspParam = new WaferMergeParam();
                DataContractReaderWriter.ReadXml_WithoutException(inspParam, paramPath);
                //var srcDir = @"I:\DevWork\20230517111719";
                var progress = new Progress<string>(s => this.Title = s);
                var rootDir = this.TextBoxTargetDir.Text;
                if (!Directory.Exists(rootDir))
                {
                    MessageBox.Show("存在しないディレクトリが指定されています。");
                    return;
                }

                var dir = "";
                var sw = Stopwatch.StartNew();
                if ((bool)this.RadioButtonMovedDir.IsChecked)
                {
                    dir = rootDir;
                    var imgPaths = Directory.GetFiles(dir, "*.tif").OrderBy(s => s).ToList();
                    if (imgPaths.Count == 0)
                    {
                        MessageBox.Show(".tifファイルが存在しません");
                        return;
                    }
                    await Task.Run(() => SequencialProcess(progress, dir, inspParam));
                }
                else if ((bool)this.RadioButtonMovedAllDir.IsChecked)
                {
                    var imgPaths = Directory.GetFiles(rootDir, "*.tif", SearchOption.TopDirectoryOnly).ToList();
                    if (imgPaths.Count > 0)
                    {
                        MessageBox.Show(".tifファイルが直下に存在しています。親ディレクトリを指定してください");
                        return;
                    }

                    var dirs = Directory.GetDirectories(rootDir);
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        progress = new Progress<string>(s => this.Title = $"dir({i + 1}/{dirs.Length}): " + s);
                        await Task.Run(() => SequencialProcess(progress, dirs[i], inspParam));
                    }

                }


                sw.Stop();
                this.Title = $"終了 time:{sw.Elapsed}s";
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public string StitchWithConstParam(string srcDir, IProgress<string> progress)
        {
            var resizeRate = 0.25;
            var zurashi = new Point(-1967, 16);//25%縮小時のパラメータ
            zurashi = new Point(zurashi.X * resizeRate / 0.25, zurashi.Y * resizeRate / 0.25);

            var Z = new ProcessAndStitch();
            var mergeParam = new WaferMergeParam();
            DataContractReaderWriter.ReadXml_WithoutException(mergeParam, PARAM_PATH);
            var imgPaths = Directory.GetFiles(srcDir, "*i*.tif");
            progress.Report(imgPaths[0]);
            var firstImg = new Mat(imgPaths[0], ImreadModes.Grayscale);
            var resizedSize = new Size(firstImg.Width * resizeRate, firstImg.Height * resizeRate);
            firstImg.Dispose();
            //位置作成
            var posList = new List<Point>();
            var pos = new Point(0, 0);
            foreach (var imgPath in imgPaths)
            {
                posList.Add(pos);
                pos = pos.Add(zurashi);
            }
            var minPos = new Point(posList.Min(p => p.X), posList.Min(p => p.Y));
            posList = posList.Select(p => p.Subtract(minPos)).ToList();
            var maxPos = new Point(posList.Max(p => p.X), posList.Max(p => p.Y));
            var canvasSize = new Size(maxPos.X + resizedSize.Width, maxPos.Y + resizedSize.Height);
            using var canvas = new Mat(canvasSize, MatType.CV_8UC1, new Scalar(0));
            for (int i = 0; i < imgPaths.Length; i++)
            {
                progress.Report(imgPaths[i]);
                Trace.WriteLine($"img:{i+1}/{imgPaths.Length}");
                using var img = new Mat(imgPaths[i], ImreadModes.Grayscale);
                using var resized = img.Resize(resizedSize, interpolation:InterpolationFlags.Area);
                using var corrected = Z.CorrectBrightness(resized, mergeParam.BrightnessProfilePath);
                var roiRect = new Rect(posList[i], resizedSize);
                var roi = new Mat(canvas, roiRect);
                corrected.CopyTo(roi);
            }
            var imgName = Path.GetFileName(srcDir);
            var savePath = Path.Combine(Path.GetDirectoryName(srcDir), $"{imgName}_stitched.tif");
            progress.Report($"Save...{savePath}");
            Trace.WriteLine($"Save...{savePath}");
            canvas.SaveImage(savePath);
            canvas.Resize(new Size(1000,1000),interpolation:InterpolationFlags.Area)
                      .SaveImage(FileManager.GetRenamedPath_Add(savePath, "_mini"));
            return savePath;
        }
        async public Task<bool> CheckServer(int waitSeconds)
        {
            var param = new WaferMergeParam();
            DataContractReaderWriter.ReadXml_WithoutException(param, "waferMergeSetting.xml");

            var tcpAdress = new WaferInspectByDL(param.WaferInspectParamPath).InspParam.port;
            try
            {
                this.ButtonCheckDlServer.Content = "サーバ通信: 確認中..";
                //サーバーに接続テスト
                var isConnected = await Task.Run(() =>
                {
                    //var context = new ZContext();
                    using (var socket = new RequestSocket())
                    {
                        //var poll = ZPollItem.CreateReceiver();
                        socket.Connect(tcpAdress);
                        //socket.Connect("tcp://133.139.81.94:5556");
                        var frames = new byte[4];
                        frames[0] = 99;
                        socket.SendMoreFrame(frames);
                        var frames1 = new byte[4];
                        frames1[0] = 1;
                        socket.SendMoreFrame(frames1);
                        var frames2 = new byte[4];
                        frames2[0] = 1;
                        socket.SendMoreFrame(frames2);
                        var frames3 = new byte[4];
                        frames3[0] = 1;
                        socket.SendMoreFrame(frames3);
                        var frames4 = new byte[4];
                        frames4[0] = 1;
                        socket.SendMoreFrame(frames4);
                        var frames5 = new byte[4];
                        frames5[0] = 1;
                        socket.SendFrame(frames5);

                        //socket.SendFrame(frames);
                        Trace.WriteLine("sent: ");

                        PollEvents pollEvents = PollEvents.PollIn;
                        if (socket.Poll(pollEvents, TimeSpan.FromSeconds(waitSeconds)) == PollEvents.PollIn)
                        {
                            Trace.WriteLine($"receive:");
                            return true;
                        }
                        else return false;
                    }
                });


                if (isConnected)
                {
                    this.ButtonCheckDlServer.Content = "サーバ通信: OK";
                    this.ButtonCheckDlServer.Background = new SolidColorBrush(Colors.LightGreen);
                }
                else
                {
                    this.ButtonCheckDlServer.Content = "サーバ通信: NG";
                    this.ButtonCheckDlServer.Background = new SolidColorBrush(Colors.Red);
                }
                return isConnected;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
                throw;
            }
        }

        private async void ButtonCheckDlServer_Click(object sender, RoutedEventArgs e)
        {
            var logPath = "connectLog.txt";
            int timeSpanSecond = 300;

            while (true)
            {
                var isConnected = await this.CheckServer(10);
                var txt = new List<string>();
                if (isConnected) txt.Add($"Connected:{DateTime.Now}");
                else txt.Add($"Error!!!:{DateTime.Now}");
                File.AppendAllLines(logPath, txt);
                await Task.Delay(TimeSpan.FromSeconds(timeSpanSecond));
            }

        }
        private void ButtonCompare_Click(object sender, RoutedEventArgs e)
        {
            var src1 = this.TextBoxCompareDir1.Text;
            var src2 = this.TextBoxCompareDir2.Text;
            if (src1 == src2)
            {
                MessageBox.Show("両方のフォルダが同じです。", "Error!!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!src1.Contains("Chip") && !src2.Contains("Chip"))
            {
                MessageBox.Show("チップ結果ではないフォルダが指定されています(たぶん)", "Error!!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            this.CompareChipOfWafers(src1, src2);

        }
        public class LabelWithResult
        {
            public int Label;
            public ResultClass Result;
            public LabelWithResult(int label, ResultClass result)
            {
                Label = label;
                Result = result;
            }
        }
        private (List<int> connects1, List<int> connects2) GetOverlappedRect(List<Rect2d> rects1, List<Rect2d> rects2, Size2d marginSize, double intersectRate = 0.5)
        {
            int labelNum = 0;
            bool isNewLabel = false;
            var labels1 = new List<int>();
            for (int i = 0; i < rects1.Count; i++) labels1.Add(-1);
            var labels2 = new List<int>();
            for (int i = 0; i < rects2.Count; i++) labels2.Add(-1);

            for (int i = 0; i < rects1.Count; i++)
            {
                for (int j = 0; j < rects2.Count; j++)
                {
                    if (rects1[i].AddSize(marginSize).IntersectsMore_Any(rects2[j], intersectRate))
                    {
                        if (labels2[j] == -1)
                        {
                            labels1[i] = labelNum;
                            labels2[j] = labelNum;
                            isNewLabel = true;
                        }
                        else
                        {
                            labels1[i] = labels2[j];
                        }
                    }
                }
                if (isNewLabel)
                {
                    labelNum++;
                    isNewLabel = false;
                }

            }
            return (labels1, labels2);
        }

        private void CompareChipOfWafers(string src1, string src2)
        {
            try
            {
                double intersectRate = 0.01;
                int margin = 20;
                string searchImgStr = "*score.tif";
                string searchResultStr = "result*.csv";
                (var img1, var results1) = ReadImgAndResults(src1);
                (var img2, var results2) = ReadImgAndResults(src2);
                if (img1 == null || img2 == null) return;
                var saveDir = Path.Combine(Path.GetDirectoryName(src1), "差分比較結果_" + Path.GetFileName(src1));
                Directory.CreateDirectory(saveDir);
                var outputCsvPath = Path.Combine(saveDir, "result_compare.csv");
                var outputImgPath = Path.Combine(saveDir, "score_compare.tif");

                (var labels1, var labels2) = this.GetOverlappedRect(results1.Select(r => r.Rect).ToList(), results2.Select(r => r.Rect).ToList(), new Size2d(margin, margin), intersectRate);
                var labelWithResults1 = results1.Zip(labels1, (r, l) => (Result: r, Label: l)).ToList();
                var labelWithResults2 = results2.Zip(labels2, (r, l) => (Result: r, Label: l)).ToList();

                //src1,2の情報を付与
                var labelWithResultsAll = new List<(ResultClass Result, int Label, int Src)>();
                labelWithResultsAll.AddRange(labelWithResults1.Select(l => (l.Result, l.Label, 1)));
                labelWithResultsAll.AddRange(labelWithResults2.Select(l => (l.Result, l.Label, 2)));
                var overlapped = labelWithResultsAll.OrderBy(l => l.Label).Where(l => l.Label != -1);
                var unique = labelWithResultsAll.OrderBy(l => l.Label).Where(l => l.Label == -1);

                //csv保存 重複を先に
                var csvData = new List<string>() { "差分比較," + ResultClass.GetHeader() };
                csvData.AddRange(overlapped.Select(o => $"重複{o.Label}_src{o.Src}," + o.Result.GetString()));
                csvData.AddRange(unique.Select(o => $"src{o.Src}," + o.Result.GetString()));
                File.WriteAllLines(outputCsvPath, csvData, Encoding.UTF8);

                //画像保存
                var overlapped1 = overlapped.Where(o => o.Src == 1).Select(o => o.Result).ToList();
                var overlapped2 = overlapped.Where(o => o.Src == 2).Select(o => o.Result).ToList();
                var unique1 = unique.Where(u => u.Src == 1).Select(u => u.Result).ToList();
                var unique2 = unique.Where(u => u.Src == 2).Select(u => u.Result).ToList();
                var scoreImg = new Mat();
                img1.ConvertTo(scoreImg, MatType.CV_8UC3);
                if (overlapped1.Count > 0) ResultClass.DrawResults(ref scoreImg, overlapped1, new Scalar(255, 0, 255));
                if (overlapped2.Count > 0) ResultClass.DrawResults(ref scoreImg, overlapped2, new Scalar(255, 255, 0));
                if (unique1.Count() > 0) ResultClass.DrawResults(ref scoreImg, unique1, new Scalar(0, 0, 255));
                if (unique2.Count() > 0) ResultClass.DrawResults(ref scoreImg, unique2, new Scalar(255, 0, 0));
                scoreImg.SaveImage(outputImgPath);

                img1.Dispose();
                img2.Dispose();
                scoreImg.Dispose();
                MessageBox.Show($"終了しました。{outputCsvPath}");

                (Mat? img, List<ResultClass>? results) ReadImgAndResults(string dirPath)
                {
                    if (!Directory.Exists(dirPath))
                    {
                        MessageBox.Show($"ディレクトリが見つかりません:{dirPath}");
                        return (null, null);
                    }
                    var file1 = Directory.GetFiles(dirPath, searchImgStr).FirstOrDefault();
                    var file2 = Directory.GetFiles(dirPath, searchResultStr).FirstOrDefault();
                    if (file1 == null)
                    {
                        MessageBox.Show($"ディレクトリ内に{searchImgStr}のファイルが見つかりません");
                        return (null, null);
                    }
                    else if (file2 == null)
                    {
                        MessageBox.Show($"ディレクトリ内に{searchImgStr}のファイルが見つかりません");
                        return (null, null);
                    }
                    else
                    {
                        var img = new Mat(file1);
                        var csv = File.ReadAllLines(file2).Skip(1).Select(f => new ResultClass(f)).ToList();
                        return (img, csv);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ButtonXlsx_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var srcDir = this.TextBoxCompareDir1.Text;
                var dstDir = this.TextBoxCompareDir2.Text;
                var dir = WaferInspectCommon.ChipResultToWaferResult(srcDir, dstDir);
                if (dir == "") MessageBox.Show("対象ディレクトリ1の中に必要なファイルがありません");
                else
                {
                    this.TextBoxCompareDir2.Text = dir;
                    MessageBox.Show("終了しました。\n対象ディレクトリ2に出力先ディレクトリをセットしました。");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ButtonConvertAndCompare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var srcDir1 = this.TextBoxCompareDir1.Text;
                var srcDir2 = this.TextBoxCompareDir2.Text;
                var convertedDirBase = Path.Combine(Path.GetDirectoryName(srcDir1), "差分比較結果_" + Path.GetFileName(srcDir1));//差分比較結果と同じ場所に変換結果を出力
                Directory.CreateDirectory(convertedDirBase);
                this.Title = "変換中...";
                var convertedDir = await Task.Run(() => WaferInspectCommon.ChipResultToWaferResult(srcDir2, convertedDirBase));
                this.Title = "差分比較中...";
                if (convertedDir == "") MessageBox.Show("対象ディレクトリ2の中に必要なファイルがありません");
                else
                {
                    this.CompareChipOfWafers(srcDir1, convertedDir);
                }
                this.Title = $"差分比終了:{DateTime.Now}";

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void ButtonWaferCompare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var srcDir1 = this.TextBoxCompareDir1.Text;
                var srcDir2 = this.TextBoxCompareDir2.Text;
                this.Title = "ウエハ差分比較中...";
                //new WaferCompareParameter(@"Settings\waferCompareParameter.xml").Save();
                var C = new WaferCompare(@"Settings\waferCompareParameter.xml");
                await Task.Run(() => C.Compare(srcDir1, srcDir2));
                this.Title = "ウエハ差分比較終了.";
                MessageBox.Show($"終了しました。");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString(), "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private ResultClass FromDetailResultToResultClass(string csvStr)
        {
            var tmp = csvStr.Split(',');
            var StripId = int.TryParse(tmp[0], out var w) ? w : 0;
            var ChipName = tmp[0];
            var AreaName = tmp[1];
            var Rect = new Rect2d(double.Parse(tmp[2]), double.Parse(tmp[3]), double.Parse(tmp[4]), double.Parse(tmp[5]));
            var Score = double.Parse(tmp[6]);
            var DefectId = int.TryParse(tmp[7], out var d) ? d : 0;
            var ImgPath = tmp[8];
            return new ResultClass(99, ChipName, AreaName, DefectId, Rect, Score, ImgPath);
        }
        private void CompareWafer(string srcDir1, string srcDir2)
        {
            //var csvName = "*detailResult_WithoutIgnore.csv";
            var csvName = "*detailResult.csv";
            var margin = 20;
            var intersectRate = 0.0;
            var csvpath1 = Directory.GetFiles(srcDir1, csvName).FirstOrDefault();
            var csvpath2 = Directory.GetFiles(srcDir2, csvName).FirstOrDefault();
            var outputPathCsv = Path.Combine(srcDir1, $"compareWaferWith_{Path.GetFileName(srcDir2)}.csv");
            var outputPathImg = Path.Combine(srcDir1, $"compareWaferWith_{Path.GetFileName(srcDir2)}.tif");
            var results1 = File.ReadAllLines(csvpath1).Skip(1).Select(c => FromDetailResultToResultClass(c));
            var results2 = File.ReadAllLines(csvpath2).Skip(1).Select(c => FromDetailResultToResultClass(c));
            (var labels1, var labels2) = this.GetOverlappedRect(results1.Select(r => r.Rect).ToList(), results2.Select(r => r.Rect).ToList(), new Size2d(margin, margin), intersectRate);
            var labelWithResults1 = results1.Zip(labels1, (r, l) => (Result: r, Label: l)).ToList();
            var labelWithResults2 = results2.Zip(labels2, (r, l) => (Result: r, Label: l)).ToList();
            //src1,2の情報を付与
            var labelWithResultsAll = new List<(ResultClass Result, int Label, int Src)>();
            labelWithResultsAll.AddRange(labelWithResults1.Select(l => (l.Result, l.Label, 1)));
            labelWithResultsAll.AddRange(labelWithResults2.Select(l => (l.Result, l.Label, 2)));
            var overlapped = labelWithResultsAll.OrderBy(l => l.Label).Where(l => l.Label != -1);
            var unique = labelWithResultsAll.OrderBy(l => l.Label).Where(l => l.Label == -1);

            //csv保存
            var csvData = new List<string>() { "差分比較," + ResultClass.GetHeader() };
            csvData.AddRange(unique.Select(o => $"src{o.Src}," + o.Result.GetString()));
            csvData.AddRange(overlapped.Select(o => $"重複{o.Label}_src{o.Src}," + o.Result.GetString()));
            File.WriteAllLines(outputPathCsv, csvData, Encoding.UTF8);

            //画像保存
            var imgName = "*score.tif";
            var imgPath = Directory.GetFiles(srcDir1, imgName).FirstOrDefault();
            var img = new Mat(imgPath, ImreadModes.Color);
            var overlapped1 = overlapped.Where(o => o.Src == 1).Select(o => o.Result).ToList();
            var overlapped2 = overlapped.Where(o => o.Src == 2).Select(o => o.Result).ToList();
            var unique2 = unique.Where(u=>u.Src==2).Select(o => o.Result).ToList();
            if (overlapped1.Count > 0) ResultClass.DrawResults(ref img, overlapped1, new Scalar(255, 255, 0));
            if (overlapped2.Count > 0) ResultClass.DrawResults(ref img, overlapped2, new Scalar(0, 255, 255));
            if (unique2.Count() > 0) ResultClass.DrawResults(ref img, unique2, new Scalar(0, 255, 0));
            img.SaveImage(outputPathImg);
        }
        #endregion

        private void ButtonParamModify_Click(object sender, RoutedEventArgs e)
        {
            var dirPath = @"C:\Users\r00526430\source\repos\WaferImageCutter\WaferImageCutter\bin\Debug\net7.0-windows\Settings\waferParams";
            var buttonResult = MessageBox.Show($"このディレクトリに対して実行されます（ファイルを書き換えるので、バックアップ推奨）。{dirPath}", "question",MessageBoxButton.OKCancel);
            if (buttonResult == MessageBoxResult.Cancel) return;
            var files = Directory.GetFiles(dirPath, "*Chip*.xml");
            foreach (var file in files)
            {
                var param = new ChipScoreParameter(file);
                //やる操作

                //シフト
                var shiftX = -208;
                var shiftY = -287;
                param.Area = new System.Windows.Rect(param.Area.X + shiftX, param.Area.Y + shiftY, param.Area.Width, param.Area.Height);

                //全体倍率変更
                //var bairitsu = 0.25 / 0.1;
                //param.Area = new System.Windows.Rect(param.Area.X*bairitsu, param.Area.Y*bairitsu, param.Area.Width*bairitsu, param.Area.Height*bairitsu);
                //param.AreaInChips.ForEach(a => a.Area_InChip = a.Area_InChip.ToCvRect().ResizeFromZero(bairitsu).ToWindowsRect());
                //param.IgnoreAreas = param.IgnoreAreas.Select(a => a.ResizeFromZero(bairitsu)).ToList();

                //領域のサイズ変更
                //var reduceSize = new Size(0, -20);
                //var w = param.AreaInChips.Where(a => a.Name == "WithoutBridge").ToList();
                //w.ForEach(w => w.Area_InChip = Rect2d.Inflate(w.Area_InChip.ToCvRect(), reduceSize.Width, reduceSize.Height).ToWindowsRect());

                //除外領域をなくす

                param.Save();
            }
        }

        private async void ButtonConstProcess_Click(object sender, RoutedEventArgs e)
        {
            var rootDir = this.TextBoxTargetDir.Text;
            if (!Directory.Exists(rootDir))
            {
                MessageBox.Show("存在しないディレクトリが指定されています。");
                return;
            }

            var dir = "";
            var sw = Stopwatch.StartNew();
            if ((bool)this.RadioButtonMovedDir.IsChecked)
            {
                dir = rootDir;
                var imgPaths = Directory.GetFiles(dir, "*.tif").OrderBy(s => s).ToList();
                if (imgPaths.Count == 0)
                {
                    MessageBox.Show(".tifファイルが存在しません");
                    return;
                }
                var progress = new Progress<string>(s => this.Title = "連結中..."+s);
                var savePath = await Task.Run(() => this.StitchWithConstParam(rootDir, progress));
                MessageBox.Show($"連結しました。{savePath}");
            }
            else if ((bool)this.RadioButtonMovedAllDir.IsChecked)
            {
                var imgPaths = Directory.GetFiles(rootDir, "*.tif", SearchOption.TopDirectoryOnly).ToList();
                if (imgPaths.Count > 0)
                {
                    MessageBox.Show(".tifファイルが直下に存在しています。親ディレクトリを指定してください");
                    return;
                }

                var dirs = Directory.GetDirectories(rootDir);
                for (int i = 0; i < dirs.Length; i++)
                {
                    var progress = new Progress<string>(s => this.Title = $"dir({i + 1}/{dirs.Length}): " + s);
                    var savePath = await Task.Run(() => this.StitchWithConstParam(dirs[i], progress));
                }
            }
            this.Title = $"連結終了しました。{sw.Elapsed}";
        }

        private void MainWindow1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.appProperty.TargetDir = this.TextBoxTargetDir.Text;
            this.appProperty.CompareDir1 = this.TextBoxCompareDir1.Text;
            this.appProperty.CompareDir2 = this.TextBoxCompareDir2.Text;
            DataContractReaderWriter.WriteXml_WithoutException(this.appProperty, APP_PROPERTY_PATH);
        }

        private void MainWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            DataContractReaderWriter.ReadXml_WithoutException(this.appProperty, APP_PROPERTY_PATH);
            this.TextBoxTargetDir.Text = this.appProperty.TargetDir;
            this.TextBoxCompareDir1.Text = this.appProperty.CompareDir1;
            this.TextBoxCompareDir2.Text = this.appProperty.CompareDir2;
        }
    }
}
