using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using System.Diagnostics;
using System.IO;

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
        public static Rect Shift(this Rect r, int shiftX, int shiftY)
        {
            return new Rect(r.X + shiftX, r.Y + shiftY, r.Width, r.Height);
        }
        public static Point Center(this Rect r)
        {
            return new Point((int)(0.5 + (r.Left + r.Right) * 0.5), (int)(0.5 + (r.Top + r.Bottom) * 0.5));
        }
        public static Point Mid(this Point p1, Point p2)
        {
            return new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
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

        public static Rect RectFromCenterPoint(int x, int y, int w, int h)
        {
            return new Rect(x - w / 2, y - h / 2, w, h);
        }
        public static Rect RectFromCenterPoint(Point p, Size s)
        {
            return new Rect(p.X - s.Width / 2, p.Y - s.Height / 2, s.Width, s.Height);
        }
        public static bool IsOver(this Rect rect, Size size)
        {
            return (rect.X < 0 || rect.Y < 0 || rect.Right > size.Width || rect.Bottom > size.Height);
        }
        public static void ThrowIfRectOverSize(Size size, Rect rect, string message = "")
        {
            if (rect.IsOver(size)) throw new ArgumentOutOfRangeException($"{message}/rectがsizeをはみ出していますRect:X:{rect.X},Y:{rect.Y},W:{rect.Width},H:{rect.Height} Size:W:{size.Width},H{size.Height}");
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
        public static Rect2d ToDouble(this Rect r)
        {
            return new Rect2d(r.X, r.Y, r.Width, r.Height);
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
        #endregion
        public static void ShowImage(Mat src, string title = "mat", double resizeRatio = 1.0)
        {
            var tmp = new Mat(src.Size(), src.Type(), 0);
            //var tmp = new Mat();
            //tmp = src.Clone();
            src.CopyTo(tmp);
            if (resizeRatio != 1.0) Cv2.Resize(tmp, tmp, Size.Zero, resizeRatio, resizeRatio, InterpolationFlags.Area);
            var newTitle = CheckMinMax(tmp, title);
            Cv2.ImShow(newTitle, NormalizeTo8bit(tmp));
            Cv2.WaitKey();
            //tmp.Dispose();
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
