using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using IjhCommonUtility;
using System.Reflection.Metadata;
using System.IO.Packaging;
using System.Windows.Media.Media3D;
using System.Windows.Data;
using System.Windows.Shapes;
using Path = System.IO.Path;
using System.Data.Common;
using System.Net.Sockets;
using System.Windows.Controls;
using NetMQ;
using NetMQ.Sockets;
using static System.Formats.Asn1.AsnWriter;
using System.Runtime.Serialization;
using OpenCvSharp.Flann;
using MathNet.Numerics;

namespace Wafer
{
    public class CutAreaParam
    {
        public string Name = "";
        public List<Rect> CutAreas = new List<Rect>();
        public CutAreaParam(string name, List<Rect> cutAreas)
        {
            Name = name;
            CutAreas = cutAreas;
        }
        public CutAreaParam(string name, Rect cutArea)
        {
            Name = name;
            CutAreas = new List<Rect>() { cutArea };
        }
    }

    public class ResultClass
    {
        public int StripId { get; set; }
        public string ChipName { get; set; }
        public string AreaName { get; set; }
        public int DefectId { get; set; }
        public Rect2d Rect { get; set; }
        public double Score { get; set; }
        public string ImgPath { get; set; }
        public string ImgPath2 { get; set; }//2次画像パス
        public ResultClass(int stripId, string chipName, string areaName, int defectId, Rect rect, double score, string imgPath, string imgPath2 = "")
        {
            StripId = stripId;
            ChipName = chipName;
            AreaName = areaName;
            DefectId = defectId;
            Rect = rect.To2d();
            Score = score;
            ImgPath = imgPath;
            ImgPath2 = imgPath2;
        }
        public ResultClass(int stripId, string chipName, string areaName, int defectId, Rect2d rect, double score, string imgPath, string imgPath2 = "")
        {
            StripId = stripId;
            ChipName = chipName;
            AreaName = areaName;
            DefectId = defectId;
            Rect = rect;
            Score = score;
            ImgPath = imgPath;
            ImgPath2 = imgPath2;
        }
        public ResultClass(string csvContent)
        {
            var tmp = csvContent.Split(',');
            StripId = int.TryParse(tmp[0], out var w) ? w : 0;
            ChipName = tmp[1];
            AreaName = tmp[2];
            DefectId = int.TryParse(tmp[3], out var d) ? d : 0;
            Rect = new Rect2d(double.Parse(tmp[4]), double.Parse(tmp[5]), double.Parse(tmp[6]), double.Parse(tmp[7]));
            Score = double.Parse(tmp[8]);
            ImgPath = tmp[9];
            if (tmp.Length < 11) ImgPath2 = "";
            else ImgPath2 = tmp[10];
        }
        public string GetString()
        {
            return $"{this.StripId},{this.ChipName},{this.AreaName},{this.DefectId},{this.Rect.X:f2},{this.Rect.Y:f2},{this.Rect.Width:f2},{this.Rect.Height:f2},{this.Score},{this.ImgPath},{this.ImgPath2}";
        }
        static public string GetHeader()
        {
            return "StripId,ChipId,AreaName,DefectId,rX,rY,rW,rH,Score,ImgPath,ImgPath2";
        }
        static public Mat DrawResults(Mat src, List<ResultClass> results, Scalar color)
        {
            var RectThickness = 2;
            var t = RectThickness + 2;
            return MatFunctions.MakeScoreRectsInImage(src, results.Select(r => OpenCvSharp.Rect.Inflate(r.Rect.ToInt(), t, t)).ToList(), color, results.Select(r => r.Score).ToList(), results.Select(r => r.DefectId.ToString()).ToList(), RectThickness, 1);
        }
        static public void DrawResults(ref Mat src, List<ResultClass> results, Scalar color)
        {
            var RectThickness = 2;
            var t = RectThickness + 2;
            MatFunctions.MakeScoreRectsInImage(ref src, results.Select(r => OpenCvSharp.Rect.Inflate(r.Rect.ToInt(), t, t)).ToList(), color, results.Select(r => r.Score).ToList(), results.Select(r => r.DefectId.ToString()).ToList(), RectThickness, 1);
        }
    }

