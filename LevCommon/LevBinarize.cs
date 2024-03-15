using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Xml.Linq;

namespace LevCommon
{
    public enum BinarizeTypes { Sinmple, Org, Adaptive, OHTU }

    public class LevBinarize
    {
        class HistData
        {
            public int GrayLevel;
            public int PointCount = 0;
            public int ObjectCount;
            public HistData(int grayLevel)
            {
                GrayLevel = grayLevel;
            }
        }
        public class HistDataComparer : IComparer
        {
            public int Compare(object t1, object t2)
            {
                HistData d1 = (HistData)t1;
                HistData d2 = (HistData)t2;

                return d1.PointCount - d2.PointCount;
            }
        }

        public ShowImageFunc AddImageFunc = null;
        void AddImage(Mat mat, string title)
        {
            if (AddImageFunc == null)
            {
                return;
            }
            AddImageFunc(mat, title);
            //DkImageViewer viewer = new DkImageViewer();
            //
            //panel.Children.Add(viewer);
            //viewer.Title = title;
            //viewer.SetSourceImage(matImage);
            //viewer.Draw();
        }
        public Mat? Execute(LevCoreParammeter prData, Mat srcMat)
        {
            Mat binaryMat = null;
            switch (prData.BinarizeType)
            {
                case BinarizeTypes.OHTU:
                    binaryMat = srcMat.Threshold(0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary);
                    break;
                case BinarizeTypes.Adaptive:
                    int size = prData.BinTh; //仮に入れてる値
                    binaryMat = srcMat.AdaptiveThreshold(255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, size, 3);
                    break;
                case BinarizeTypes.Sinmple:
                    int th = prData.BinTh;
                    binaryMat = srcMat.Threshold(th, 255, ThresholdTypes.Binary);
                    break;
                case BinarizeTypes.Org:
                    binaryMat = MTh(prData, srcMat);
                    break;
                default:
                    break;
            }
            return binaryMat;
        }
        Mat MTh(LevCoreParammeter prData, Mat srcMat)
        {
            //Mat mat = srcMat.Clone();
            //HistData[] histDatas = new HistData[256];
            //
            //IComparer hstDataComparer = new HistDataComparer();
            //
            //for (int i = 0; i < 256; i++)
            //{
            //    histDatas[i] = new HistData(i);
            //}
            //for (int y = 0; y < mat.Cols; y++)
            //{
            //    for (int x = 0; x < mat.Rows; x++)
            //    {
            //        byte b = mat.At<byte>(y, x);
            //        histDatas[b].PointCount++;
            //    }
            //}
            //Array.Sort(histDatas, hstDataComparer);

            //int rth = prData.BinTh;
            //for(int th = 200; th >10 ; th--)
            //{
            //    mat = srcMat.Threshold(th, 255, ThresholdTypes.Binary);
            //    var erodeMat = new Mat(mat.Rows, mat.Cols, MatType.CV_8UC1);
            //    var dilateMat = new Mat(mat.Rows, mat.Cols, MatType.CV_8UC1);
            //    Cv2.Erode(mat, erodeMat, null);
            //    Cv2.Dilate(erodeMat, mat, null);
            //    mat = Mask(prData, mat);
            //    ConnectedComponents cc = Cv2.ConnectedComponentsEx(mat);
            //    if(cc.Blobs.Count-1 >2)
            //    {                   
            //        break;
            //    }
            //    rth = th;
            //    // Debug.WriteLine($"{hisData.GrayLevel} {cc.Blobs.Count}");
            //}
            //
            Mat s2 = srcMat.Clone();
            double sum = 0;
            double sum2 = 0;
            double count = 0;
            double bsum = 0;
            double bsum2 = 0;
            double bcount = 0;
            int tmpTh = prData.BinTh;
            for (int y = 0; y < s2.Cols; y++)
            {
                for (int x = 0; x < s2.Rows; x++)
                {
                    int b = s2.At<byte>(y, x);
                    if (b >= tmpTh)
                    {
                        s2.At<byte>(y, x) = 255;
                        sum += b;
                        sum2 += b * b;
                        count++;
                    }
                    else
                    {
                        bsum += b;
                        bsum2 += b * b;
                        bcount++;
                        s2.At<byte>(y, x) = 0;
                    }
                }
            }
            var a = sum / count;
            var v = sum2 / count - a * a; ;
            var s = Math.Sqrt(v);

            var ba = bsum / bcount;
            var bv = bsum2 / bcount - ba * ba; ;
            var bs = Math.Sqrt(bv);


            //AddImage(panel, (Mat)s2, (string)"s2");
            //AddImage(panel, (Mat)srcMat, (string)"XX");
            //
            //Mat mask = srcMat.Threshold(15, 255, ThresholdTypes.Binary);
            //AddImage(panel, (Mat)mask, (string)"XX");
            //
            //Mat meanMat = new Mat();
            //Mat stdDevMat = new Mat();
            //srcMat.MeanStdDev(meanMat, stdDevMat, mask);
            //var mean = meanMat.At<byte>(0, 0);
            //var stdDev = stdDevMat.At<byte>(0, 0);

            double k = prData.BinTh2;
            int rth = (int)(a + k * s);
            //  rth = (int)(a );
            rth = Math.Min(rth, 50);

            int brth = (int)(ba + bs * 9.0);

            Mat binMat = srcMat.Threshold(rth, 255, ThresholdTypes.Binary);

            return binMat;
        }
        static public Mat MThOLD(LevCoreParammeter prData, Mat srcMat, System.Windows.Controls.Panel panel)
        {
            //Mat mat = srcMat.Clone();
            //HistData[] histDatas = new HistData[256];
            //
            //IComparer hstDataComparer = new HistDataComparer();
            //
            //for (int i = 0; i < 256; i++)
            //{
            //    histDatas[i] = new HistData(i);
            //}
            //for (int y = 0; y < mat.Cols; y++)
            //{
            //    for (int x = 0; x < mat.Rows; x++)
            //    {
            //        byte b = mat.At<byte>(y, x);
            //        histDatas[b].PointCount++;
            //    }
            //}
            //Array.Sort(histDatas, hstDataComparer);

            //int rth = prData.BinTh;
            //for(int th = 200; th >10 ; th--)
            //{
            //    mat = srcMat.Threshold(th, 255, ThresholdTypes.Binary);
            //    var erodeMat = new Mat(mat.Rows, mat.Cols, MatType.CV_8UC1);
            //    var dilateMat = new Mat(mat.Rows, mat.Cols, MatType.CV_8UC1);
            //    Cv2.Erode(mat, erodeMat, null);
            //    Cv2.Dilate(erodeMat, mat, null);
            //    mat = Mask(prData, mat);
            //    ConnectedComponents cc = Cv2.ConnectedComponentsEx(mat);
            //    if(cc.Blobs.Count-1 >2)
            //    {                   
            //        break;
            //    }
            //    rth = th;
            //    // Debug.WriteLine($"{hisData.GrayLevel} {cc.Blobs.Count}");
            //}
            //
            Mat s2 = srcMat.Clone();
            double sum = 0;
            double sum2 = 0;
            double count = 0;
            int tmpTh = prData.BinTh;
            for (int y = 0; y < s2.Cols; y++)
            {
                for (int x = 0; x < s2.Rows; x++)
                {
                    int b = s2.At<byte>(y, x);
                    if (b >= tmpTh)
                    {
                        s2.At<byte>(y, x) = 255;
                        sum += b;
                        sum2 += b * b;
                        count++;
                    }
                    else
                    {
                        s2.At<byte>(y, x) = 0;
                    }
                }
            }
            var a = sum / count;
            var v = sum2 / count - a * a; ;
            var s = Math.Sqrt(v);
            //AddImage(panel, (Mat)s2, (string)"s2");
            //AddImage(panel, (Mat)srcMat, (string)"XX");
            //
            //Mat mask = srcMat.Threshold(15, 255, ThresholdTypes.Binary);
            //AddImage(panel, (Mat)mask, (string)"XX");
            //
            //Mat meanMat = new Mat();
            //Mat stdDevMat = new Mat();
            //srcMat.MeanStdDev(meanMat, stdDevMat, mask);
            //var mean = meanMat.At<byte>(0, 0);
            //var stdDev = stdDevMat.At<byte>(0, 0);

            double k = prData.BinTh2;
            int rth = (int)(a + k * s);
            rth = Math.Min(rth, 50);
            Mat binMat = srcMat.Threshold(rth, 255, ThresholdTypes.Binary);

            return binMat;
        }

    }
}
