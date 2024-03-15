using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LevCommon
{
    public class LevCoreParammeter
    {
        public NormalizeTypes NormalizeType = NormalizeTypes.Org;
        public BinarizeTypes BinarizeType = BinarizeTypes.Org;

        public int BinTh = 15;
        public double BinTh2 = 0.5;
        public int AreaTh = 12;
        public int WidthTh = 3;
        public int HeightTh = 3;
        public int MaskWidth = 28;

        public int DistanceTh = 30;
        public int DetectCountTh = 10;

        public int DetectSizeTh = 150;

        public JudgeTypes JudgeType = JudgeTypes.CenterPoint;
        public double JudgeTh = 18;
        public LevCoreParammeter()
        {
        }
        public static LevCoreParammeter Create(string filePath)
        {
            LevCoreParammeter? obj = null;
            if (System.IO.File.Exists(filePath))
            {
                System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(LevCoreParammeter));
                using (System.IO.StreamReader fs = new System.IO.StreamReader(filePath))
                {
                    obj = (LevCoreParammeter)reader.Deserialize(fs);
                }
            }
            return obj;
        }
        public void Save(string filePath)
        {
            System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(LevCoreParammeter));
            using (System.IO.FileStream fs = System.IO.File.Create(filePath))
            {
                writer.Serialize(fs, this);
            }
        }
    }

}
