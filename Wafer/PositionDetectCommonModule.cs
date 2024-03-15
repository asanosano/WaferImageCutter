using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Wafer.ChipResult;

namespace ServerSoftware
{
    public class PositionDetectCommonModule
    {
        public List<AutoInspectResult> UnFlagFloatedImg(Mat srcImg, List<AutoInspectResult> aiResults, int maskTh = 230, int th = 10)
        {
            List<Mat> imgs = GetAllImgs(srcImg, aiResults);
            for (int i = 0; i < imgs.Count(); i++)
            {
                Mat mask = imgs[i].Threshold(maskTh, 255, ThresholdTypes.BinaryInv);
                double mean = imgs[i].Mean(mask).Val0;
                if (mean < th)
                {
                    aiResults[i].IsGetSecondImage = false;
                }
            }
            return aiResults;
        }
        private List<Mat> GetAllImgs(Mat srcImg, List<AutoInspectResult> inspectResults)
        {
            var imgs = new List<Mat>();
            foreach (var inspectResult in inspectResults)
            {
                imgs.Add(srcImg[inspectResult.ImgRect]);
            }
            return imgs;
        }
    }
}