    public partial class ProcessAndStitch
    {
        public List<(int Id, Rect GrobalRect, Mat Img, string Path)> OrgImgInfos = new List<(int Id, Rect GrobalRect, Mat Img, string Path)>();
        public double ResizeRate;
        public Point PlaceCorrectHistory;
        public ProcessAndStitch() { }

        //元画像の切り出し領域　現状このパラメータはベタ書き
        public List<CutAreaParam> CutParams = new List<CutAreaParam>()
        {
            new CutAreaParam("i00", Rect.Empty),
            new CutAreaParam("i01", new Rect(0,38900,3240,49400)),
            new CutAreaParam("i02", new Rect(0,31900,7050,63000)),
            new CutAreaParam("i03", new Rect(0,26800,8100,73250)),
            new CutAreaParam("i04", new List<Rect>(){new Rect(0,14500,1800,98600), new Rect(1500,17570,6500,91900)}),
            new CutAreaParam("i05", new Rect(0,13300,8100,100300)),
            new CutAreaParam("i06", new Rect(0,10500,8100,105700)),
            new CutAreaParam("i07", new Rect(0,9050,8100,109000)),
            new CutAreaParam("i08", new Rect(0,9050,8100,109000)),
            new CutAreaParam("i09", new Rect(0,10800,8100,105000)),
            new CutAreaParam("i10", new Rect(0,14000,8100,99000)),
            new CutAreaParam("i11", new Rect(0,18800,8100,89500)),
            new CutAreaParam("i12", new Rect(0,25800,8100,75600)),
            new CutAreaParam("i13", new List<Rect>(){new Rect(0, 35300, 4000, 56500), new Rect(3700,31000,4300,64500) }),
            new CutAreaParam("i14", new Rect(5750,40000,2250,47200)),
            new CutAreaParam("i15", Rect.Empty)
        };

        public Mat Fukugen(Rect rect)
        {
            var c = rect.Center().Subtract(this.PlaceCorrectHistory);
            var contain = this.OrgImgInfos.Where(r => r.GrobalRect.Contains(c)).FirstOrDefault();
            if (contain.Path == null) throw new ArgumentException($"Rectが範囲外です:{rect}");
            var grobalLoc = contain.GrobalRect.Location;
            var localRect = MatFunctions.RectFromCenterPoint(c.X - grobalLoc.X, c.Y - grobalLoc.Y, rect.Width, rect.Height);
            localRect = localRect.ResizeFromZero(1.0 / this.ResizeRate);
            return MatFunctions.RoiToMat(contain.Img, localRect);
        }

