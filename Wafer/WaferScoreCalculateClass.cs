using DocumentFormat.OpenXml.Presentation;
using IjhCommonUtility;
using MathNet.Numerics.Integration;
using OpenCvSharp;
using OpenCvSharp.Flann;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Wafer
{
    public class WaferScoreCalculateClass
    {
        public enum RotateAngle { None, Rotate90, Rotate180, Rotate270 }
        enum AreaCategory {Filter, Island, DummyIsland}
        public class WaferResultClass//作りかけ
        {
            public string WaferLotNo = "";
            public string WaferFileName = "";
            public string OrgImgNo = "";
            public Rect Rect_OrgImg = Rect.Empty;
            public double Score = 0.0;
            public Rect Rect = Rect.Empty;
            public string Label = "";
            public string ChipId = "";
            public string AreaInChipId = "";
            public WaferResultClass(ResultClass r, Rect rect)
            {
                this.OrgImgNo = r.StripId.ToString();
                this.Score = r.Score;
                this.Rect_OrgImg = r.Rect.ToInt();
            }
        }
        public class WaferScore
        {
            public List<ChipScore> ChipScores = new List<ChipScore>();
            public double Score;
            public List<ResultClass> OutOfChipResults = new List<ResultClass>();
            public WaferScore(List<string> ChipParamPaths, Point basePoint)
            {
                foreach (var cp in ChipParamPaths)
                {
                    var p = new ChipScoreParameter(cp);
                    ChipScores.Add(new ChipScore(p, basePoint));
                    //p.Save();
                }
            }
            public void ResizeParamRects(double resizeRate, Point bp)
            {
                foreach(var c in this.ChipScores)
                {
                    c.ChipArea_FromBasePoint = c.ChipArea_FromBasePoint.ResizeFromZero(resizeRate);
                    c.SetBasePoint(bp);
                    c.IgnoreAreas = c.IgnoreAreas.Select(i=>i.ResizeFromZero(resizeRate)).ToList();
                    foreach(var a in c.AreaInChips)
                    {
                        a.Area_InChip = a.Area_InChip.ResizeFromZero(resizeRate);
                        a.Area_Global = a.Area_InChip.Add(c.ChipArea_Global.Location);
                    }
                }
            }
            public (double waferScore, List<ChipScore> chipScores) Calculate(List<ResultClass> results)
            {
                //パラメータ
                double intersectAreaRate = 0.3;//面積のうち、これ以上重複していたら該当エリアに含める
                foreach (var r in results)
                {
                    var intersectChips = this.ChipScores.Where(c => r.Rect.IntersectsMore(c.ChipArea_Global.To2d(), intersectAreaRate));
                    if (intersectChips.Count() > 0)
                    {
                        var chip = intersectChips.First();
                        chip.AddResult(r);
                    }
                    else OutOfChipResults.Add(r);
                }
                //とりあえず、全チップのうち検出ありの割合がウエハスコア
                var waferScore = this.ChipScores.Where(c => c.Score > 0).Count() / this.ChipScores.Count;
                return (waferScore, this.ChipScores);
            }
            public void MakeChipResult(List<ChipScore> chipScores, Mat waferImg, string dirPath)
            {
                double resizeRate = 0.25;
                Size chipImgSizeBase = new Size(3660,11967);
                foreach (var c in chipScores)
                {
                    //var chipImgSize = c.Rotate == RotateAngle.None || c.Rotate == RotateAngle.Rotate180 ? chipImgSizeBase : new Size(chipImgSizeBase.Height, chipImgSizeBase.Width);
                    //var roiRect = MatFunctions.RectFromCenterPoint(c.ChipArea_Global.Center(), chipImgSize);
                    var roiRect = c.ChipArea_Global;
                    using var chipImg = MatFunctions.RoiToMat(waferImg, roiRect);
                    var resultsFromChipLocation = c.AreaInChips.SelectMany(a => a.Results
                                                                                                                  .Select(a2=>new ResultClass(a2.StripId, c.Name, a.Name, a2.DefectId,a2.Rect,a2.Score,a2.ImgPath,a2.ImgPath2)))
                                                                                     .ToList();
                    //範囲外は過検出が多いので除外する
                    //resultsFromChipLocation.AddRange(c.OutOfAreaResults.ToList());
                    resultsFromChipLocation.ForEach(r=>r.Rect=r.Rect.Subtract(roiRect.Location));
                    if (c.Rotate == RotateAngle.None) { }
                    else if (c.Rotate == RotateAngle.Rotate270)
                    {
                        var orgSize = new Size2d(chipImg.Size().Width, chipImg.Size().Height);
                        var rotatedOrgSize = new Size(orgSize.Height, orgSize.Width);
                        Cv2.Rotate(chipImg, chipImg, RotateFlags.Rotate90Counterclockwise);
                        resultsFromChipLocation.ForEach(r => r.Rect = MatFunctions.RotateRect(r.Rect, orgSize, RotateFlags.Rotate90Counterclockwise));
                        c.IgnoreAreas = c.IgnoreAreas.Select(i=> MatFunctions.RotateRect(i, orgSize.ToInt(), RotateFlags.Rotate90Counterclockwise)).ToList();
                        //回転なし画像に合わせてサイズ調整
                        Cv2.Resize(chipImg, chipImg, chipImgSizeBase);
                        (var rateX, var rateY) = ((double)chipImgSizeBase.Width / rotatedOrgSize.Width, (double)chipImgSizeBase.Height / rotatedOrgSize.Height);
                        resultsFromChipLocation.ForEach(r => r.Rect = r.Rect.ResizeFromZero(rateX, rateY));
                        c.IgnoreAreas = c.IgnoreAreas.Select(i => i.ResizeFromZero(rateX, rateY)).ToList();
                        //resultsFromChipLocation = resultsFromChipLocation.Select(r => r.Rect = MatFunctions.RotateRectFromPoint_Clockwise(r.Rect, chipImgCenter, Math.PI/2));
                    }
                    else throw new NotImplementedException();
                    var resultDir = Path.Combine(dirPath, c.Name);
                    Directory.CreateDirectory(resultDir);
                    for (int i = 0; i < resultsFromChipLocation.Count; i++) resultsFromChipLocation[i].DefectId = i;//DefectIdの振り直し
                    using var scoreImg = ResultClass.DrawResults(chipImg, resultsFromChipLocation, Scalar.Red);
                    scoreImg.SaveImage(Path.Combine(resultDir, c.Name + "_score.tif"));
                    var resultStrs = new List<string>() {ResultClass.GetHeader() };
                    resultStrs.AddRange(resultsFromChipLocation.Select(r => r.GetString()));
                    File.WriteAllLines(Path.Combine(resultDir, $"result_{c.Name}.csv"), resultStrs, Encoding.UTF8);
                }
            }
            public void MakeCsv_Total(List<ChipScore> chipScores, string filePath)
            {
                var csvContents = new List<string>() { this.GetCsvHeader_Total() };
                foreach (var c in chipScores)
                {
                    //各AreaInChipのスコアとAreaに含まれない検出数を出力 現状スコア＝検出数なのでごっちゃに出力　要検討
                    var allCount = c.AreaInChips.Sum(a => a.Results.Count);
                    allCount += c.OutOfAreaResults.Count;
                    csvContents.AddRange(c.AreaInChips.Select(a => $"{c.Name},{a.Name},{a.Area_Global.X},{a.Area_Global.Y},{a.Area_Global.Width},{a.Area_Global.Height},{a.GetScore()}"));
                    csvContents.Add($"{c.Name},Other,{c.ChipArea_Global.X},{c.ChipArea_Global.Y},{c.ChipArea_Global.Width},{c.ChipArea_Global.Height},{c.OutOfAreaResults.Count}");
                    csvContents.Add($"{c.Name},All,{c.ChipArea_Global.X},{c.ChipArea_Global.Y},{c.ChipArea_Global.Width},{c.ChipArea_Global.Height},{allCount}");
                }
                //チップ外の検出（ウエハ全面ではないので注意）
                csvContents.Add($"Other,Other,0,0,0,0,{this.OutOfChipResults.Count}");
                File.WriteAllLines(filePath, csvContents, Encoding.UTF8);
            }
            public void MakeCsv_Detail(List<ChipScore> chipScores, string filePath)
            {
                var csvContents = new List<string>() { this.GetCsvHeader_Detail() };
                var tmp = chipScores.SelectMany(c => c.AreaInChips.SelectMany(a =>
                {
                    var strs = new List<string>();
                    for (int i = 0; i < a.Results.Count; i++)
                    {
                        var result = a.Results[i];
                        var r = result.Rect;
                        strs.Add($"{c.Name},{a.Name},{r.X},{r.Y},{r.Width},{r.Height},{result.Score:f3},{i},{result.ImgPath}");
                    }
                    return strs;
                }))
                .ToList();

                var tmp_Out = chipScores.SelectMany(c =>
                {
                    var strs = new List<string>();
                    for (int i = 0; i < c.OutOfAreaResults.Count; i++)
                    {
                        var result = c.OutOfAreaResults[i];
                        var r = result.Rect;
                        strs.Add($"{c.Name},{"Other"},{r.X},{r.Y},{r.Width},{r.Height},{result.Score:f3},{i},{result.ImgPath}");
                    }
                    return strs;
                })
                .ToList();
                //チップ外の結果も入れる
                for (int i = 0; i < this.OutOfChipResults.Count; i++)
                {
                    var result = this.OutOfChipResults[i];
                    var r = this.OutOfChipResults[i].Rect;
                    tmp_Out.Add($"{"None"},{"Other"},{r.X},{r.Y},{r.Width},{r.Height},{result.Score:f3},{i},{result.ImgPath}");
                }
                csvContents.AddRange(tmp);
                File.WriteAllLines(FileManager.GetRenamedPath_Add(filePath, "_WithoutIgnore"), csvContents, Encoding.UTF8);
                csvContents.AddRange(tmp_Out);
                File.WriteAllLines(filePath, csvContents, Encoding.UTF8);
            }
            public string GetCsvHeader_Total()
            {
                return "ChipName, AreaName, X, Y, W, H, Score";
            }
            public string GetCsvHeader_Detail()
            {
                return "ChipName, AreaName, X, Y, W, H, Score, Id, ImgPath";
            }
            public int ChipAdressToId(string ChipAdress)
            {
                switch (ChipAdress)
                {
                    case "U1": return 1;
                    case "A1": return 2;
                    case "A2": return 3;
                    case "A3": return 4;
                    case "A4": return 5;
                    case "A5": return 6;
                    case "A6": return 7;
                    case "L2": return 8;
                    case "L1": return 9;
                    default: return 0;
                }
            }

        }
        /// <summary>
        /// シート内の各アドレスにあるチップのデータ
        /// </summary>
        public class ChipScore
        {
            public string Name = "";
            public RotateAngle Rotate = RotateAngle.None;
            public List<AreaInChip> AreaInChips= new List<AreaInChip>();
            public Rect ChipArea_Global;
            public Rect ChipArea_FromBasePoint;
            public List<ResultClass> OutOfAreaResults = new List<ResultClass>();
            public double Score=0.0;
            public List<Rect> IgnoreAreas = new List<Rect>();
            public ChipScore(ChipScoreParameter param, Point basePoint)
            {
                Name = param.Name;
                ChipArea_FromBasePoint = param.Area.ToCvRect();
                this.SetBasePoint(basePoint);
                AreaInChips = param.AreaInChips.Select(c => new AreaInChip(c, this.ChipArea_Global.Location)).ToList();
                Rotate = param.Rotate;
                IgnoreAreas = param.IgnoreAreas;
            }
            public void SetBasePoint(Point bp)
            {
                this.ChipArea_Global = this.ChipArea_FromBasePoint.Add(bp);
            }
            public void AddResult(ResultClass result)
            {
                double intersectsThresh = 0.3;
                var ignoreAreas_Global = this.IgnoreAreas.Select(i => i.Add(this.ChipArea_Global.Location)).ToList();
                var isIntersectsWithIgnore = ignoreAreas_Global.Any(i => result.Rect.IntersectsWith(i.To2d()));
                if (isIntersectsWithIgnore)
                {
                    this.OutOfAreaResults.Add(result);
                    return;
                }
                var intersects = this.AreaInChips.Where(a => result.Rect.IntersectsMore(a.Area_Global.To2d(), intersectsThresh));
                if (intersects.Count() > 0)
                {
                    //重複対象が複数ある時は最小面積のAreaInChipに追加
                    intersects.OrderBy(i=>i.Area_InChip.Area()).First().Add(result);
                }
                else
                {
                    this.OutOfAreaResults.Add(result);
                }
            }
            public double Calculate()
            {
                var score = this.AreaInChips.Sum(a => a.GetScore());
                return score;
            }
        }
        [DataContract]
        public class ChipScoreParameter
        {
            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public List<AreaInChipParameter> AreaInChips { get; set; }
            [DataMember]
            public System.Windows.Rect Area { get; set; }
            [DataMember]
            public RotateAngle Rotate { get; set; } = RotateAngle.None;
            [DataMember]
            public List<Rect> IgnoreAreas { get; set; }  = new List<Rect>();

            public string ReadPath = "";
            public ChipScoreParameter()
            {
                this.Name = "";
                this.AreaInChips= new List<AreaInChipParameter>();
                this.Area = new System.Windows.Rect();
            }
            public ChipScoreParameter(string xmlPath)
            {
                DataContractReaderWriter.ReadXml(this, xmlPath, new List<Type>() { new AreaInChipParameter().GetType()});
                this.ReadPath= xmlPath;
            }
            /// <summary>
            /// 保存　パス指定しない場合は読み込み時のパスに上書き
            /// </summary>
            /// <param name="xmlPath"></param>
            public void Save(string xmlPath="")
            {
                if (xmlPath == "") xmlPath = this.ReadPath;
                DataContractReaderWriter.WriteXml(this, xmlPath);
            }

        }
        /// <summary>
        /// チップ内の各領域（フィルタ部・アイランド部とか）を示すデータ
        /// </summary>
        public class AreaInChip
        {
            public string Name = "";
            public Rect Area_Global = Rect.Empty;
            //public Rect Area_FromBasePoint;
            public Rect Area_InChip = Rect.Empty;
            public List<ResultClass> Results = new List<ResultClass>();
            public AreaInChip()
            {
            }
            public AreaInChip(AreaInChipParameter param, Point basePoint)
            {
                this.Name= param.Name;
                this.Area_InChip = param.Area_InChip.ToCvRect();
                this.Area_Global = this.Area_InChip.Add(basePoint);
            }

            public void Add(ResultClass result)
            {
                this.Results.Add(result);
            }
            public double GetScore()
            {
                //とりあえずカウントするだけ
                return this.Results.Count;

            }
        }
        [DataContract]
        public class AreaInChipParameter
        {
            [DataMember]
            public string Name { get; set; }
            [DataMember]
            public System.Windows.Rect Area_InChip { get; set; }
            public AreaInChipParameter()
            {
                this.Name = "";
                this.Area_InChip = new System.Windows.Rect();
            }
        }

    }
}
