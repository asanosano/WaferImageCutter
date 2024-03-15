using IjhCommonUtility;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Size = OpenCvSharp.Size;
using Rect = OpenCvSharp.Rect;
using System.Runtime.Serialization;

namespace Wafer
{

    /// <summary>
    /// 二値化による検出処理
    /// </summary>
    [DataContract]
    public class WaferInspectBinaryParameter
    {
        [DataMember]
        public Size CutImageSize = new Size(500, 500);
        [DataMember]
        public Size CutImageMarginSize = new Size(100, 100);
        [DataMember]
        public int PixValThresh = 170;
        [DataMember]
        public int WidthThresh = 3;
        [DataMember]
        public int BlurFilterSize = 0;
        [DataMember]
        public int OverlapMarginSize = 50;
        [DataMember]
        public double OverlapAreaRatio = 0.01;


        //非パラメータ
        public string readPath = "";
        public WaferInspectBinaryParameter() { }
        public WaferInspectBinaryParameter(string xmlPath)
        {
            DataContractReaderWriter.ReadXml_WithoutException(this, xmlPath);
            readPath = xmlPath;
        }
        public bool Save(string xmlPath)
        {
            return DataContractReaderWriter.WriteXml_WithoutException(this, xmlPath);
        }
    }
    public class WaferInspectByBinary
    {
        public WaferInspectBinaryParameter InspBinaryParam = new WaferInspectBinaryParameter();
        public WaferInspectByBinary(string InspParamPath)
        {
            this.InspBinaryParam = new WaferInspectBinaryParameter(InspParamPath);
            //this.InspBinaryParam = new WaferInspectBinaryParameter();
            //this.InspBinaryParam.Save(InspParamPath);
        }

        public void InspectWafer_Rule(Mat img, string imgPath, string outputDir, int i, CutAreaParam cutParam)
        {
            double dupTh = 0.01;//重複除去の閾値（面積比）
            var sw = Stopwatch.StartNew();
            if (cutParam.CutAreas[0] == Rect.Empty) return;
            var fileNameBase = Path.GetFileNameWithoutExtension(imgPath);
            var outputDir2 = Path.Combine(outputDir, fileNameBase);
            Directory.CreateDirectory(outputDir2);
            //OKNG判定, 画像保存
            var rects = new List<Rect>();

            foreach (var area in cutParam.CutAreas)
            {
                rects.AddRange(WaferInspectCommon.CutToRect(area.Size, this.InspBinaryParam.CutImageSize, this.InspBinaryParam.CutImageMarginSize).Select(a => a.Shift(area.X, area.Y)).ToList());
            }
            List<(int Id, Rect Rect, Mat Img)> cutResults = rects.Select(r => (i, r, MatFunctions.RoiToMat(img, r))).ToList();
            var inspResults = new List<ResultClass>();
            var defectId = 0;
            foreach (var cutResult in cutResults)
            {
                var ruleResults = GetRuleResult(cutResult.Img);
                if (ruleResults.Count == 0) continue;
                var bestScore = ruleResults.Select(r => r.rect.Area()).Max();
                var cutImgPath = Path.Combine(outputDir2, $"i{i}_d{defectId:d3}_RX{cutResult.Rect.X}_RY{cutResult.Rect.Y}_RW{cutResult.Rect.Width}_RH{cutResult.Rect.Height}_{bestScore}.tif");
                cutResult.Img.SaveImage(cutImgPath);
                foreach (var r in ruleResults)
                {
                    var score = Math.Max(r.rect.Size.Width, r.rect.Size.Height);//長辺サイズ
                    var globalRect = r.rect.Add(cutResult.Rect.Location);
                    inspResults.Add(new ResultClass(i, "", "", defectId, globalRect, score, cutImgPath));
                }
            }

            //重複マージ
            var mergedResults = WaferInspectCommon.MergeDuplicate(inspResults, dupTh, this.InspBinaryParam.OverlapMarginSize);

            //NG結果の保存
            var csvHeader = ResultClass.GetHeader();
            var csvContents_tmp = new List<string>() { csvHeader };
            csvContents_tmp.AddRange(mergedResults.Select(m => m.GetString()));
            File.WriteAllLines(Path.Combine(outputDir2, "result.csv"), csvContents_tmp, Encoding.UTF8);

            //デバッグ用
            var csvContents_dbg = new List<string>() { csvHeader };
            csvContents_dbg.AddRange(inspResults.Select(m => m.GetString()));
            File.WriteAllLines(Path.Combine(outputDir2, "debug.csv"), csvContents_dbg, Encoding.UTF8);
        }

        //ルールベース検出アルゴ
        public List<(int defectId, Rect rect)> GetRuleResult(Mat img)
        {
            double pixThresh = this.InspBinaryParam.PixValThresh;
            int areaThresh = this.InspBinaryParam.WidthThresh;
            int blurSize = this.InspBinaryParam.BlurFilterSize;
            var results = new List<(int defectId, Rect rect)>();
            var blured = new Mat();
            var threshed = new Mat();
            //ぼかしを入れてアンカーパターン過検出対策
            if (blurSize > 1)
            {
                Cv2.GaussianBlur(img, blured, new Size(blurSize, blurSize), -1);
            }
            else blured = img.Clone();
            //二値化
            Cv2.Threshold(blured, threshed, pixThresh, 255, ThresholdTypes.Binary);
            //二値化で検出できた部分を少し太らせる（Dilate）
            Cv2.MorphologyEx(threshed, threshed, MorphTypes.Dilate, null, null, 1);
            var cc = Cv2.ConnectedComponentsEx(threshed);
            var blobs = cc.Blobs.Skip(1).Where(b => b.Area > areaThresh);
            var blobId = 0;
            foreach (var blob in blobs)
            {
                results.Add((blobId, blob.Rect));
                blobId++;
            }
            blured.Dispose();
            threshed.Dispose();
            return results;
        }


    }
}
