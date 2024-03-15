using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LevCommon
{
    public class PascalVocData
    {

        Dictionary<VocTags, string> VocTagDict = new Dictionary<VocTags, string>();

        public enum VocTags { annotation, folder, filename, size, width, height, depth, Object, bndbox, xmin, ymin, xmax, ymax, name, difficult }

        public string Folder;
        public string FileName;
        public int Width;
        public int Height;
        public int Depth;
        public List<VocObject> ObjectList = new List<VocObject>();

        public PascalVocData()
        {
            foreach (VocTags tag in Enum.GetValues(typeof(VocTags)))
            {
                VocTagDict.Add(tag, tag.ToString().ToLower());
            }
        }
        //public PascalVocData(string filePath)
        //{
        //    foreach (VocTags tag in Enum.GetValues(typeof(VocTags)))
        //    {
        //        VocTagDict.Add(tag, tag.ToString().ToLower());
        //    }
        //    Load(filePath);
        //}
        public void Init(string imageFilePath, int w, int h)
        {
            this.Folder = System.IO.Path.GetDirectoryName(imageFilePath);
            this.FileName = System.IO.Path.GetFileName(imageFilePath); ;
            this.Width = w;
            this.Height = h;
            this.Depth = 3; //　要検討
            this.ObjectList = new List<VocObject>(); ;
        }

        public bool Load(string filePath)
        {
            //filePath = @"F:\20220927CycleGAN_青照明_Resize0.5\input\異物\20220908100129_TEST0908-025_20220908_A4_C04_M1_N1_S1_7283_1549_320_320_C2_TEST.xml";

            List<VocObject> objectList = new List<VocObject>();
            if (System.IO.File.Exists(filePath) == false)
            {
                return false;
            }
            XElement xml = XElement.Load(filePath);
            var folder = xml.Element(VocTagDict[VocTags.folder]).Value;
            var fileName = xml.Element(VocTagDict[VocTags.filename]).Value;
            var size = xml.Element(VocTagDict[VocTags.size]);
            var width = Convert.ToInt32(size.Element(VocTagDict[VocTags.width]).Value);
            var height = Convert.ToInt32(size.Element(VocTagDict[VocTags.height]).Value);
            var depth = Convert.ToInt32(size.Element(VocTagDict[VocTags.depth]).Value);
            var objs = xml.Elements(VocTagDict[VocTags.Object]);
            foreach (var obj in objs)
            {
                if (obj.IsEmpty)
                {
                    Debug.WriteLine($"'object' is empty:{filePath}");
                    return false;
                }
                string name = obj.Element(VocTagDict[VocTags.name]).Value;
                bool difficult = obj.Element(VocTagDict[VocTags.difficult]).Value == "0" ? false : true;
                var bndbox = obj.Element(VocTagDict[VocTags.bndbox]);
                var xmin = Convert.ToInt32(bndbox.Element(VocTagDict[VocTags.xmin]).Value);
                var ymin = Convert.ToInt32(bndbox.Element(VocTagDict[VocTags.ymin]).Value);
                var xmax = Convert.ToInt32(bndbox.Element(VocTagDict[VocTags.xmax]).Value);
                var ymax = Convert.ToInt32(bndbox.Element(VocTagDict[VocTags.ymax]).Value);

                var vocObject = new VocObject(name, new VocBox(xmin, ymin, xmax, ymax), difficult);

                objectList.Add(vocObject);
            }
            this.Folder = folder;
            this.FileName = fileName;
            this.Width = width;
            this.Height = height;
            this.Depth = depth;
            this.ObjectList = objectList;

            return true;
        }

        public bool Save(string filePath)
        {
            object[] vocobjs = new object[ObjectList.Count];
            for (int i = 0; i < ObjectList.Count; i++)
            {
                vocobjs[i] = new XElement(
                    VocTagDict[VocTags.Object],
                        new XElement(VocTagDict[VocTags.bndbox],
                           new XElement(VocTagDict[VocTags.xmin], ObjectList[i].Box.XMin),
                           new XElement(VocTagDict[VocTags.ymin], ObjectList[i].Box.YMin),
                           new XElement(VocTagDict[VocTags.xmax], ObjectList[i].Box.XMax),
                           new XElement(VocTagDict[VocTags.ymax], ObjectList[i].Box.YMax)
                        ),
                        new XElement(VocTagDict[VocTags.name], ObjectList[i].Name),
                        new XElement(VocTagDict[VocTags.difficult], ObjectList[i].Difficult ? 1 : 0)
                        );
            }

            var xml = new XElement(VocTagDict[VocTags.annotation],
                                                    new XElement(VocTagDict[VocTags.folder], this.Folder),
                                                    new XElement(VocTagDict[VocTags.filename], this.FileName),

                                                    new XElement(VocTagDict[VocTags.size],
                                                        new XElement(VocTagDict[VocTags.width], this.Width),
                                                        new XElement(VocTagDict[VocTags.height], this.Height),
                                                        new XElement(VocTagDict[VocTags.depth], this.Depth)),
                                                    vocobjs
                                                   );
            xml.Save(filePath);

            return true;
        }
    }
    public class VocObject
    {
        public string Name;
        public VocBox Box;
        public bool Difficult = false;

        public VocObject(string name, VocBox box, bool difficult = false)
        {
            Name = name;
            Box = box;
            Difficult = difficult;
        }
    }
    public class VocBox
    {
        public int XMin;
        public int YMin;
        public int XMax;
        public int YMax;

        public VocBox(int xmin, int ymin, int xmax, int ymax)
        {
            XMin = xmin;
            YMin = ymin;
            XMax = xmax;
            YMax = ymax;
        }
    }
}