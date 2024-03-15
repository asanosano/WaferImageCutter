using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Wafer.ChipResult
{
    public class WorkInfo
    {
        public string DateStr;
        public string LotNo;
        public string TrayID;
        public string Address;
        public string Category;
        public string MachineID;
        public string ImageNumber;
        public string Status;
        public string RetryCount;

        public string NameWithoutExtension;
        public string Name;
        public string FilePath;
        public string CsvContents;

        //static public string CaptureEndExt = ".cpe";
        static public string JudgedPtn = "*_N0_S?.csv";
        //static public string InspectionEndExt = ".ise";

        public override string ToString()
        {
            return NameWithoutExtension;
        }

        public WorkInfo()
        {
        }

        public WorkInfo(string filePath)
        {
            FilePath = filePath;
            NameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string[] pp = NameWithoutExtension.Split('_');
            if (Path.GetExtension(filePath) == ".csv") CsvContents = File.ReadAllText(FilePath, Encoding.UTF8);

            int idx = 0;
            DateStr = pp[idx++];
            LotNo = pp[idx++];
            TrayID = pp[idx++];
            Address = pp[idx++];
            Category = pp[idx++];
            MachineID = pp[idx++];
            ImageNumber = pp[idx++];
            Status = pp[idx++];
            if (pp.Length > 8) RetryCount = pp[idx++];
            else RetryCount = "0";
        }

        public string GetWorkID()
        {
            return $"{DateStr}_{LotNo}_{TrayID}_{Address}_{Category}_{MachineID}";
        }
        public WorkInfo CloneWithDivisionInfo(Divisions div)
        {
            var wi = new WorkInfo();
            wi.Address = Address;
            wi.Category = Category;
            wi.DateStr = DateStr;
            wi.FilePath = FilePath;
            wi.ImageNumber = $"N{(int)div}";
            wi.LotNo = LotNo;
            wi.MachineID = MachineID;
            wi.Name = Name;
            wi.NameWithoutExtension = NameWithoutExtension;
            wi.Status = Status;
            wi.TrayID = TrayID;
            return wi;
        }
        public string GetNgImageName(AutoInspectResult aiResult)
        {
            var sr = aiResult.ScoreRect;
            var ir = aiResult.ImgRect;
            return $"{DateStr}_{LotNo}_{TrayID}_{Address}_{Category}_{MachineID}_{ir.X}_{ir.Y}_{ir.Width}_{ir.Height}_S2_1_{sr.X}_{sr.Y}_{sr.Width}_{sr.Height}_{aiResult.Score}_{aiResult.Method}";
        }
        //static
        static int CompareCreationTime(string fileX, string fileY)
        {
            return DateTime.Compare(File.GetCreationTime(fileX), File.GetCreationTime(fileY));
        }
        static public List<WorkInfo> GetWorkFileList(string dirPath)
        {
            List<WorkInfo> wList = new List<WorkInfo>();

            string[] files = Directory.GetFiles(dirPath, JudgedPtn);
            Array.Sort(files, CompareCreationTime);
            foreach (var file in files)
            {
                WorkInfo w = new WorkInfo(file);
                wList.Add(w);
            }
            return wList;
        }

        static public List<WorkInfo> GetJudgedFileList(string dirPath)
        {
            List<WorkInfo> wList = new List<WorkInfo>();

            string[] files = Directory.GetFiles(dirPath, JudgedPtn);
            Array.Sort(files, CompareCreationTime);
            foreach (var file in files)
            {
                WorkInfo w = new WorkInfo(file);
                wList.Add(w);
            }
            return wList;
        }
        static public string[] GetImageFiles(string dir, WorkInfo w, bool withCSV)
        {
            string p1 = string.Format("{0}_{1}_{2}_{3}_{4}_{5}_?_?.tif", w.DateStr, w.LotNo, w.TrayID, w.Address, w.Category, w.MachineID);
            string[] files1 = Directory.GetFiles(dir, p1);

            string p2 = string.Format("{0}_{1}_{2}_{3}_{4}_{5}_?_?_judged.csv", w.DateStr, w.LotNo, w.TrayID, w.Address, w.Category, w.MachineID);
            string[] files2 = Directory.GetFiles(dir, p2);

            string[] files;
            if (withCSV)
            {
                files = files1.Concat(files2).ToArray();
            }
            else
            {
                files = files1;
            }
            return files;
        }
        static public string N1ToFrontRight(string Nx)
        {
            switch (Nx)
            {
                case "N1": return "FrontRight";
                case "N2": return "FrontLeft";
                case "N3": return "BackRight";
                case "N4": return "BackLeft";
                default: return "";
            }
        }
    }
    public static class WorkInfoExtentions
    {

        static public bool IsSameDivision(this string Nx1, string Nx2)
        {
            return Nx1.ToDivision() == Nx2.ToDivision();
        }
        static public bool IsSameDivision(this string Nx1, int Nx2)
        {
            return Nx1.ToDivision() == Nx2.ToDivision();
        }
        static public bool IsSameDivision(this Divisions Nx1, string Nx2)
        {
            return Nx1.ToDivision() == Nx2.ToDivision();
        }
        static public bool IsSameDivision(this string Nx1, Divisions Nx2)
        {
            return Nx1.ToDivision() == Nx2.ToDivision();
        }
        static private int ToDivision(this object Nx)
        {
            switch (Nx)
            {
                case "N1":
                case "FrontRight":
                case "1":
                case 1:
                case Divisions.FrontRight:
                    return 1;
                case "N2":
                case "FrontLeft":
                case "2":
                case 2:
                case Divisions.FrontLeft:
                    return 2;
                case "N3":
                case "BackRight":
                case "3":
                case 3:
                case Divisions.BackRight:
                    return 3;
                case "N4":
                case "BackLeft":
                case "4":
                case 4:
                case Divisions.BackLeft:
                    return 4;
                default: return 0;
            }
        }
    }
}
