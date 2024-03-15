using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size;
using Point = OpenCvSharp.Point;
using DocumentFormat.OpenXml.Drawing;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Wafer.ProcessAndStitch;
using DocumentFormat.OpenXml.Math;

namespace IjhCommonUtility
{
    /// <summary>
    /// OpenCvSharpのクラス（主にMat）に関する自作関数群
    /// </summary>
    public static class MatFunctions
    {
        /// <summary>
        /// ROIをディープコピーで作成
        /// </summary>
        /// <param name="m"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Mat RoiToMat(Mat m, Rect r)
        {
            var tmp = new Mat(m, r);
            var dst = new Mat();
            tmp.CopyTo(dst);
            return dst;
        }
        #region Rect Functions
        public static Rect2d To2d(this Rect r) => new Rect2d(r.X, r.Y, r.Width, r.Height); 
        public static Rect ToInt(this Rect2d r) => new Rect((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
        public static Rect ToCvRect(this System.Windows.Rect r) => new Rect((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
        public static Rect2d ToCvRect2d(this System.Windows.Rect r) => new Rect2d(r.X, r.Y, r.Width, r.Height);
        public static System.Windows.Rect ToWindowsRect(this Rect2d r) => new System.Windows.Rect(r.X, r.Y, r.Width, r.Height);
        public static System.Windows.Rect ToWindowsRect(this Rect r) => new System.Windows.Rect(r.X, r.Y, r.Width, r.Height);
        public static int Area(this Rect r) => r.Width * r.Height;

        public static Point2d To2d(this Point p) => new Point2d(p.X, p.Y);
        public static Point ToInt(this Point p) => new Point(p.X, p.Y);
        public static Size2d To2d(this Size s) => new Size2d(s.Width, s.Height);
        public static Size ToInt(this Size2d s) => new Size(s.Width, s.Height);
        public static Rect Shift(this Rect r, int shiftX, int shiftY)
        {
            return new Rect(r.X + shiftX, r.Y + shiftY, r.Width, r.Height);
        }
        public static Point Center(this Rect r)
        {
            return new Point((int)(0.5 + (r.Left + r.Right) * 0.5), (int)(0.5 + (r.Top + r.Bottom) * 0.5));
        }
        public static Point2d Center(this Rect2d r)
        {
            return new Point2d((r.Left + r.Right) * 0.5, (r.Top + r.Bottom) * 0.5);
        }
        public static Point Mid(this Point p1, Point p2)
        {
            return new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
        }
        public static Point2d Mid(this Point2d p1, Point2d p2)
        {
            return new Point2d((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
        }
        /// <summary>
        /// 中心位置を変えずにリサイズ
        /// </summary>
        /// <param name="r"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        public static Rect ResizeFromCenter(this Rect r, double rate)
        {
            var resized = new Size(r.Width * rate, r.Height * rate);
            var pos = new Point(r.X + (r.Width - resized.Width) / 2, r.Y + (r.Height - resized.Height) / 2);
            return new Rect(pos.X, pos.Y, resized.Width, resized.Height);
        }
        public static Rect2d ResizeFromCenter(this Rect2d r, double rate)
        {
            var resized = new Size2d(r.Width * rate, r.Height * rate);
            var pos = new Point2d(r.X + (r.Width - resized.Width) / 2, r.Y + (r.Height - resized.Height) / 2);
            return new Rect2d(pos.X, pos.Y, resized.Width, resized.Height);
        }
        /// <summary>
        /// 中心位置を変えずにサイズを増減
        /// </summary>
        /// <param name="r"></param>
        /// <param name="add">上下左右にこの数値をプラス</param>
        /// <returns></returns>
        public static Rect AddSize(this Rect r, Size add)
        {
            return new Rect(r.X-add.Width, r.Y-add.Height, r.Width + add.Width*2, r.Height + add.Height*2);
        }
        public static Rect2d AddSize(this Rect2d r, Size2d add)
        {
            return new Rect2d(r.X - add.Width, r.Y - add.Height, r.Width + add.Width * 2, r.Height + add.Height * 2);
        }
        /// <summary>
        /// ゼロ点基準でリサイズ
        /// </summary>
        /// <param name="r"></param>
        /// <param name="rate"></param>
        /// <returns></returns>
        public static Rect ResizeFromZero(this Rect r, double rate)
        {
            return new Rect((int)(r.X * rate), (int)(r.Y * rate), (int)(r.Width * rate), (int)(r.Height * rate));
        }
        public static Rect ResizeFromZero(this Rect r, double rateX, double rateY)
        {
            return new Rect((int)(r.X * rateX), (int)(r.Y * rateY), (int)(r.Width * rateX), (int)(r.Height * rateY));
        }
        public static Rect2d ResizeFromZero(this Rect2d r, double rate)
        {
            return new Rect2d(r.X * rate, r.Y * rate, r.Width * rate, r.Height * rate);
        }
        public static Rect2d ResizeFromZero(this Rect2d r, double rateX, double rateY)
        {
            return new Rect2d(r.X * rateX, r.Y * rateY, r.Width * rateX, r.Height * rateY);
        }

        public static Rect RectFromCenterPoint(int x, int y, int w, int h)
        {
            return new Rect(x - w / 2, y - h / 2, w, h);
        }
        public static Rect2d RectFromCenterPoint(double x, double y, double w, double h)
        {
            return new Rect2d(x - w / 2, y - h / 2, w, h);
        }
        public static Rect RectFromCenterPoint(Point p, Size s)
        {
            return new Rect(p.X - s.Width / 2, p.Y - s.Height / 2, s.Width, s.Height);
        }
        public static Rect2d RectFromCenterPoint(Point2d p, Size2d s)
        {
            return new Rect2d(p.X - s.Width / 2, p.Y - s.Height / 2, s.Width, s.Height);
        }
        /// <summary>
        /// このRectに対して〇%以上の重複があるかどうか
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="thresh"></param>
        /// <returns></returns>
        public static bool IntersectsMore(this Rect rect1, Rect rect2, double thresh = 0.5)
        {
            var intersect = rect1.Intersect(rect2);
            var intersectArea = (double)intersect.Width * intersect.Height;
            var rect1Area = (double)rect1.Width * rect1.Height;
            return intersectArea / rect1Area > thresh;
        }
        public static bool IntersectsMore(this Rect2d rect1, Rect2d rect2, double thresh = 0.5)
        {
            var intersect = rect1.Intersect(rect2);
            var intersectArea = intersect.Width * intersect.Height;
            var rect1Area = rect1.Width * rect1.Height;
            return intersectArea / rect1Area > thresh;
        }
        /// <summary>
        /// いずれかのRectに対して〇%以上の重複があるかどうか
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="thresh"></param>
        /// <returns></returns>
        public static bool IntersectsMore_Any(this Rect rect1, Rect rect2, double thresh = 0.5)
        {
            return IntersectsMore(rect1, rect2, thresh) || IntersectsMore(rect2, rect1, thresh);
        }
        public static bool IntersectsMore_Any(this Rect2d rect1, Rect2d rect2, double thresh = 0.5)
        {
            return IntersectsMore(rect1, rect2, thresh) || IntersectsMore(rect2, rect1, thresh);
        }
        public static bool IsOver(this Rect rect, Size size)
        {
            return (rect.X < 0 || rect.Y < 0 || rect.Right > size.Width || rect.Bottom > size.Height);
        }
        public static void ThrowIfRectOverSize(Size size, Rect rect, string message = "")
        {
            if (rect.IsOver(size)) throw new ArgumentOutOfRangeException($"{message}/rectがsizeをはみ出していますRect:X:{rect.X},Y:{rect.Y},W:{rect.Width},H:{rect.Height} Size:W:{size.Width},H{size.Height}");
        }
        public static Rect ShiftIfRectOverSize(Size size, Rect rect, bool cutIfOverhang = false)
        {
            int newX = rect.X < 0 ? 0 : rect.X;
            int newY = rect.Y < 0 ? 0 : rect.Y;
            int right = newX + rect.Width;
            int bottom = newY + rect.Height;
            if (right > size.Width) newX -= right - size.Width;
            if (bottom > size.Height) newY -= bottom - size.Height;
            var newRect = new Rect(newX, newY, rect.Width, rect.Height);
            if (cutIfOverhang && newRect.IsOver(size)) return CutIfRectOverSize(size, newRect);
            if (!cutIfOverhang) ThrowIfRectOverSize(size, newRect, "Error:CutIfRectOverSize 移動後もはみ出し");
            return newRect;
        }
        public static Rect CutIfRectOverSize(Size size, Rect rect)
        {
            Console.WriteLine($"In:X:{rect.X},Y:{rect.Y},W:{rect.Width},H:{rect.Height} Size:W:{size.Width},H{size.Height}");
            int newX, newY, newWidth, newHeight;
            if (rect.X < 0)
            {
                newX = 0;
                newWidth = rect.Width - rect.X;
            }
            else
            {
                newX = rect.X;
                newWidth = rect.Width;
            }
            if (rect.Y < 0)
            {
                newY = 0;
                newHeight = rect.Height - rect.Y;
            }
            else
            {
                newY = rect.Y;
                newHeight = rect.Height;
            }
            if (newX + newWidth > size.Width) newWidth -= (newX + newWidth - size.Width);
            if (newY + newHeight > size.Height) newHeight -= (newY + newHeight - size.Height);
            var newRect = new Rect(newX, newY, newWidth, newHeight);
            Console.WriteLine($"Out:X:{newX},Y:{newY},W:{newWidth},H:{newHeight} Size:W:{size.Width},H{size.Height}");
            ThrowIfRectOverSize(size, newRect, "Error:CutIfRectOverSize 縮小後もはみ出し");
            return newRect;
        }
        public static Rect GetBoundingBox(IEnumerable<Point> points)
        {
            var x = points.Min(p => p.X);
            var y = points.Min(p => p.Y);
            var w = points.Max(p => p.X) - x;
            var h = points.Max(p => p.Y) - y;
            return new Rect(x, y, w, h);
        }
        public static Rect GetBoundingBox(IEnumerable<Point2f> points)
        {
            var x = points.Min(p => p.X);
            var y = points.Min(p => p.Y);
            var w = points.Max(p => p.X) - x;
            var h = points.Max(p => p.Y) - y;
            return new Rect((int)x, (int)y, (int)w, (int)h);
        }
        /// <summary>
        /// (0,0)とsizeをはみ出さないようにRectをずらす
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Rect FitIn(this Rect rect, Size size)
        {
            int newX = rect.X < 0 ? 0 : rect.X;
            int newY = rect.Y < 0 ? 0 : rect.Y;
            int right = newX + rect.Width;
            int bottom = newY + rect.Height;
            if (right > size.Width) newX -= right - size.Width;
            if (bottom > size.Height) newY -= bottom - size.Height;
            var newRect = new Rect(newX, newY, rect.Width, rect.Height);
            ThrowIfRectOverSize(size, newRect);
            return newRect;
        }
        /// <summary>
        /// srcPointをorgPointに対して時計回りに回転させる
        /// </summary>
        /// <param name="srcPoint"></param>
        /// <param name="orgPoint">回転の原点</param>
        /// <param name="radian">回転させる角度[rad]</param>
        /// <returns></returns>
        public static Point RotatePoint_Clockwise(Point srcPoint, Point orgPoint, double radian)
        {
            var rad = radian;
            Point fromOrigin = srcPoint - orgPoint;
            var rotated = new Point(orgPoint.X + Math.Cos(rad) * fromOrigin.X - Math.Sin(rad) * fromOrigin.Y,
                                                orgPoint.Y + Math.Sin(rad) * fromOrigin.X + Math.Cos(rad) * fromOrigin.Y);
            return rotated;
        }
        public static Point2d RotatePoint_Clockwise(Point2d srcPoint, Point2d orgPoint, double radian)
        {
            var rad = radian;
            Point2d fromOrigin = srcPoint - orgPoint;
            var rotated = new Point2d(orgPoint.X + Math.Cos(rad) * fromOrigin.X - Math.Sin(rad) * fromOrigin.Y,
                                                orgPoint.Y + Math.Sin(rad) * fromOrigin.X + Math.Cos(rad) * fromOrigin.Y);
            return rotated;
        }
        /// <summary>
        /// Rectの中心点をorgPointに対して時計回りに回転させる
        /// </summary>
        /// <param name="srcRect"></param>
        /// <param name="orgPoint">回転の原点</param>
        /// <param name="radian">回転させる角度[rad]</param>
        /// <returns></returns>
        public static Rect RotateRectFromPoint_Clockwise(Rect srcRect, Point orgPoint, double radian)
        {
            var srcPoint = srcRect.Center();
            var rotated = RotatePoint_Clockwise(srcPoint, orgPoint, radian);
            return RectFromCenterPoint(rotated, srcRect.Size);
        }
        public static Rect2d RotateRectFromPoint_Clockwise(Rect2d srcRect, Point2d orgPoint, double radian)
        {
            var srcPoint = srcRect.Center();
            var rotated = RotatePoint_Clockwise(srcPoint, orgPoint, radian);
            return RectFromCenterPoint(rotated, srcRect.Size);
        }
        /// <summary>
        /// 画像の回転（90°ごと）に合わせて画像上のRectを回転させる
        /// </summary>
        /// <param name="srcRect"></param>
        /// <param name="imgSize_beforeRotate"></param>
        /// <param name="rotate"></param>
        /// <returns></returns>
        public static Rect RotateRect(Rect srcRect, Size imgSize_beforeRotate, RotateFlags rotate)
        {
            var s = imgSize_beforeRotate;
            var r = srcRect;
            Rect newRect = Rect.Empty;
            switch (rotate)
            {
                case RotateFlags.Rotate90Clockwise:
                    newRect = new Rect(s.Height - r.Bottom, r.X, r.Height, r.Width);
                    break;
                case RotateFlags.Rotate180:
                    newRect = new Rect(s.Width - r.Right, s.Height - r.Bottom, r.Width, r.Height);
                    break;
                case RotateFlags.Rotate90Counterclockwise:
                    newRect = new Rect(r.Y, s.Width - r.Right, r.Height, r.Width);
                    break;            
            }
            return newRect;
        }
        public static Rect2d RotateRect(Rect2d srcRect, Size2d imgSize_beforeRotate, RotateFlags rotate)
        {
            var s = imgSize_beforeRotate;
            var r = srcRect;
            Rect2d newRect = Rect2d.Empty;
            switch (rotate)
            {
                case RotateFlags.Rotate90Clockwise:
                    newRect = new Rect2d(s.Height - r.Bottom, r.X, r.Height, r.Width);
                    break;
                case RotateFlags.Rotate180:
                    newRect = new Rect2d(s.Width - r.Right, s.Height - r.Bottom, r.Width, r.Height);
                    break;
                case RotateFlags.Rotate90Counterclockwise:
                    newRect = new Rect2d(r.Y, s.Width - r.Right, r.Height, r.Width);
                    break;
            }
            return newRect;
        }

        #endregion
        public static void ShowImage(Mat src, string title = "mat", double resizeRatio = 1.0, bool isNormalize=true)
        {
            using var tmp = new Mat(src.Size(), src.Type(), 0);
            src.CopyTo(tmp);
            if (resizeRatio != 1.0) Cv2.Resize(tmp, tmp, Size.Zero, resizeRatio, resizeRatio, InterpolationFlags.Area);
            var newTitle = CheckMinMax(tmp, title);
            using var normalized = isNormalize ? NormalizeTo8bit(tmp) : tmp;
            Cv2.ImShow(newTitle, normalized);
            Cv2.WaitKey();
        }
        public static string CheckMinMax(Mat srcFloat, string message = "maxDefect")
        {
            double min;
            double max;
            srcFloat.MinMaxIdx(out min, out max);
            string mes = message + " max:" + max + " min:" + min;
            Trace.WriteLine(mes);
            return mes;
        }
        private static Mat NormalizeTo8bit(Mat src)
        {
            Mat tmp = new Mat();
            double min, max;
            src.MinMaxLoc(out min, out max);
            if (min < 0)
            {
                src += -min;
                max += -min;
            }
            src *= 255 / max;
            src.ConvertTo(tmp, MatType.CV_8UC1);
            return tmp;
            //return tmp.Normalize(0, 255, NormTypes.MinMax);
        }
        public static Mat MakeScoreRectsInImage(Mat src, List<Rect> rects,  Scalar color, List<double>? scores=null, List<string>? labels = null, int thickness=1, double txtSize = 1.0)
        {
            var dst = src.Channels() == 1 ? src.CvtColor(ColorConversionCodes.GRAY2BGR) : src.Clone();
            for(int i=0;i<rects.Count;i++)
            {
                dst.Rectangle(rects[i], color, thickness);
                var txt = labels == null ? scores == null ? $" "
                                                                           : $"{scores[i]:f2}"
                                                   : scores == null ? $"{labels[i]}" 
                                                                           : $"{labels[i]}:{scores[i]:f2}";
                dst.PutText(txt, rects[i].TopLeft, HersheyFonts.HersheyPlain, txtSize, color, 1);
            }
            return dst;
        }
        public static void MakeScoreRectsInImage(ref Mat src, List<Rect> rects, Scalar color, List<double>? scores = null, List<string>? labels = null, int thickness = 1, double txtSize = 1.0)
        {
            for (int i = 0; i < rects.Count; i++)
            {
                src.Rectangle(rects[i], color, thickness);
                var txt = labels == null ? scores == null ? $" "
                                                                           : $"{scores[i]:f2}"
                                                   : scores == null ? $"{labels[i]}"
                                                                           : $"{labels[i]}:{scores[i]:f2}";
                src.PutText(txt, rects[i].TopLeft, HersheyFonts.HersheyPlain, txtSize, color, 1);
            }
        }

        /// <summary>
        /// Cv2.ConnectedComponentsは再現性のないエラーを出すので、例外が出たらリトライして無理やり利用する処理。
        /// あまり良いやり方ではない
        /// </summary>
        /// <param name="srcByte"></param>
        /// <returns></returns>
        public static ConnectedComponents GetConnectedComponents(Mat srcByte, PixelConnectivity connectivity = PixelConnectivity.Connectivity4)
        {
            ConnectedComponents cc;
            int retry = 2;
            for (int i = 0; i < retry; i++)
            {
                try
                {
                    cc = Cv2.ConnectedComponentsEx(srcByte, connectivity);
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"●ConnectedComponentsExでエラーが出ました {i}回目 {e.ToString()}");
                    continue;
                }
                if (cc != null) return cc;
            }
            throw new Exception($"●ConnectedComponentsに失敗しました {retry}回");
        }
        public class RectWithLabel
        {
            public Rect2d Rect;
            public int Label;
            public RectWithLabel(Rect2d r, int label)
            {
                Rect = r;
                Label = label;
            }
        }
        public static List<Rect> GetConnectedRect(List<Rect> rects, double intersectsThresh = 0.0, int margin = 0)
        {
            var _rects = rects.Select(r => new Rect2d(r.Location, new Size2d(r.Width,r.Height))).ToList();
            var c = MatFunctions.GetRectConnects(_rects, intersectsThresh, margin);
            var groups = rects.Zip(c, (rect, id) => (Rect: rect, Id: id)).ToLookup(d => d.Id);
            var unionRects = new List<Rect>();
            foreach (var g in groups)
            {
                //Trace.WriteLine("Group" + g.Key);
                if (g.Key == -1)
                {
                    foreach (var r in groups[g.Key])
                    {
                        unionRects.Add(r.Rect);
                        Trace.WriteLine(r.Rect);
                    }
                }
                else
                {
                    var union = groups[g.Key].First().Rect;
                    foreach (var r in groups[g.Key])
                    {
                        union = union.Union(r.Rect);
                        //Trace.WriteLine(r.Rect);
                    }
                    unionRects.Add(union);
                }
            }
            return unionRects;
        }
        /// <summary>
        ///  重複部分をラベリング　重複なしなら-1のラベル、重複しているものは正の同じ番号になる
        /// </summary>
        /// <param name="rects"></param>
        /// <param name="intersectsThresh">〇％超の面積が重複していたら重複判定する</param>
        /// <returns></returns>
        public static List<int> GetRectConnects(List<Rect2d> rects, double intersectsThresh=0.0, int margin = 0)
        {
            //重複部分をラベリング　重複なしなら-1のラベル、重複しているものは正の同じ番号になる
            //重複判定マージン
            var size_Add = new Size2d(margin, margin);
            var _rects = margin != 0 ? rects.Select(r => r.AddSize(size_Add)).ToList() : rects;

            var rectWithLabel = _rects.Select(r => new RectWithLabel(r, -1)).ToList();
            var labelList = new List<int>();
            for (int i = 0; i < _rects.Count; i++) labelList.Add(-1);
            int label = 0;
            for (int i = 0; i < _rects.Count; i++)
            {
                var isNewLabel = false;
                for (int j = 0; j < _rects.Count; j++)
                {
                    if (i == j) continue;
                    if (_rects[i].IntersectsMore_Any(_rects[j], intersectsThresh))
                    {
                        if (labelList[i] == -1 && labelList[j] == -1)
                        {
                            isNewLabel = true;
                            labelList[i] = label;
                            labelList[j] = label;
                        }
                        else if (labelList[i] == -1)
                        {
                            labelList[i] = labelList[j];
                        }
                        else if (labelList[j] == -1)
                        {
                            labelList[j] = labelList[i];
                        }
                        else//両方Label登録済みの場合、ラベルをマージ 連番に抜けができるので注意！
                        {
                            var mergeLabelTo = Math.Min(labelList[j], labelList[i]);
                            var mergeLabelFrom = Math.Max(labelList[j], labelList[i]);
                            for(int k = 0; k < labelList.Count; k++)
                            {
                                if (labelList[k] == mergeLabelFrom) labelList[k]  = mergeLabelTo;
                            }
                        }
                    }
                }
                if (isNewLabel)
                {
                    label++;
                    isNewLabel = false;
                }
            }
            //連番抜けの修正
            var labelNum = label-1;
            if (labelNum > 0)
            {
                for (int i = 0; i < labelNum; i++)
                {
                    if (!labelList.Any(l => l == i))
                    {
                        for (int j = 0; j < labelList.Count; j++)
                        {
                            if (labelList[j] > i) labelList[j]--;
                        }
                        labelNum--;
                    }
                }
                //labelList.ForEach(l => Trace.Write(l + ","));
                //Trace.WriteLine("");
            }
            return labelList;
        }
        /// <summary>
        /// 重複しているRectを合成（Union）する
        /// </summary>
        /// <param name="rects"></param>
        /// <param name="intersectsThresh">重複判定する面積しきい値（%）</param>
        /// <returns></returns>
        public static List<Rect> GetUnionRects(List<Rect> rects, double intersectsThresh = 0.0)
        {
            var connectList = GetRectConnects(rects.Select(r=>r.To2d()).ToList(), intersectsThresh);
            var groups = rects.Zip(connectList, (rect, id) => (Rect: rect, Id: id)).ToLookup(d => d.Id);
            var unionRects = new List<Rect>();
            foreach (var g in groups)
            {
                //Trace.WriteLine("Group" + g.Key);
                if (g.Key == -1)
                {
                    foreach (var r in groups[g.Key])
                    {
                        unionRects.Add(r.Rect);
                        Trace.WriteLine(r.Rect);
                    }
                }
                else
                {
                    var union = groups[g.Key].First().Rect;
                    foreach (var r in groups[g.Key])
                    {
                        union = union.Union(r.Rect);
                        //Trace.WriteLine(r.Rect);
                    }
                    unionRects.Add(union);
                }
            }
            return unionRects;
        }
        //unsafe public static double GetContrast1DMaxX(Mat src)
        //{
        //    if (src.Type() != MatType.CV_8UC1) throw new TypeAccessException($"●MatType.CV_8UC1のみ対応です:{src.Type()}");
        //    double aveOfMaxContrast = 0;
        //    byte* u = src.DataPointer;
        //    int tmpContrast;
        //    int maxContrast = 0;
        //    for (int y = 0; y < src.Height; y++)
        //    {
        //        for (int x = 1; x < src.Width; x++)
        //        {
        //            if (x == 1) u++;
        //            tmpContrast = *u - *(u - 1);
        //            if (tmpContrast < 0) tmpContrast *= -1;
        //            if (tmpContrast > maxContrast)
        //            {
        //                maxContrast = tmpContrast;
        //            }
        //            u++;
        //        }
        //        aveOfMaxContrast += maxContrast;
        //        maxContrast = 0;
        //    }
        //    aveOfMaxContrast /= src.Height;
        //    return aveOfMaxContrast;
        //}
    }
}
