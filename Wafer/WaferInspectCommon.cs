using IjhCommonUtility;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wafer.ChipResult;

namespace Wafer
{
    internal static class WaferInspectCommon
    {
        public static List<Rect> CutToRect(Size srcSize, Size rectSize, Size marginSize)
        {
            //パラメータ　ベタ書き
            //切り出し初期位置のオフセットのバリエーション
            var StartPoints = new List<Point>() { new Point(0, 0) };//, new Point(80, 80), new Point(160, 160), new Point(320, 320) };
            //切り出し枠サイズ
            Size RectSize = rectSize;//new Size(640, 640);
            //切り出し枠の重複
            //Size Margin = new Size(240, 240);
            Size Margin = marginSize;//new Size(320, 320);

            var result = new List<Rect>();
            Size Step = new Size(RectSize.Width - Margin.Width, RectSize.Height - Margin.Height);
            int MaxX = srcSize.Width - RectSize.Width;
            int MaxY = srcSize.Height - RectSize.Height;
            foreach (var start in StartPoints)
            {
                //端部も漏れなく埋めるように切り出す
                for (int y = start.Y; y < srcSize.Height - Step.Height; y += Step.Height)
                {
                    for (int x = start.X; x < srcSize.Width - Step.Width; x += Step.Width)
                    {
                        var xx = x < MaxX ? x : MaxX;
                        var yy = y < MaxY ? y : MaxY;
                        result.Add(new Rect(xx, yy, RectSize.Width, RectSize.Height));
                    }
                }
            }
            return result;
        }
        public static void CutToImg(Mat src, Size rectSize, Size marginSize, string saveDir, string fileName, int avoidBlackImgThresh = 50, double resizeRate = 1.0, string ext = ".tif")
        {
            var rects = CutToRect(src.Size(), rectSize, marginSize);
            for (int i = 0; i < rects.Count; i++)
            {
                var r = rects[i];
                var savePath = Path.Combine(saveDir, $"{fileName}_i{i}_X{r.X}_Y{r.Y}_W{r.Width}_H{r.Height}.tif");
                if (resizeRate == 1.0)
                {
                    new Mat(src, r).SaveImage(savePath);
                }
                else
                {
                    try
                    {
                        int resizedWidth = (int)(r.Width * resizeRate + 0.5);
                        int resizedHeight = (int)(r.Height * resizeRate + 0.5);
                        var mat = new Mat(src, r).Resize(new Size(resizedWidth, resizedHeight), 0, 0, InterpolationFlags.Area);
                        if (mat.Mean()[0] > avoidBlackImgThresh) mat.SaveImage(savePath);//真っ黒画像を除外する
                    }
                    catch { Trace.WriteLine($"Error:i={i}"); }

                }
            }
        }
        public static string ChipResultToWaferResult(string srcDir, string dstDir, string dstDirName = "")
        {
            int ChipCenterFromMaruana = 20443;
            Size ImgSize = new Size(3660, 11967);
            Size DefectImgSize = new Size(320, 320);
            double resizeRate = 0.25;
            var N1ResultPath = Directory.GetFiles(srcDir, "*N1*_judged.xlsx").FirstOrDefault();
            var N1ImgPath = Directory.GetFiles(srcDir, "*N1*.png").FirstOrDefault();
            var N2ResultPath = Directory.GetFiles(srcDir, "*N2*_judged.xlsx").FirstOrDefault();
            var N2ImgPath = Directory.GetFiles(srcDir, "*N2*.png").FirstOrDefault();
            if (N1ResultPath == null || N1ImgPath == null || N2ResultPath == null || N2ImgPath == null)
            {
                return "";
            }
            var dstDir1 = dstDirName == "" ? Path.Combine(dstDir, Path.GetFileName(srcDir)) : Path.Combine(dstDir, dstDirName);
            var dstDir2 = Path.Combine(dstDir1, "defectImgs");
            var dstDir3 = Path.Combine(dstDir1, "microscopeImgs");
            Directory.CreateDirectory(dstDir2);
            Directory.CreateDirectory(dstDir3);

            (Mat img, List<ResultClass> results) GetImgAndResults(string xlsxPath, string imgPath, bool isRight)
            {
                var microscopeDir = Path.Combine(Path.GetDirectoryName(xlsxPath), "MicroscopeImages");
                (var tmpBp, var results) = ExcelWriter.ReadIjhXlsx(xlsxPath);
                using var img = new Mat(imgPath, ImreadModes.Grayscale);
                var bp = new Point(tmpBp.X, tmpBp.Y + ChipCenterFromMaruana);//丸穴位置→ワーク中心位置に
                var tmpResults = results.Where(r => r.IsGetSecondImage).ToList();
                //1次画像保存
                tmpResults.ForEach(r => new Mat(img, MatFunctions.RectFromCenterPoint(r.ScoreRect_GlobalPos.Center(), DefectImgSize).FitIn(img.Size())).SaveImage(Path.Combine(dstDir2, $"{r.ImageNumber}_{r.PartialID}.tif")));

                //リサイズ
                tmpResults.ForEach(r => r.ImgRect = r.ImgRect.Subtract(bp).ResizeFromZero(resizeRate));
                tmpResults.ForEach(r => r.ScoreRect = r.ScoreRect.ResizeFromZero(resizeRate));
                using var resizedImg = img.Resize(Size.Zero, resizeRate, resizeRate, InterpolationFlags.Area);
                var resizedBp = bp.Multiply(resizeRate);
                var cropRect = isRight ? new Rect(resizedBp.X, resizedBp.Y - ImgSize.Height / 2, ImgSize.Width / 2, ImgSize.Height)
                                                : new Rect(resizedBp.X - ImgSize.Width / 2, resizedBp.Y - ImgSize.Height / 2, ImgSize.Width / 2, ImgSize.Height);

                //画像中心基準→ゼロ点基準に
                var croppedImgCenter = new Point(ImgSize.Width / 2, ImgSize.Height / 2);
                tmpResults.ForEach(r => r.ImgRect = r.ImgRect.Add(croppedImgCenter));
                var dstResults = tmpResults.Select(r => new ResultClass(99, r.Address, r.AreaID, r.PartialID, r.ScoreRect_GlobalPos, r.Score[0], Path.Combine(dstDir2, $"{r.ImageNumber}_{r.PartialID}.tif")))
                                        .ToList();
                //２次画像保存
                for (int i = 0; i < tmpResults.Count; i++)
                {
                    double cropRate = 0.7;
                    double resizeRate = 0.5;
                    var r = tmpResults[i];
                    var searchStr1 = $"{r.ImageNumber}_i{r.PartialID:d5}*";
                    var searchStr2 = "*_just*";
                    var ds = Directory.GetDirectories(microscopeDir, searchStr1);
                    if (ds.Length == 0) continue;
                    var imPth = FileManager.FirstFileSearch(ds[0], searchStr2);
                    if (imPth == "") continue;
                    using var im = new Mat(imPth);
                    using var roi = new Mat(im, new Rect((int)(im.Width * (1 - cropRate) / 2), (int)(im.Height * (1 - cropRate) / 2), (int)(im.Width * cropRate), (int)(im.Height * cropRate)));
                    var savePath = Path.Combine(dstDir3, Path.GetFileName(imPth));
                    using var resized = roi.Resize(Size.Zero, resizeRate, resizeRate, InterpolationFlags.Area);
                    Cv2.Rotate(resized, resized, RotateFlags.Rotate90Counterclockwise);
                    resized.SaveImage(savePath);
                    dstResults[i].ImgPath2 = savePath;
                }
                var croppedImg = MatFunctions.RoiToMat(resizedImg, cropRect);
                return (croppedImg, dstResults);
            }

            //左右の画像を1枚に合成
            (var rightImg, var rightResults) = GetImgAndResults(N1ResultPath, N1ImgPath, true);
            (var leftImg, var leftResults) = GetImgAndResults(N2ResultPath, N2ImgPath, false);
            using var stitchedImg = new Mat(ImgSize, MatType.CV_8UC1);
            stitchedImg[new Rect(0, 0, ImgSize.Width / 2, ImgSize.Height)] = leftImg;
            stitchedImg[new Rect(ImgSize.Width / 2, 0, ImgSize.Width / 2, ImgSize.Height)] = rightImg;
            //stitchedImg.SaveImage(Path.Combine(dstDir1, "stitched.tif"));
            var csv = new List<string>() { ResultClass.GetHeader() };
            var leftRightResults = leftResults.ToList();
            leftRightResults.AddRange(rightResults.ToList());
            csv.AddRange(leftRightResults.Select(r => r.GetString()));
            using var scoreImg = ResultClass.DrawResults(stitchedImg, leftRightResults, new Scalar(0, 0, 255));
            scoreImg.SaveImage(Path.Combine(dstDir1, "score.tif"));
            File.WriteAllLines(Path.Combine(dstDir1, "result.csv"), csv, Encoding.UTF8);
            rightImg.Dispose();
            leftImg.Dispose();
            return dstDir1;
        }
        internal class ResultWithLabel
        {
            public ResultClass Result;
            public int Label;
            public ResultWithLabel(ResultClass r, int l)
            {
                this.Result = r;
                this.Label = l;
            }
        }
        public static List<ResultClass> MergeDuplicate(List<ResultClass> results, double intersectRateThresh = 0.8, int margin = 0)
        {
            if (results.Count == 0) return results;
            var dstResults = new List<ResultClass>();
            //重複部分をラベリング
            var resultRects = results.Select(r => r.Rect).ToList();
            var connects = MatFunctions.GetRectConnects(resultRects, intersectRateThresh, margin);
            var resultsWithLabel = connects.Zip(results, (c, r) => new ResultWithLabel(r, c)).ToList();
            var labelNum = connects.Max()+1;

            for (int i = 0; i < labelNum; i++)
            {
                //各ラベルで、最高スコアと合成Rectの代表を作る
                var sameLabel = resultsWithLabel.Where(r => r.Label == i).ToList();
                if (sameLabel.Count == 0) continue;
                var unionRect = sameLabel[0].Result.Rect;
                sameLabel.ForEach(s => unionRect = unionRect.Union(s.Result.Rect));
                var best = sameLabel.MaxBy(s => s.Result.Score).Result;
                best.Rect = unionRect;
                dstResults.Add(best);
            }
            var unique = resultsWithLabel.Where(r => r.Label == -1);
            if (unique.Count() > 0)
            {
                dstResults.AddRange(unique.Select(u => u.Result));
            }
            return dstResults;
        }
        private static List<ResultClass> MergeDuplicate_Old(List<ResultClass> results, double intersectRateThresh = 0.8)
        {

            var dstResults = new List<ResultClass>();
            //重複部分をラベリング
            var resultsWithLabel = results.Select(r => new ResultWithLabel(r, -1)).ToList();
            int label = 0;
            for (int i = 0; i < results.Count; i++)
            {
                var isNewLabel = false;
                for (int j = 0; j < results.Count; j++)
                {
                    if (i == j) continue;
                    if (IntersectsMoreAny(resultsWithLabel[i].Result.Rect, resultsWithLabel[j].Result.Rect, intersectRateThresh))
                    {
                        if (resultsWithLabel[i].Label == -1 && resultsWithLabel[j].Label == -1)
                        {
                            isNewLabel = true;
                            resultsWithLabel[j].Label = label;
                            resultsWithLabel[i].Label = label;
                        }
                        else if (resultsWithLabel[i].Label == -1)
                        {
                            resultsWithLabel[i].Label = resultsWithLabel[j].Label;
                        }
                        else if (resultsWithLabel[j].Label == 1)
                        {
                            resultsWithLabel[j].Label = resultsWithLabel[i].Label;
                        }
                    }
                }
                if (isNewLabel)
                {
                    label++;
                    isNewLabel = false;
                }
            }

            for (int i = 0; i < label; i++)
            {
                //各ラベルで、最高スコアと合成Rectの代表を作る
                var sameLabel = resultsWithLabel.Where(r => r.Label == i).ToList();
                var unionRect = sameLabel[0].Result.Rect;
                sameLabel.ForEach(s => unionRect = unionRect.Union(s.Result.Rect));
                var best = sameLabel.MaxBy(s => s.Result.Score).Result;
                best.Rect = unionRect;
                dstResults.Add(best);
            }
            var unique = resultsWithLabel.Where(r => r.Label == -1);
            if (unique.Count() > 0)
            {
                dstResults.AddRange(unique.Select(u => u.Result));
            }
            return dstResults;
            bool IntersectsMoreAny(Rect2d r1, Rect2d r2, double thresh)
            {
                return r1.IntersectsMore(r2, thresh) || r2.IntersectsMore(r1, thresh);

            }
        }
    }
}
