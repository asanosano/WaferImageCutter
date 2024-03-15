using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LevCommon
{
    public class LevResult
    {
        public ExecuteStatusTypes ExecuteStatusType = ExecuteStatusTypes.None;
        public DetectObjectStatusTypes DetectStatus = DetectObjectStatusTypes.None;

        public List<DetectObjectData> DetectDataList = new List<DetectObjectData>();
        public List<DetectGroupData> DetectGroupDataList = new List<DetectGroupData>();
        public List<AnnotationObjectData> AnnotationDataList = new List<AnnotationObjectData>();

        public String InputImageFilePath;
        public String GeneratedImageFilePath;
        public String AnnotationFilePath = "";

        public Mat InputImageMat;
        public Mat GeneratedImageMat;
        public Mat DiffImageMat;
        public Mat DetectMat;
        public Mat DetetectColorMat;

        public String DiffImageFilePath = "";
        public String DetectImageFilePath = "";


        public string GetResult()
        {
            string str = "";
            string fname = System.IO.Path.GetFileName(InputImageFilePath);

            string nx = "XX";
            if (fname.Contains("_N1_"))
            {
                nx = "N1";
            }
            else if (fname.Contains("_N2_"))
            {
                nx = "N2";
            }
            else if (fname.Contains("_N3_"))
            {
                nx = "N3";
            }
            else if (fname.Contains("_N4_"))
            {
                nx = "N4";
            }
            if (ExecuteStatusType == ExecuteStatusTypes.Executed)
            {
                str += $"{DetectStatus}\t";
                str += $"{nx}\t";
                str += $"{InputImageFilePath}\t";
                str += $"{DiffImageFilePath}\t";
                str += $"{DetectImageFilePath}\t";

                foreach (var detectGroupData in DetectGroupDataList)
                {
                    str += $"{detectGroupData.Distannce:F1}\t";
                }
            }
            else
            {
                str += $"{ExecuteStatusType}";
            }
            str += "\n";
            return str;
        }

    }


    public class ObjectData
    {
        public double SX;
        public double SY;
        public double WX;
        public double WY;

        public System.Windows.Rect Rect
        {
            get { return new System.Windows.Rect(SX, SY, WX, WY); }
        }
        public System.Windows.Point CenterPoint
        {
            get { return new System.Windows.Point(SX + WX / 2, SY + WY / 2); }
        }
        public System.Windows.Rect CenterRect
        {
            get { return new System.Windows.Rect(SX + WX / 2, SY + WY / 2, 0.1, 0.1); }
        }
        public ObjectData(double sx, double sy, double wx, double wy)
        {
            SX = sx;
            SY = sy;
            WX = wx;
            WY = wy;
        }
    }

    public class AnnotationObjectData : ObjectData
    {
        public List<DetectGroupData> ContainDetectGroupList = new List<DetectGroupData>();
        public List<double> JudgeData = new List<double>();

        public bool Difficult = false;
        public AnnotationObjectData(double sx, double sy, double wx, double wy, bool difficult) : base(sx, sy, wx, wy)
        {
            Difficult = difficult;
        }
    }

    public class DetectObjectData : ObjectData
    {
        public List<DetectObjectData> RelatedObjectlist = new List<DetectObjectData>();

        // public List<AnnotationObjectData> ContainObjectList = new List<AnnotationObjectData>();
        public List<double> JudgeData = new List<double>();
        public DetectObjectData(double sx, double sy, double wx, double wy) : base(sx, sy, wx, wy)
        {
        }
    }
    public class DetectGroupData
    {
        public DetectObjectStatusTypes DetectObjectStatus = DetectObjectStatusTypes.None;
        public List<DetectObjectData> RelatedObjectlist = new List<DetectObjectData>();

        public double SX;
        public double SY;
        public double WX;
        public double WY;

        public AnnotationObjectData AnnotationObject = null;
        public double Distannce = double.MaxValue;
        public System.Windows.Rect Rect
        {
            get { return new System.Windows.Rect(SX, SY, WX, WY); }
        }
        public System.Windows.Point CenterPoint
        {
            get { return new System.Windows.Point(SX + WX / 2, SY + WY / 2); }
        }
        public System.Windows.Rect CenterRect
        {
            get { return new System.Windows.Rect(SX + WX / 2, SY + WY / 2, 0.1, 0.1); }
        }

        public void Update()
        {
            SX = double.MaxValue;
            SY = double.MaxValue;
            double X2 = 0;
            double Y2 = 0;
            WX = 0;
            WY = 0;

            foreach (var detectObjectData in RelatedObjectlist)
            {
                SX = Math.Min(SX, detectObjectData.SX);
                SY = Math.Min(SY, detectObjectData.SY);
                X2 = Math.Max(X2, detectObjectData.SX + detectObjectData.WX);
                Y2 = Math.Max(Y2, detectObjectData.SY + detectObjectData.WY);
            }
            WX = X2 - SX;
            WY = Y2 - SY;
        }

    }

}
