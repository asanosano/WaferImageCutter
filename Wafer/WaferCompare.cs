using IjhCommonUtility;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Wafer
{
    [DataContract]
    internal class WaferCompareParameter
    {
        [DataMember]
        public string ResultSearchName = "*detailResult.csv";
        [DataMember]
        public double OverlapMarginSize = 20;
        [DataMember]
        public double OverlapAreaRatio = 0.0;
        public WaferCompareParameter() { }

        public WaferCompareParameter(string xmlPath)
        {
            DataContractReaderWriter.ReadXml_WithoutException(this, xmlPath);
            readPath = xmlPath;
        }
        public bool Save()
        {
            return DataContractReaderWriter.WriteXml_WithoutException(this, readPath);
        }
        public string readPath = "";
    }
    internal class WaferCompare
    {
        public WaferCompareParameter param;
        public WaferCompare(string xmlPath) 
        {
            param = new WaferCompareParameter(xmlPath);
        }
        public void Compare(string srcDir1, string srcDir2)
        {
            var csvName = param.ResultSearchName;
            var margin = param.OverlapMarginSize;
            var intersectRate = param.OverlapAreaRatio;
            var csvpath1 = Directory.GetFiles(srcDir1, csvName).FirstOrDefault();
            var csvpath2 = Directory.GetFiles(srcDir2, csvName).FirstOrDefault();
            var outputPathCsv = Path.Combine(srcDir1, $"compareWaferWith_{Path.GetFileName(srcDir2)}.csv");
            var outputPathImg = Path.Combine(srcDir1, $"compareWaferWith_{Path.GetFileName(srcDir2)}.tif");
            var results1 = File.ReadAllLines(csvpath1).Skip(1).Select(c => FromDetailResultToResultClass(c));
            var results2 = File.ReadAllLines(csvpath2).Skip(1).Select(c => FromDetailResultToResultClass(c));
            (var labels1, var labels2) = this.GetOverlappedRect(results1.Select(r => r.Rect).ToList(), results2.Select(r => r.Rect).ToList(), new Size2d(margin, margin), intersectRate);
            var labelWithResults1 = results1.Zip(labels1, (r, l) => (Result: r, Label: l)).ToList();
            var labelWithResults2 = results2.Zip(labels2, (r, l) => (Result: r, Label: l)).ToList();
            //src1,2の情報を付与
            var labelWithResultsAll = new List<(ResultClass Result, int Label, int Src)>();
            labelWithResultsAll.AddRange(labelWithResults1.Select(l => (l.Result, l.Label, 1)));
            labelWithResultsAll.AddRange(labelWithResults2.Select(l => (l.Result, l.Label, 2)));
            var overlapped = labelWithResultsAll.OrderBy(l => l.Label).Where(l => l.Label != -1);
            var unique = labelWithResultsAll.OrderBy(l => l.Label).Where(l => l.Label == -1);

            //csv保存
            var csvData = new List<string>() { "差分比較," + ResultClass.GetHeader() };
            csvData.AddRange(unique.Select(o => $"src{o.Src}," + o.Result.GetString()));
            csvData.AddRange(overlapped.Select(o => $"重複{o.Label}_src{o.Src}," + o.Result.GetString()));
            File.WriteAllLines(outputPathCsv, csvData, Encoding.UTF8);

            //画像保存
            var imgName = "*score.tif";
            var imgPath = Directory.GetFiles(srcDir1, imgName).FirstOrDefault();
            var img = new Mat(imgPath, ImreadModes.Color);
            var overlapped1 = overlapped.Where(o => o.Src == 1).Select(o => o.Result).ToList();
            var overlapped2 = overlapped.Where(o => o.Src == 2).Select(o => o.Result).ToList();
            var unique2 = unique.Where(u => u.Src == 2).Select(o => o.Result).ToList();
            if (overlapped1.Count > 0) ResultClass.DrawResults(ref img, overlapped1, new Scalar(255, 255, 0));
            if (overlapped2.Count > 0) ResultClass.DrawResults(ref img, overlapped2, new Scalar(0, 255, 255));
            if (unique2.Count() > 0) ResultClass.DrawResults(ref img, unique2, new Scalar(0, 255, 0));
            img.SaveImage(outputPathImg);
        }
        private ResultClass FromDetailResultToResultClass(string csvStr)
        {
            var tmp = csvStr.Split(',');
            var StripId = int.TryParse(tmp[0], out var w) ? w : 0;
            var ChipName = tmp[0];
            var AreaName = tmp[1];
            var Rect = new Rect2d(double.Parse(tmp[2]), double.Parse(tmp[3]), double.Parse(tmp[4]), double.Parse(tmp[5]));
            var Score = double.Parse(tmp[6]);
            var DefectId = int.TryParse(tmp[7], out var d) ? d : 0;
            var ImgPath = tmp[8];
            return new ResultClass(99, ChipName, AreaName, DefectId, Rect, Score, ImgPath);
        }
        private (List<int> connects1, List<int> connects2) GetOverlappedRect(List<Rect2d> rects1, List<Rect2d> rects2, Size2d marginSize, double intersectRate = 0.5)
        {
            int labelNum = 0;
            bool isNewLabel = false;
            var labels1 = new List<int>();
            for (int i = 0; i < rects1.Count; i++) labels1.Add(-1);
            var labels2 = new List<int>();
            for (int i = 0; i < rects2.Count; i++) labels2.Add(-1);

            for (int i = 0; i < rects1.Count; i++)
            {
                for (int j = 0; j < rects2.Count; j++)
                {
                    if (rects1[i].AddSize(marginSize).IntersectsMore_Any(rects2[j], intersectRate))
                    {
                        if (labels2[j] == -1)
                        {
                            labels1[i] = labelNum;
                            labels2[j] = labelNum;
                            isNewLabel = true;
                        }
                        else
                        {
                            labels1[i] = labels2[j];
                        }
                    }
                }
                if (isNewLabel)
                {
                    labelNum++;
                    isNewLabel = false;
                }

            }
            return (labels1, labels2);
        }
    }
}