        //ディレクトリ内にi0~15の画像が揃っている前提で、画像ごとの設定を使って切り出す
        //出力はフォルダごとに分かれる
        public void CutToImg(string srcDir, string saveDir, double resizeRate = 1.0)
        {
            var cutSize = new Size(640, 640);
            var marginSize = new Size(320, 320);
            var imgPaths = Directory.GetFiles(srcDir, "*").OrderBy(p => p).ToList();
            for (int i = 0; i < imgPaths.Count; i++)
            {
                var cutParam = this.CutParams[i];
                if (cutParam.CutAreas[0] == Rect.Empty) continue;

                var p = imgPaths[i];
                var fileNameBase = Path.GetFileNameWithoutExtension(p);
                var saveDir2 = Path.Combine(saveDir, fileNameBase);
                //saveDir2 = saveDir;
                Directory.CreateDirectory(saveDir2);
                var src = new Mat(p, ImreadModes.Grayscale);
                var rects = new List<Rect>();
                foreach (var area in cutParam.CutAreas)
                {
                    rects.AddRange(WaferInspectCommon.CutToRect(area.Size, cutSize, marginSize).Select(a => a.Shift(area.X, area.Y)).ToList());
                }
                for (int j = 0; j < rects.Count; j++)
                {
                    var r = rects[j];
                    var savePath = Path.Combine(saveDir2, $"{fileNameBase}_i{j}_X{r.X}_Y{r.Y}_W{r.Width}_H{r.Height}.tif");
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
                            if (mat.Mean()[0] > 40) mat.SaveImage(savePath);//真っ黒画像を除外する
                        }
                        catch { Trace.WriteLine($"Error:j={j}"); throw; }

                    }
                }
                src.Dispose();
            }
        }

        //巨大画像だとうまくいかなかったので没
        public void StitchImages_OpenCv(List<Mat> imgs, string savePath)
        {
            var pano = new Mat();
            var st = Stitcher.Create(Stitcher.Mode.Scans);
            var status = st.Stitch(imgs, pano);
            pano.SaveImage(savePath);
        }
        public (Mat stitched, List<Point> stitchLocations) StitchImages(List<Mat> imgs, double resizeRate = 0.1)
        {
            //右から左につなげていく　imgsは右から順になっている前提
            var templateRect = new Rect(789, 150, 30, 12300);//resize10%の時のパラメータ
            var searchArea = new Rect(0, 100, 60, 12400);
            var res = resizeRate / 0.1;

            templateRect = new Rect((int)(templateRect.X * res), (int)(templateRect.Y * res), (int)(templateRect.Width * res), (int)(templateRect.Height * res));
            searchArea = new Rect((int)(searchArea.X * res), (int)(searchArea.Y * res), (int)(searchArea.Width * res), (int)(searchArea.Height * res));
            var pasteLocations = new List<Point>();//1つ前の画像に対する貼り付け位置
            var imgSize = imgs[0].Size();//画像サイズは全て等しい前提
            Trace.WriteLine($"start:");
            //画像間のマッチングで貼り付け位置（相対）を決定
            for (int i = 1; i < imgs.Count; i++)
            {
                var matchImg = new Mat();
                var templateImg = PreProcess(new Mat(imgs[i], templateRect).Clone());
                var searchAreaImg = PreProcess(new Mat(imgs[i - 1], searchArea).Clone());
                Cv2.MatchTemplate(searchAreaImg, templateImg, matchImg, TemplateMatchModes.CCoeff);
                matchImg.MinMaxLoc(out var min, out var max, out var minLoc, out var maxLoc);
                pasteLocations.Add(maxLoc.Add(searchArea.Location).Subtract(templateRect.Location));
                //MatFunctions.ShowImage(searchAreaImg.Resize(new Size(searchAreaImg.Width, 1000)), "search");
                //MatFunctions.ShowImage(templateImg.Resize(new Size(templateImg.Width, 1000)), "template");
                //MatFunctions.ShowImage(matchImg, "match");
            }
            //相対位置をグローバル位置に
            var pasteLocations_Global = new List<Point>();
            pasteLocations_Global.Add(new Point(0, 0));
            for (int i = 0; i < pasteLocations.Count; i++)
            {
                pasteLocations_Global.Add(pasteLocations_Global[i].Add(pasteLocations[i]));
            }
            //マイナス値を補正
            var tmp = pasteLocations_Global;
            var minXY = new Point(tmp.Min(p => p.X), tmp.Min(p => p.Y));
            pasteLocations_Global = tmp.Select(t => t.Subtract(minXY)).ToList();
            this.OrgImgInfos = imgs.Select((img, i) => (Id: i, GrobalRect: new Rect(pasteLocations_Global[i], img.Size()), Img: img, Path: "")).ToList();
            //合体
            Trace.WriteLine($"merge");
            var resultSize = new Size(pasteLocations_Global.Max(p => p.X) + imgSize.Width, pasteLocations_Global.Max(p => p.Y) + imgSize.Height);
            var resultImg = new Mat(resultSize, MatType.CV_8UC1, 0);
            var prevRect = Rect.Empty;
            for (int i = 0; i < pasteLocations_Global.Count; i++)
            {
                if (i > 0)
                {
                    //継ぎ目が両画像のグラデーションになるように合成
                    var pLeft = pasteLocations[i - 1];
                    var YCurrent = pLeft.Y < 0 ? -pLeft.Y : 0;
                    var YPrev = pLeft.Y < 0 ? 0 : pLeft.Y;
                    var YDistance = pLeft.Y < 0 ? -pLeft.Y : pLeft.Y;
                    var blendSize = new Size(imgs[i].Width + pLeft.X, imgs[i].Height - YDistance);
                    var roiCurrent = new Mat(imgs[i], new Rect(new Point(-pLeft.X, YCurrent), blendSize));
                    var roiPrev = new Mat(imgs[i - 1], new Rect(new Point(0, YPrev), blendSize));
                    var blended = BlendWithGradation(roiCurrent, roiPrev);
                    //MatFunctions.ShowImage(imgs[i].Resize(new Size(imgs[i].Width, 1000)), "Current");
                    //MatFunctions.ShowImage(imgs[i - 1].Resize(new Size(imgs[i - 1].Width, 1000)), "Prev");

                    //roiCurrent.SaveImage(FileManager.GetRenamedPath_New(savePath, "cur"));
                    //roiPrev.SaveImage(FileManager.GetRenamedPath_New(savePath, "pre"));
                    //blended.SaveImage(FileManager.GetRenamedPath_New(savePath, "blend"));
                    //MatFunctions.ShowImage(roiCurrent.Resize(new Size(roiCurrent.Width * 2, 1000)), "CurrentROI");
                    //MatFunctions.ShowImage(roiPrev.Resize(new Size(roiPrev.Width * 2, 1000)), "PrevROI");
                    //MatFunctions.ShowImage(blended.Resize(new Size(blended.Width * 2, 1000)), "Blended");
                    blended.CopyTo(roiCurrent);
                }
                var roi = new Mat(resultImg, new Rect(pasteLocations_Global[i], imgs[i].Size()));
                imgs[i].CopyTo(roi);
            }
            return (resultImg, pasteLocations_Global);
            Mat PreProcess(Mat img)
            {
                //ガウシアンぼかし＋縦方向ソーベルフィルタ
                using var tmpBlur = img.GaussianBlur(new Size(3, 3), 0);
                using var tmpSobel = tmpBlur.Sobel(MatType.CV_32FC1, 0, 1, 3, 1, 0, BorderTypes.Reflect101);
                using var tmpAbs = tmpSobel.Abs().ToMat();
                var processed = new Mat();
                tmpAbs.ConvertTo(processed, MatType.CV_8UC1);
                return processed;
            }
        }
        private void SaveProfile(string imgPath, string saveDir = "")
        {
            double resizeRate = 1.0;//0.1;
            var yStart = 21705;// 1300;//20763;
            var yHeight = 80;// 200;//80;
            var basePixelValue = 110.0;

            var img = new Mat(imgPath, ImreadModes.Grayscale);
            using var img_resized = img.Resize(Size.Zero, resizeRate, resizeRate, InterpolationFlags.Area);
            img.Dispose();
            using var roi = new Mat(img_resized, new Rect(0, yStart, img_resized.Width, yHeight)).Clone();
            var profile_tmp = new Mat<float>(roi.Reduce(ReduceDimension.Row, ReduceTypes.Avg, MatType.CV_32FC1)).ToArray();
            roi.SaveImage(Path.Combine(saveDir, "kakunin.tif"));
            double[] profile = profile_tmp.Select(p => (double)p).ToArray();
            //平坦化するための係数にする
            profile = profile.Select(p => basePixelValue / p).ToArray();
            var profileTxt = string.Join(",", profile.Select(p => $"{p:f5}").ToList());
            var csvFullName = FileManager.GetRenamedPath_Add(imgPath, ".csv", true);
            var savePath = saveDir == "" ? csvFullName : Path.Combine(saveDir, Path.GetFileName(csvFullName));
            File.WriteAllText(savePath, profileTxt, Encoding.UTF8);

        }
        private Mat ReadProfile(string path)
        {
            var profile = File.ReadAllText(path).Split(',').Select(p => float.Parse(p)).ToArray();
            using var tmp = new Mat(1, profile.Length, MatType.CV_32FC1, profile);
            return tmp.Clone();
        }
        public void CorrectBrightness_SaveProfile(string imgPath, string saveDir)
        {
            SaveProfile(imgPath, saveDir);
        }

        public void CorrectBrightness(string imgPath, string csvPath)
        {
            //var imgPath = @"E:\asano\Imgs\resize0.1\20221028030854\20221028030854_i08_Y1040000.tif";
            //var csvPath = FileManager.GetRenamedPath_Add(imgPath, ".csv", true);
            //SaveProfile(imgPath);
            using var src = new Mat(imgPath, ImreadModes.Grayscale);
            using var filtered = CorrectBrightness(src, csvPath);
            filtered.SaveImage(FileManager.GetRenamedPath_Add(imgPath, "_corrected"));
        }
        public Mat CorrectBrightness(Mat img, string csvPath)
        {
            var profile = ReadProfile(csvPath);
            if (profile.Width != img.Width)
            {
                Trace.WriteLine($"profWidth:{profile.Width}, imgWidth:{img.Width}");
                Cv2.Resize(profile, profile, new Size(img.Width, 1), 0, 0, InterpolationFlags.Area);
            }
            using var filter = profile.Repeat(img.Height, 1);
            using var tmp = new Mat();
            filter.ConvertTo(tmp, MatType.CV_8UC1);
            tmp.SaveImage(FileManager.GetRenamedPath_Add(csvPath, "_filt.tif", true));
            using Mat img_float = new Mat();
            img.ConvertTo(img_float, MatType.CV_32FC1);
            Cv2.Multiply(img_float, filter, img_float);
            var filtered = new Mat();
            img_float.ConvertTo(filtered, MatType.CV_8UC1);
            return filtered;
        }
        public Point GetCenterPlaceFromMark(Point markLeft, Point markRight, List<Point> stitchLocations, double resizeRate)
        {
            var markLeftResized = markLeft * resizeRate;
            var markRightResized = markRight * resizeRate;
            var markLeftGlobal = markLeftResized.Add(stitchLocations[14]);//画像番号と左右マークの対応は決め打ち
            var markRightGlobal = markRightResized.Add(stitchLocations[1]);
            var center = new Point((markLeftGlobal.X+markRightGlobal.X)/2, (markLeftGlobal.Y + markRightGlobal.Y) / 2);
            return center;
        }
        public Point GetCenterPlace(Mat mergedImg)
        {
            var thresh = 30;
            var kernelSize = 5;
            var tmpSize = new Size(10000, 10000);

            var orgSize = mergedImg.Size();
            var rateX = (double)orgSize.Width / tmpSize.Width;
            var rateY = (double)orgSize.Height / tmpSize.Height;
            using var kernel = new Mat(kernelSize, kernelSize, MatType.CV_8UC1, new Scalar(1));
            using var tmp = mergedImg.Resize(tmpSize, 0, 0, InterpolationFlags.Area);
            Cv2.Threshold(tmp, tmp, thresh, 255, ThresholdTypes.Binary);
            //Cv2.Threshold(margedImg, threshed, thresh, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            //using var morphed = new Mat(tmp.Size(), tmp.Type());
            //Cv2.MorphologyEx(morphed, tmp, MorphTypes.Close, kernel);
            var cc = Cv2.ConnectedComponentsEx(tmp, PixelConnectivity.Connectivity4);
            var maxBlob = cc.Blobs.Skip(1).OrderByDescending(b => b.Area).First();
            var p = maxBlob.Rect.Center();
            var dstPoint = new Point(p.X * rateX, p.Y * rateY);
            return dstPoint;
        }

        /// <summary>
        /// グラデーションでアルファブレンド
        /// </summary>
        /// <param name="src1">左/上に来る画像</param>
        /// <param name="src2">右/下に来る画像</param>
        /// <param name="isYoko">横方向にブレンドする　falseなら縦</param>
        /// <param name="grad">グラデーション配列(0~1.0, 画像の幅と同じ)　nullなら自動で線形グラデーション</param>
        /// <returns></returns>
        public Mat BlendWithGradation(Mat src1, Mat src2, bool isYoko = true, double[]? grad = null)
        {
            float[]? grad_f = null;
            if (grad == null)
            {
                var length = isYoko ? src1.Width : src1.Height;
                grad_f = Enumerable.Range(0, length).Select(r => (float)r / length).ToArray();
            }
            else
            {
                grad_f = grad.Select(g => (float)g).ToArray();
            }
            using var gradImgBase = isYoko ? new Mat(1, src1.Width, MatType.CV_32FC1, grad_f) : new Mat(src1.Height, 1, MatType.CV_32FC1, grad_f);
            using var gradImg = isYoko ? gradImgBase.Repeat(src1.Height, 1) : gradImgBase.Repeat(1, src1.Width);
            using var s1 = new Mat();
            src1.ConvertTo(s1, MatType.CV_32FC1);
            using var s2 = new Mat();
            src2.ConvertTo(s2, MatType.CV_32FC1);
            using var dstFloat = s1.Mul(1 - gradImg) + s2.Mul(gradImg);
            var dst = new Mat();
            dstFloat.ToMat().ConvertTo(dst, MatType.CV_8UC1);
            return dst;
        }
        /// <summary>
        /// ウエハ傾き算出
        /// 短冊位置ずれを別途考慮する必要あり！！
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        public (double degree, Point pLeft, Point pRight, Mat matchedImgLeft, Mat matchedImgRight) Katamuki(string dirPath, string templateImgPath)
        {
            var srcPath1 = Directory.GetFiles(dirPath, "*i01*.tif").First();
            var srcPath2 = Directory.GetFiles(dirPath, "*i14*.tif").First();
            using var imgRight = new Mat(srcPath1, ImreadModes.Grayscale);
            using var imgLeft = new Mat(srcPath2, ImreadModes.Grayscale);
            return Katamuki_FromImg(imgLeft, imgRight, templateImgPath);
            //File.WriteAllLines(Path.Combine(Path.GetDirectoryName(dirPath), "katamuki.csv"), content );
        }
        public (double degree_clockwise, Point pLeft, Point pRight, Mat matchedImgLeft, Mat matchedImgRight) Katamuki_FromImg(Mat imgLeft, Mat imgRight, string templateImgPath)
        {
            //var matchArea1 = new Rect(2700, 59300, 5000, 10000);
            //var matchArea2 = new Rect(2200, 59800, 5000, 10000);
            var matchAreaLeft = new Rect(3280, 59280, 3000, 8000);
            var matchAreaRight = new Rect(3860, 59780, 3200, 8000);
            double pixPerMilimeter = 3.0 / 5.0 * 1000;//単位長さ当たりの画素数3/5[pix/um]
            double betweenMark = 170;//マーク同士の幅が約170mm
            double haba_mark = betweenMark * pixPerMilimeter;// 170.0 * 3.0 / 5.0 * 1000;マーク同士の距離[pix]
            using var template = new Mat(templateImgPath, ImreadModes.Grayscale);
            var pLeft = CheckMatch(imgLeft, matchAreaLeft, template);
            var matchedImgLeft = MatFunctions.RoiToMat(imgLeft, MatFunctions.RectFromCenterPoint(pLeft, template.Size()));
            var pRight = CheckMatch(imgRight, matchAreaRight, template);
            var matchedImgRight = MatFunctions.RoiToMat(imgRight, MatFunctions.RectFromCenterPoint(pRight, template.Size()));
            Trace.WriteLine($"pLeft:{pLeft}, pRight:{pRight}");
            var kakudo_clockwise = Math.Atan((double)(pRight.Y - pLeft.Y) / haba_mark) * 180 / Math.PI;//degree
            Trace.WriteLine($"pLeft:({pLeft.X}, {pLeft.Y}), pRight:({pRight.X}, {pRight.Y}), kakudo:{kakudo_clockwise} deg");
            var content = new List<string>() { $"X_pLeft,Y_pLeft, X_pRight, Y_pRight, degree", $"{pLeft.X}, {pLeft.Y}, {pRight.X}, {pRight.Y}, {kakudo_clockwise}" };
            return (kakudo_clockwise, pLeft, pRight, matchedImgLeft, matchedImgRight);
        }
        public Point CheckMatch(Mat src, Rect MatchArea, Mat templateImg)
        {
            using var srcForMatch = MatFunctions.RoiToMat(src, MatchArea);

            using var templateImgProcessed = PreProcess(templateImg);
            using var srcForMatchProcessed = PreProcess(srcForMatch);
            //MatFunctions.ShowImage(templateImg);
            //MatFunctions.ShowImage(templateImgProcessed);
            //MatFunctions.ShowImage(srcForMatch, resizeRatio: 0.2);
            //MatFunctions.ShowImage(srcForMatchProcessed, resizeRatio: 0.2);
            using var matchResult = new Mat();
            Cv2.MatchTemplate(srcForMatchProcessed, templateImgProcessed, matchResult, TemplateMatchModes.CCoeffNormed);
            //MatFunctions.ShowImage(matchResult, resizeRatio: 0.2);

            matchResult.MinMaxLoc(out var min, out var max, out var minLoc, out var maxLoc);
            var maxLocCenter = maxLoc.Add(new Point(templateImg.Width / 2, templateImg.Height / 2));
            var resultLoc = MatchArea.Location.Add(maxLocCenter);
            //var rect = MatFunctions.RectFromCenterPoint(resultLoc, new Size(200, 200));
            //MatFunctions.ShowImage(MatFunctions.RoiToMat(src, rect));
            return resultLoc;
            Mat PreProcess(Mat img)
            {
                //ガウシアンぼかし＋ラプラシアンフィルタ
                //8UC1指定だと負の値は0に丸められるが、処理速度優先でとりあえずこれ
                return img.GaussianBlur(new Size(3, 3), 0).Laplacian(MatType.CV_8UC1);
            }
        }

        /// <summary>
        /// スキューを補正
        /// XPerYが＋のとき、画像TopLeftの座標が左にずれるよう変形（ ＼ ）
        /// </summary>
        /// <param name="src"></param>
        /// <param name="XPerY"></param>
        /// <returns></returns>
        public Mat CorrectSkew(Mat src, double XPerY)
        {
            var srcPoints = new List<Point2f>() { new Point2f(0, 0), new Point2f(0, src.Height), new Point2f(src.Width, 0) };
            var dstPoints = new List<Point2f>() { new Point2f((float)(-src.Height * XPerY * 0.5), 0), new Point2f((float)(src.Height * XPerY * 0.5), src.Height), new Point2f(src.Width - (float)(src.Height * XPerY * 0.5), 0) };
            var affineMat = Cv2.GetAffineTransform(srcPoints, dstPoints);
            var dstMat = new Mat();
            Cv2.WarpAffine(src, dstMat, affineMat, src.Size(), InterpolationFlags.Linear, BorderTypes.Replicate);
            return dstMat;
        }
    }
}
