using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace Wafer.ChipResult
{
    public class AutoInspectResult
    {
        public string Date;
        public string LotNo;
        public string TrayID;
        public string Address;
        public string Category;
        public string MachineID;
        public string ImageNumber; //N1,N2,N3,N4
        public string AreaID;
        public string Standard;
        public int PartialID;//切り出し番号
        public Rect ImgRect;
        public Rect ScoreRect;
        public int Method; //処理の種類
        public int DefectID;
        public string Comment;
        public DefectGrade DefectGrade;
        public List<double> Score = new List<double>();
        public bool IsGetSecondImage = true;

        public Rect ScoreRect_GlobalPos => new Rect(ScoreRect.X + ImgRect.X, ScoreRect.Y + ImgRect.Y, ScoreRect.Width, ScoreRect.Height);
        public string WorkID => $"{Date}_{LotNo}_{TrayID}_{Address}_{Category}_{MachineID}";


        public AutoInspectResult() { }
        /// <summary>
        /// 通常用（スコア複数）
        /// </summary>
        public AutoInspectResult(PartialImageList pi, int partialID, List<double> score, Rect scoreRect, int method = 0, int defectId = 0, DefectGrade grade = DefectGrade.None, string comment = "", bool is2nd = true)
        {
            Initialize(pi.WI.DateStr, pi.WI.LotNo, pi.WI.TrayID, pi.WI.Address, pi.WI.Category, pi.WI.MachineID, pi.WI.ImageNumber, pi.AreaID, pi.Standard, partialID, pi.Rects[partialID], scoreRect, score, grade, method, defectId, comment, is2nd);
        }
        /// <summary>
        /// 通常用（スコア1個）
        /// </summary>
        public AutoInspectResult(PartialImageList pi, int partialID, double score, Rect scoreRect, int method = 0, int defectId = 0, DefectGrade grade = DefectGrade.None, string comment = "", bool is2nd = true)
        {
            var tmp = new List<double>();
            tmp.Add(score);
            Initialize(pi.WI.DateStr, pi.WI.LotNo, pi.WI.TrayID, pi.WI.Address, pi.WI.Category, pi.WI.MachineID, pi.WI.ImageNumber, pi.AreaID, pi.Standard, partialID, pi.Rects[partialID], scoreRect, tmp, grade, method, defectId, comment, is2nd);
        }

        /// <summary>
        /// SeamlessInspection用コンストラクタ
        /// </summary>
        public AutoInspectResult(PartialImageList pi, int partialID, Rect cutRect, double score, Rect scoreRect, int method = 0, int defectId = 0, DefectGrade grade = DefectGrade.None, string comment = "", bool is2nd = true)
        {
            var tmp = new List<double>();
            tmp.Add(score);
            Initialize(pi.WI.DateStr, pi.WI.LotNo, pi.WI.TrayID, pi.WI.Address, pi.WI.Category, pi.WI.MachineID, pi.WI.ImageNumber, pi.AreaID, pi.Standard, partialID, cutRect, scoreRect, tmp, grade, method, defectId, comment, is2nd);
        }
        /// <summary>
        /// 直接全部セットするコンストラクタ
        /// </summary>
        public AutoInspectResult(string date, string lot, string trayID, string trayAddress, string category, string machineID, string N1234, string areaID, string standard, int PartialID, Rect imgRect, Rect scoreRect, List<double> score, int method = 0, int defectId = 0, DefectGrade grade = DefectGrade.None, string comment = "", bool is2nd = true)
        {
            Initialize(date, lot, trayID, trayAddress, category, machineID, N1234, areaID, standard, PartialID, imgRect, scoreRect, score, grade, method, defectId, comment, is2nd);
        }

        /// <summary>
        /// csv列から読み込むコンストラクタ
        /// </summary>
        public AutoInspectResult(string csvStr)
        {
            var s = csvStr.Split(',').ToList();
            var nItem = 25;
            var iter = nItem - s.Count;
            for (int i = 0; i < iter; i++) s.Add("");
            var cutRect = new Rect(int.Parse(s[10]), int.Parse(s[11]), int.Parse(s[12]), int.Parse(s[13]));
            var scoreRect = new Rect(int.Parse(s[14]), int.Parse(s[15]), int.Parse(s[16]), int.Parse(s[17]));
            if (s[20] == "") s[20] = "0.0";
            if (s[21] == "") s[21] = "0.0";
            if (s[18] == "") s[18] = "0";
            if (s[19] == "") s[19] = "0";
            var scores = new List<double>() { double.Parse(s[20]), double.Parse(s[21]) };
            var isParsed = Enum.TryParse<DefectGrade>(s[22], out var grade);
            if (!isParsed) grade = DefectGrade.None;
            isParsed = bool.TryParse(s[24], out var is2nd);
            if (!isParsed) is2nd = true;
            Initialize(s[0], s[1], s[2], s[3], s[4], s[5], s[6], s[7], s[8], int.Parse(s[9]), cutRect, scoreRect, scores, grade, int.Parse(s[18]), int.Parse(s[19]), s[23], is2nd);
        }

        private void Initialize(string date, string lot, string trayID, string trayAddress, string category, string machineID, string N1234, string areaID, string standard, int PartialID, Rect imgRect, Rect scoreRect, List<double> score, DefectGrade grade, int method, int defectId, string comment, bool is2nd)
        {
            Date = date;
            LotNo = lot;
            TrayID = trayID;
            Address = trayAddress;
            Category = category;
            MachineID = machineID;
            ImageNumber = N1234;
            AreaID = areaID;
            Standard = standard;
            this.PartialID = PartialID;
            ImgRect = imgRect;
            ScoreRect = scoreRect;
            Method = method;
            DefectID = defectId;
            Comment = comment;
            Score = score;
            DefectGrade = grade;
            IsGetSecondImage = is2nd;
        }
        public new string ToString()
        {
            string scores = "";
            if (Score.Count == 1) scores = $"{Score[0]:f3},";
            else if (Score.Count == 2)
            {
                scores = $"{Score[0]:f3}, {Score[1]:f3}";
            }
            else
            {
                scores = $"{Score[0]:f3}, {Score[1]:f3}";
                for (int i = 2; i < Score.Count - 2; i++)
                {
                    scores += $"_{Score[i]:f3}";
                }
            }
            return $"{Date},{LotNo},{TrayID},{Address},{Category},{MachineID},{ImageNumber},{AreaID},{Standard},{PartialID},{ImgRect.X},{ImgRect.Y},{ImgRect.Width},{ImgRect.Height},{ScoreRect.X},{ScoreRect.Y},{ScoreRect.Width},{ScoreRect.Height},{Method},{DefectID},{scores},Gray,{Comment},{IsGetSecondImage}";
        }
        public string GetNgImageName(string retryCount, int id)
        {
            var sr = ScoreRect_GlobalPos;
            var ir = ImgRect;
            return $"{Date}_{LotNo}_{TrayID}_{Address}_{Category}_{MachineID}_{ImageNumber}_S2_{retryCount}_i{id:d5}_X{sr.X}_Y{sr.Y}_W{sr.Width}_H{sr.Height}_{Score[0]:f2}_{DefectID}";
            ///*スコア領域込み*/return $"{this.Date}_{this.LotNo}_{this.TrayID}_{this.Address}_{this.Category}_{this.MachineID}_{this.ImageNumber}_CX{ir.X}_CY{ir.Y}_CW{ir.Width}_CH{ir.Height}_S2_{retryCount}_SX{sr.X}_SY{sr.Y}_SW{sr.Width}_SH{sr.Height}_{this.Score[0]:f2}_{this.Method}";
        }
        public string GetNgImageNameScoreFirst(string retryCount)
        {
            var sr = ScoreRect;
            var ir = ImgRect;
            return $"{Score[0]:f3}_{Date}_{LotNo}_{TrayID}_{Address}_{Category}_{MachineID}_{ImageNumber}_S2_{retryCount}_{ir.X}_{ir.Y}_{ir.Width}_{ir.Height}_{DefectID}";
            //return $"{this.Date}_{this.LotNo}_{this.TrayID}_{this.Address}_{this.Category}_{this.MachineID}_{this.ImageNumber}_S2_{retryCount}_X{ir.X}_Y{ir.Y}_W{ir.Width}_H{ir.Height}_{this.Score[0]:f2}_{this.DefectID}";
        }
        public static string GetOutputCsvHeader()
        {
            //旧項目
            //return $"Date,lotNo,TrayID,Adress,Kind,Machine,ImageNo,InspArea,Standard,FileName,CutSX,CutSY,CutWX,CutWY,ScoreSX,ScoreSY,ScoreWX,ScoreWY,Method,Score,Judge{Environment.NewLine}";
            //           /*   0       1        2         3       4         5         6            7              8            9         10       11      12       13       14          15         16        17         18      19      20   */
            //旧項目(20210315)
            //return $"Date,lotNo,TrayID,Adress,Kind,Machine,ImageNo,InspArea,Standard,CutNo,CutSX,CutSY,CutWX,CutWY,ScoreSX,ScoreSY,ScoreWX,ScoreWY,Method,DefectID,Comment,Score1,Score2,Judge";
            return $"DateTime,LotNo,TrayID,A,C,M,N,Area,Std,CNo,CSX,CSY,CWX,CWY,SSX,SSY,SWX,SWY,Method,DefectID,DScore1,DScore2,DJudge,DCmt,Is2ndImg";
            /*               0              1         2      3 4  5  6    7     8      9     10    11   12     13    14   15   16     17      18          19            20           21          22        23       24     */
        }
        public AutoInspectResult Clone()
        {
            AutoInspectResult newObj = new AutoInspectResult();
            newObj.Date = Date;
            newObj.LotNo = LotNo;
            newObj.TrayID = TrayID;
            newObj.Address = Address;
            newObj.Category = Category;
            newObj.MachineID = MachineID;
            newObj.ImageNumber = ImageNumber;
            newObj.AreaID = AreaID;
            newObj.Standard = Standard;
            newObj.PartialID = PartialID;
            newObj.ImgRect = new Rect(ImgRect.X, ImgRect.Y, ImgRect.Width, ImgRect.Height);
            newObj.ScoreRect = new Rect(ScoreRect.X, ScoreRect.Y, ScoreRect.Width, ScoreRect.Height);
            newObj.Method = Method;
            newObj.DefectID = DefectID;
            newObj.Comment = Comment;
            newObj.Score = new List<double>();
            for (int i = 0; i < Score.Count; i++)
            {
                newObj.Score.Add(Score[i]);
            }
            newObj.DefectGrade = DefectGrade;
            newObj.IsGetSecondImage = IsGetSecondImage;
            return newObj;
        }
        #region スクリーニング装置用
        public static string GetOutputCsvHeaderForScreening()
        {
            return $"NGラベル番号,NG名称,画像位置,開始座標X,開始座標Y,幅,高さ,スコア,NG切り出し画像の対象";
        }
        #endregion
    }
}
