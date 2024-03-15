using IjhCommonUtility;
using MathNet.Numerics;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Wafer.MainWindow;
using System.Windows.Threading;
using System.Diagnostics;

namespace Wafer
{
    //画像読み込み～検査～連結～集計　の一連のプログラム MainWindowのpartial classとして定義
    partial class MainWindow
    {
        public void SequencialProcess(IProgress<string> progress, string srcWaferDir, WaferMergeParam param)
        {
            bool isSkipInspect = false;
            Dispatcher.Invoke(() => isSkipInspect = (bool)this.CheckBoxSkipInspect.IsChecked);
            //new WaferInspectParameter().Save(param.WaferInspectParamPath);
            var Z = new ProcessAndStitch();//暫定措置
            double resizeRate = param.ResizeRate;
            double XPerY = param.SkewCorrectParam_XPerY;
            var cropSize = param.CropSize;
            string inspectResultDir = param.InspectResultDir;
            string profilePath = param.BrightnessProfilePath;
            string templateImgPath = param.MatchingTemplateImgPath;
            string waferParamsDir = param.WaferParamsDir;
            var W = new WaferInspectByBinary(param.WaferInspectParamPath);
            //var W = new WaferInspectByDL(param.WaferInspectParamPath);//パラメータ読込先も変更すること

            //元画像を使う処理（検査、傾き算出）
            //省メモリ化のため こまめにDisposeする
            Trace.WriteLine($"処理開始:{DateTime.Now}");
            progress.Report("0.画像読み込み中...");
            var resizedImgs = new List<Mat>();
            var imgPaths = Directory.GetFiles(srcWaferDir, "*.tif").OrderBy(s => s).ToList();//ファイル名順に並べると、撮像右側から並ぶ前提
            var waferName = Path.GetFileName(Path.GetDirectoryName(imgPaths[0]));
            var waferResultDir = Path.Combine(inspectResultDir, waferName);
            Directory.CreateDirectory(waferResultDir);
            var leftAlignmentImg = new Mat();
            var rightAlignmentImg = new Mat();
            for (int i = 0; i < imgPaths.Count; i++)
            {
                progress.Report($"1.検査中...{i + 1}/{imgPaths.Count}");
                var img = new Mat(imgPaths[i], ImreadModes.Grayscale);
                if (isSkipInspect) Trace.WriteLine($"i{i}----検査をスキップします----");
                //else W.InspectWafer_Part(img, imgPaths[i], waferResultDir, i);
                else W.InspectWafer_Rule(img, imgPaths[i], waferResultDir, i, Z.CutParams[i]);
                resizedImgs.Add(img.Resize(Size.Zero, resizeRate, resizeRate, InterpolationFlags.Area));
                if (i == 1) rightAlignmentImg = img;
                else if (i == 14) leftAlignmentImg = img;
                else img.Dispose();
            }
            (var kakudo, var matchPointLeft, var matchPointRight, var matchImgLeft, var matchImgRight) = Z.Katamuki_FromImg(leftAlignmentImg, rightAlignmentImg, templateImgPath);
            leftAlignmentImg.Dispose();
            rightAlignmentImg.Dispose();

            //明るさ補正と画像合成
            progress.Report("2.明るさ補正中...");
            var workName = Path.GetFileName(srcWaferDir);
            var correcteds = new List<Mat>();
            for (int i = 0; i < resizedImgs.Count; i++)
            {
                var corrected = Z.CorrectBrightness(resizedImgs[i], profilePath);
                //corrected.SaveImage(Path.Combine(waferResultDir, $"strip{i}.tif"));
                correcteds.Add(corrected);
            }
            progress.Report("3.連結中...");
            Trace.WriteLine($"StitchImages..., {DateTime.Now}");
            (var mergedImg, var locations) = Z.StitchImages(correcteds, resizeRate);
            correcteds.ForEach(c => c.Dispose());
            var stitchLocationCsv = new List<string>() { "X,Y" };
            stitchLocationCsv.AddRange(locations.Select(l => $"{l.X},{l.Y}"));
            File.WriteAllLines(Path.Combine(waferResultDir, "stitchLocations.csv"), stitchLocationCsv, Encoding.UTF8);

            //結果のマージ
            var inspDirs = Directory.GetDirectories(waferResultDir).OrderBy(d => d);
            var globalResults = inspDirs.Where(d => File.Exists(Path.Combine(d, "result.csv")))
                                                    .SelectMany(d => File.ReadAllLines(Path.Combine(d, "result.csv")).Skip(1)
                                                                                                                                                    .Select(f => new ResultClass(f))
                                                                                                                                                    .Select(f => new ResultClass(f.StripId, f.ChipName, f.AreaName, f.DefectId, f.Rect.ResizeFromZero(resizeRate), f.Score, f.ImgPath))
                                                                                                                                                    .Select(f => new ResultClass(f.StripId, f.ChipName, f.AreaName, f.DefectId, f.Rect.Add(locations[f.StripId]), f.Score, f.ImgPath)));
            File.WriteAllLines(Path.Combine(waferResultDir, "mergedResults.csv"), globalResults.Select(g => g.GetString()), Encoding.UTF8);

            //中心位置算出
            progress.Report("4.連結後補正中...");
            var center = new OpenCvSharp.Point(0, 0);
            if (resizeRate < 0.3)//画像が巨大すぎるとエラーになるので、、
            {
                Trace.WriteLine($"GetCenter..., {DateTime.Now}");
                center = Z.GetCenterPlaceFromMark(matchPointLeft, matchPointRight, locations, resizeRate);
                Trace.WriteLine($"Center:{center}");
                File.WriteAllLines(Path.Combine(waferResultDir, "center.csv"), new List<string>() { $"{center.X},{center.Y}" }, Encoding.UTF8);
            }

            //傾き算出・補正
            //ウエハアライメントマーク位置から傾き算出　さらに短冊Yずれを考慮
            //(var kakudo, var matchPointLeft, var matchPointRight, var matchImgLeft, var matchImgRight) = Z.Katamuki_FromImg(imgs[14], imgs[1], templateImgPath);
            matchImgLeft.SaveImage(Path.Combine(waferResultDir, $"matchLeft_X{matchPointLeft.X}_Y{matchPointLeft.Y}.tif"));
            matchImgRight.SaveImage(Path.Combine(waferResultDir, $"matchRight_X{matchPointRight.X}_Y{matchPointRight.Y}.tif"));
            (var intercept, var slope) = Fit.Line(locations.Select(l => (double)l.X).ToArray(), locations.Select(l => (double)l.Y).ToArray());//短冊Yずれ グラフと画像でY軸の向きが逆なのに注意
            var kakudo_offset = Math.Atan(slope) * 180 / Math.PI;//degree
            var katamuki = kakudo + kakudo_offset;
            var tmpMatrix = Cv2.GetRotationMatrix2D(center, katamuki, 1.0);//このメソッドは反時計回りの角度[deg]指定
            using var tiltCorrectedImg = new Mat();
            Cv2.WarpAffine(mergedImg, tiltCorrectedImg, tmpMatrix, mergedImg.Size(), InterpolationFlags.Linear, BorderTypes.Default);
            using var skewCorrectedImg = Z.CorrectSkew(tiltCorrectedImg, XPerY);
            var cropRect = MatFunctions.RectFromCenterPoint(center, cropSize);
            using var croppedImg = MatFunctions.RoiToMat(skewCorrectedImg, cropRect);
            //結果のRectも同様に移動
            var radian = katamuki / 180 * Math.PI;
            var correctedResults = globalResults.Select(g => new ResultClass(g.StripId, g.ChipName, g.AreaName, g.DefectId, MatFunctions.RotateRectFromPoint_Clockwise(g.Rect, center, -radian)//反時計回り→時計回りの角度に
                                                                                                                            .Add(new Point(XPerY * (g.Rect.Y - skewCorrectedImg.Height / 2), 0))
                                                                                                                            .Subtract(cropRect.Location), g.Score, g.ImgPath))
                                                                 .ToList();

            //ウエハ良品スコア算出
            progress.Report("5.スコア画像作成中...");
            var center_cropped = center.Subtract(cropRect.Location);
            var xmlPaths = Directory.GetFiles(waferParamsDir);
            var waferScore = new WaferScoreCalculateClass.WaferScore(xmlPaths.ToList(), center_cropped);
            //double paramResize = param.ResizeRate / 0.1;//暫定　縮小0.1倍でパラメータ作ったので
            //waferScore.ResizeParamRects(paramResize, center_cropped);
            (var score, var chipResults) = waferScore.Calculate(correctedResults);
            Trace.WriteLine($"waferScore:{score}");
            waferScore.MakeCsv_Total(chipResults, Path.Combine(waferResultDir, $"{workName}_totalResult.csv"));
            waferScore.MakeCsv_Detail(chipResults, Path.Combine(waferResultDir, $"{workName}_detailResult.csv"));
            //var scoreImg = MatFunctions.MakeScoreRectsInImage(croppedImg, correctedResults.Select(g => g.Rect).ToList(), Scalar.Red, correctedResults.Select(g => g.Score).ToList());
            var scoreImg = MatFunctions.MakeScoreRectsInImage(croppedImg, waferScore.OutOfChipResults.Select(o => o.Rect.ToInt()).ToList(), Scalar.Red, waferScore.OutOfChipResults.Select(o => o.Score).ToList());
            //チップ領域表示（緑）
            MatFunctions.MakeScoreRectsInImage(ref scoreImg, waferScore.ChipScores.Select(c => c.ChipArea_Global).ToList(), Scalar.Green, null, waferScore.ChipScores.Select(c => c.Name).ToList());
            MatFunctions.MakeScoreRectsInImage(ref scoreImg, waferScore.ChipScores.SelectMany(o => o.OutOfAreaResults.Select(a => a.Rect.ToInt())).ToList(), Scalar.Green, waferScore.ChipScores.SelectMany(o => o.OutOfAreaResults.Select(a => a.Score)).ToList());
            //アイランド・フィルタ部表示（青）
            MatFunctions.MakeScoreRectsInImage(ref scoreImg, waferScore.ChipScores.SelectMany(c => c.AreaInChips.Select(a => a.Area_Global)).ToList(), Scalar.Blue, null, waferScore.ChipScores.SelectMany(c => c.AreaInChips.Select(a => a.Name)).ToList());
            ResultClass.DrawResults(ref scoreImg, waferScore.ChipScores.SelectMany(c => c.AreaInChips.SelectMany(a => a.Results)).ToList(), Scalar.Blue);
            //無視エリア（黄色）
            MatFunctions.MakeScoreRectsInImage(ref scoreImg, waferScore.ChipScores.SelectMany(c => c.IgnoreAreas.Select(i => i.Add(c.ChipArea_Global.Location))).ToList(), Scalar.Yellow, null, waferScore.ChipScores.SelectMany(c => c.IgnoreAreas.Select(i => "_Ignore")).ToList());
            progress.Report("6.画像保存中...");
            Trace.WriteLine($"Save..., {DateTime.Now}");
            scoreImg.SaveImage(Path.Combine(waferResultDir, $"{workName}_score.tif"));
            scoreImg.Dispose();

            progress.Report("7.チップ結果保存中...");
            waferScore.MakeChipResult(waferScore.ChipScores, croppedImg, waferResultDir);

            //スキューのチェック用
            var checkImg = new Mat(croppedImg, MatFunctions.RectFromCenterPoint(croppedImg.Width / 2, croppedImg.Height / 2, 1000, 10000)).Resize(new Size(1000, 1000));
            var c = new Point(checkImg.Width / 2, checkImg.Height / 2);
            Cv2.Line(checkImg, new Point(c.X, 0), new Point(c.X, checkImg.Height), new Scalar(255), 2);
            Cv2.Line(checkImg, new Point(0, c.Y), new Point(checkImg.Width, c.Y), new Scalar(255), 2);
            checkImg.SaveImage(Path.Combine(waferResultDir, $"check.tif"));

            //途中経過イメージ確認用
            //mergedImg.SaveImage(Path.Combine(waferResultDir, $"{workName}_stitched.tif"));
            //tiltCorrectedImg.SaveImage(Path.Combine(waferResultDir, $"{workName}_stitched_tiltcorrected.tif"));
            //croppedImg.SaveImage(Path.Combine(waferResultDir, $"{workName}_stitched_cropped.tif"));
            mergedImg.Resize(Size.Zero, 0.05, 0.05, InterpolationFlags.Area).SaveImage(Path.Combine(waferResultDir, $"{workName}_mini.tif"));
            mergedImg.Dispose();

            Trace.WriteLine($"End., {DateTime.Now}");
        }
    }
}
