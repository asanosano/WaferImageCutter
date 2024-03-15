
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSoftware
{
    public class PositionDetectCommonParameter
    {
        public int MaskTh { get; set; } = 230;
        public int MeanJudgeTh { get; set; } = 50;
        public PositionDetectCommonParameter()
        {
        }
        public static PositionDetectCommonParameter Create(string filePath)
        {
            PositionDetectCommonParameter? obj = null;
            if (System.IO.File.Exists(filePath))
            {
                System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(PositionDetectCommonParameter));
                using (System.IO.StreamReader fs = new System.IO.StreamReader(filePath))
                {
                    obj = (PositionDetectCommonParameter)reader.Deserialize(fs);
                }
            }
            return obj;
        }
        public void Save(string filePath)
        {
            System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(PositionDetectCommonParameter));
            using (System.IO.FileStream fs = System.IO.File.Create(filePath))
            {
                writer.Serialize(fs, this);
            }
        }
    }
}
