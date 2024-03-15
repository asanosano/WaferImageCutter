using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace Wafer.ChipResult
{
    /// <summary>
    ///  切り出し画像データ
    /// </summary>
    public class PartialImageList
    {
        /// <summary>
        /// 領域名
        /// </summary>
        public WorkInfo WI;
        public Mat SrcImg;
        public List<Rect> Rects;
        public double ResizeRate;
        public string AreaID;
        public string Standard;
        public string CutImageAreaInformationFilePath = "";
        public Size ResizedSize;
        public string CutImageSavePath = "";
        /// <summary>
        /// 切り出し画像データの初期化
        /// </summary>
        /// <param name="info">ワーク情報</param>
        /// <param name="img">元画像</param>
        /// <param name="rects">切り出し範囲</param>
        /// <param name="resizeRate">切り出すときの縮小倍率</param>
        /// <param name="areaID">エリアID</param>
        /// <param name="standard">規格（ABC...）</param>
        /// <param name="cutAreaInfomationPath">切り出し設定ファイルの場所</param>
        public PartialImageList(WorkInfo info, Mat img, List<Rect> rects, double resizeRate, string areaID, string standard, string cutAreaInfomationPath)
        {
            SrcImg = img;
            WI = info;
            Rects = rects;
            ResizeRate = resizeRate;
            AreaID = areaID;
            Standard = standard;
            CutImageAreaInformationFilePath = cutAreaInfomationPath;
            SetResizedSize();
        }
        /// <summary>
        /// 元画像からrectの範囲を切り出し
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public Mat GetImg(Rect rect)
        {
            if (rect.X + rect.Width > SrcImg.Width
                || rect.Y + rect.Height > SrcImg.Height
                || rect.X < 0
                || rect.Y < 0)
            {
                throw new ArgumentException("PartialImageListエラー：切り出し領域がはみ出しています" + "src:" + SrcImg.Width + "," + SrcImg.Height + ",rect:" + rect.X + "," + rect.Y + rect.Width + "," + rect.Height + CutImageAreaInformationFilePath);
            }
            Mat tmp = new Mat(SrcImg, rect);
            Mat img = new Mat();
            //Cv2.Resize(tmp, img, Size.Zero, ResizeRate, ResizeRate, InterpolationFlags.Linear);
            //Resizeは倍率指定とサイズ指定で結果が変わるので、ファイル保存切り出しと合わせる
            //Cv2.Resize(tmp, tmp, Size.Zero, ResizeRate, ResizeRate, InterpolationFlags.Linear);
            int resizeWidth = (int)Math.Round(rect.Width * ResizeRate);
            int resizeHeight = (int)Math.Round(rect.Height * ResizeRate);
            Cv2.Resize(tmp, img, new Size(resizeWidth, resizeHeight), 0, 0, InterpolationFlags.Area);
            tmp.Dispose();
            return img;
        }
        public Mat GetImgByFloat(Rect rect)
        {
            if (rect.X + rect.Width > SrcImg.Width
                || rect.Y + rect.Height > SrcImg.Height
                || rect.X < 0
                || rect.Y < 0)
            {
                throw new ArgumentException("PartialImageListエラー：切り出し領域がはみ出しています" + "src:" + SrcImg.Width + "," + SrcImg.Height + ",rect:" + rect.X + "," + rect.Y + rect.Width + "," + rect.Height + CutImageAreaInformationFilePath);
            }
            Mat tmp = new Mat(SrcImg, rect);
            Mat img = new Mat();
            //Resizeは倍率指定とサイズ指定で結果が変わるので、ファイル保存切り出しと合わせる
            //Cv2.Resize(tmp, tmp, Size.Zero, ResizeRate, ResizeRate, InterpolationFlags.Linear);
            int resizeWidth = (int)Math.Round(rect.Width * ResizeRate);
            int resizeHeight = (int)Math.Round(rect.Height * ResizeRate);
            Cv2.Resize(tmp, tmp, new Size(resizeWidth, resizeHeight), 0, 0, InterpolationFlags.Area);
            tmp.ConvertTo(img, MatType.CV_32FC1);
            tmp.Dispose();
            return img;
        }
        /// <summary>
        /// 元画像からn番目の範囲を切り出し
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public Mat GetImg(int n)
        {
            return GetImg(Rects[n]);
        }
        /// <summary>
        /// 元画像からすべての切り出し画像を取得
        /// </summary>
        /// <returns></returns>
        public List<Mat> GetAllImgs()
        {
            var result = new List<Mat>();
            foreach (var r in Rects) result.Add(GetImg(r));
            return result;
        }
        public List<(Mat img, Rect rect)> GetAllImgsWithRects()
        {
            var result = new List<(Mat img, Rect rect)>();
            foreach (var r in Rects) result.Add((GetImg(r), r));
            return result;
        }
        public List<AutoInspectResult> MakeAutoInspectResults(float[] scores, List<Rect> scoreRects = null)
        {
            if (scoreRects == null) scoreRects = Rects;
            var results = new List<AutoInspectResult>();
            for (int i = 0; i < scores.Length; i++)
            {
                results.Add(new AutoInspectResult(this, i, scores[i], scoreRects[i]));
            }
            return results;
        }

        private void SetResizedSize()
        {
            ResizedSize = new Size((int)(Rects[0].Width * ResizeRate), (int)(Rects[0].Height * ResizeRate));
        }
        //public IEnumerator<Rect> GetEnumerator()
        //{
        //    foreach (Rect part in this.List)
        //    {
        //        yield return part;  // ここでパーツを返す
        //    }
        //}
    }
}
