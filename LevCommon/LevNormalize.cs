using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LevCommon
{
    public enum NormalizeTypes { Org, None, EqualizeHist, DCT, }

    public class LevNormalize
    {
        public void Execute(NormalizeTypes normalizeType, ref Mat refMat, ref Mat srcMat)
        {
            switch (normalizeType)
            {
                case NormalizeTypes.DCT:
                    refMat = NormalizeDct(refMat);
                    srcMat = NormalizeDct(srcMat);
                    break;
                case NormalizeTypes.EqualizeHist:
                    refMat = NormalizeEqualizeHist(refMat);
                    srcMat = NormalizeEqualizeHist(srcMat);
                    break;
                case NormalizeTypes.Org:
                    refMat = NormalizeDct(refMat);
                    srcMat = NormalizeDct(srcMat);
                    Nor(ref refMat, ref srcMat);
                    refMat = NormalizeDct(refMat);
                    srcMat = NormalizeDct(srcMat);
                    //Mat mat = srcMat.Clone();
                    //for(int y = 0;y<mat.Cols;y++)
                    //{
                    //    for(int x=0;x<mat.Rows;x++)
                    //    {
                    //        byte b = mat.At<byte>(y, x);
                    //        byte bb = (byte)(b * 0.8);
                    //        mat.Set(y, x, bb);
                    //    }
                    //}
                    //Nor(ref mat, ref srcMat);
                    //refMat = mat;
                    break;
                default:
                    break;
            }
            return;
        }
        void Nor(ref Mat refMat, ref Mat srcMat)
        {
            double th = 10;
            int totalSize = (int)refMat.Total();

            double[] refArray = GrayMat2DoubleArray(refMat);
            double[] srcArray = GrayMat2DoubleArray(srcMat);
            List<Tuple<double, double>> rateList = new List<Tuple<double, double>>();

            double min = double.MaxValue;
            double max = double.MinValue;

            for (int i = 0; i < totalSize; i++)
            {
                if (refArray[i] > 255 - th || srcArray[i] > 255 - th)
                {
                    continue;
                }
                else if (refArray[i] < th || srcArray[i] < th)
                {
                    continue;
                }
                double tmin = (refArray[i] - th) / srcArray[i];
                double tmax = (refArray[i] + th) / srcArray[i];
                tmin = Math.Round(tmin, 2);
                tmax = Math.Round(tmax, 2);
                rateList.Add(new Tuple<double, double>(tmin, tmax));
                min = Math.Min(tmin, min);
                max = Math.Max(tmax, max);

            }
            int countMax = 0;
            double countMaxRate = -1;
            for (double rate = min; rate <= max; rate += 0.01)
            {
                int count = 0;
                foreach (var t in rateList)
                {
                    if (rate > t.Item1 && rate < t.Item2)
                    {
                        count++;
                    }
                }
                if (count >= countMax)
                {
                    countMax = count;
                    countMaxRate = rate;
                }
            }


            for (int i = 0; i < totalSize; i++)
            {
                srcArray[i] = srcArray[i] * countMaxRate;
            }
            srcMat = DoubleArray2GrayMat(srcArray, refMat.Cols, refMat.Rows);
        }

        double[] GrayMat2DoubleArray(Mat srcMat)
        {
            int totalSize = (int)srcMat.Total();
            double[] doubleArray = new double[totalSize];

            byte[] char_array_dest = new byte[totalSize];

            Marshal.Copy(srcMat.Data, char_array_dest, 0, char_array_dest.Length);

            //Buffer.BlockCopy(char_array_dest, 0, short_array_dest, 0, short_array_dest.Length/*配列数*/);

            for (int i = 0; i < totalSize; i++)
            {
                doubleArray[i] = Convert.ToDouble(char_array_dest[i]);
            }
            return doubleArray;
        }
        Mat? DoubleArray2GrayMat(double[] doubleArray, int xsize, int ysize)
        {
            int totalSize = doubleArray.Length;

            if (totalSize != xsize * ysize)
            {
                return null;
            }
            byte[] char_array = new byte[totalSize];

            for (int i = 0; i < totalSize; i++)
            {
                double min = Math.Min(255, doubleArray[i]);
                byte t = (byte)Math.Max(0, min);
                char_array[i] = t;
            }
            Mat dstMat = new Mat(ysize, xsize, MatType.CV_8UC1);
            Marshal.Copy(char_array, 0, dstMat.Data, totalSize);
            return dstMat;
        }

        void Nor2(ref Mat refMat, ref Mat srcMat)
        {
            int totalSize = (int)refMat.Total();

            double[] refArray = GrayMat2DoubleArray(refMat);
            double[] srcArray = GrayMat2DoubleArray(srcMat);
            List<double> rateList = new List<double>();

            for (int i = 0; i < totalSize; i++)
            {
                if (refArray[i] == 255 || srcArray[i] == 255)
                {
                    continue;
                }
                else if (refArray[i] == 0 || srcArray[i] == 0)
                {
                    continue;
                }

                rateList.Add(refArray[i] / srcArray[i]);
            }
            double[] rateArray = rateList.ToArray();
            Array.Sort(rateArray);
            double medianRate = rateArray[rateArray.Length / 2];

            for (int i = 0; i < totalSize; i++)
            {
                srcArray[i] = srcArray[i] * medianRate;
            }
            srcMat = DoubleArray2GrayMat(srcArray, refMat.Cols, refMat.Rows);
        }

        Mat NormalizeEqualizeHist(Mat srcMat)
        {
            Mat dstMat = new Mat();
            Cv2.EqualizeHist(srcMat, dstMat);
            //  Cv2.CreateCLAHE();
            return dstMat;
        }

        Mat NormalizeDct(Mat srcMat)
        {
            float ave = 200;
            Mat tmpMat = new Mat();
            Mat tmpMat2 = new Mat();

            int xsize = Cv2.GetOptimalDFTSize(srcMat.Width);
            int ysize = Cv2.GetOptimalDFTSize(srcMat.Height);

            if (srcMat.Width != xsize || srcMat.Height != ysize)
            {
                tmpMat = srcMat.Resize(new OpenCvSharp.Size(xsize, ysize), 0, 0, OpenCvSharp.InterpolationFlags.Cubic);
            }
            else
            {
                tmpMat = srcMat.Clone();
            }

            tmpMat.ConvertTo(tmpMat2, MatType.CV_32FC1);
            tmpMat2 = tmpMat2.Dft(DftFlags.ComplexOutput);

            var t = tmpMat2.Get<Vec2f>(0, 0);
            t.Item0 = (float)ave * (float)xsize * (float)ysize;
            tmpMat2.Set<Vec2f>(0, 0, t);

            tmpMat2 = tmpMat2.Dft(DftFlags.Inverse | DftFlags.Scale);

            tmpMat = tmpMat2.ExtractChannel(0);
            //tmpMat.MinMaxIdx(out double min4, out double max4);
            tmpMat.ConvertTo(tmpMat2, MatType.CV_8UC1);
            return tmpMat2;
        }

    }
}
