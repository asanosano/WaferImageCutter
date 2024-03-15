using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using OpenCvSharp;

namespace LevCommon
{
    [Flags]
    public enum DetectObjectStatusTypes { None = 0x00, TooManyDetectPoints = 0x01, TooLargeObject = 0x02, OK = 0x10, Miss1 = 0x20, Whichever = 0x40, OverDetection = 0x80 }

    [Flags]
    public enum ExecuteStatusTypes { None = 0x00, NoGeneratedImage = 0x01, NoAnnotation = 0x02, DetectionError = 0x04, OtherError = 0x10, Executed = 0x20 }


    public enum JudgeTypes { CenterPoint }

    public delegate void ShowImageFunc(Mat mat, string titele);

    public class LevCore
    {
        LevNormalize normalize = new LevNormalize();
        LevBinarize levBinarize = new LevBinarize();
        ShowImageFunc addImageFunc;
        public ShowImageFunc AddImageFunc
        {
            set
            {
                addImageFunc = value;
                levBinarize.AddImageFunc = value;
            }
        }
        void AddImage(Mat mat, string title)
        {
            if (addImageFunc == null)
            {
                return;
            }
            addImageFunc(mat, title);
        }
        public LevCore()
        {

        }
        public List<OpenCvSharp.Rect> GetDetectedRectList(LevCoreParammeter levCoreParammeter, Mat inputImageMat, Mat generatedImageMat)
        {
            LevResult levResult = new LevResult();
            bool status = Detect(levCoreParammeter, inputImageMat, generatedImageMat, levResult);
            if (status == false)
            {
                return null;  //　端部
            }

            if (levResult.DetectGroupDataList.Count == 0)
            {
                return GetDummyRect(inputImageMat.Rows, inputImageMat.Cols, 1, 1);  //要検討　エラー、未検出の場合ダミー出力
            }
            List<OpenCvSharp.Rect> rectList = new List<OpenCvSharp.Rect>();
            foreach (var t in levResult.DetectGroupDataList)
            {
                OpenCvSharp.Rect rect = new OpenCvSharp.Rect((int)t.SX, (int)t.SY, (int)t.WX, (int)t.WY);
                rectList.Add(rect);
            }
            return rectList;
        }
        public List<OpenCvSharp.Rect> GetDummyRect(int xsize, int ysize, int xcnt, int ycnt)
        {
            List<OpenCvSharp.Rect> rectList = new List<OpenCvSharp.Rect>();
            int wx = xsize / xcnt;
            int wy = ysize / ycnt;
            for (int y = 0; y < ycnt; y++)
            {
                for (int x = 0; x < xcnt; x++)
                {
                    rectList.Add(new OpenCvSharp.Rect(x * wx, y * wy, wx, wy));
                }
            }
            return rectList;
        }
        public List<DetectGroupData> GetDummyDetectGroupList(int xsize, int ysize, int xcnt, int ycnt)//テスト用
        {
            List<DetectGroupData> rectList = new List<DetectGroupData>();
            int wx = xsize / xcnt;
            int wy = ysize / ycnt;
            for (int y = 0; y < ycnt; y++)
            {
                for (int x = 0; x < xcnt; x++)
                {
                    DetectObjectData detectObjectData = new DetectObjectData(x * wx, y * wy, wx, wy);
                    DetectGroupData detectGroupData = new DetectGroupData();
                    detectGroupData.RelatedObjectlist.Add(detectObjectData);
                    detectGroupData.Update();
                    rectList.Add(detectGroupData);
                }
            }
            return rectList;
        }

        bool CheckEdgeAre(Mat inputImageMat)
        {
            int size = 10;
            Mat[] tmpMat = new Mat[4];
            tmpMat[0] = inputImageMat.Clone(new OpenCvSharp.Rect(0, 0, size, size));
            tmpMat[1] = inputImageMat.Clone(new OpenCvSharp.Rect(0, inputImageMat.Height - size - 1, size, size));
            tmpMat[2] = inputImageMat.Clone(new OpenCvSharp.Rect(inputImageMat.Width - size - 1, 0, size, size));
            tmpMat[3] = inputImageMat.Clone(new OpenCvSharp.Rect(inputImageMat.Width - size - 1, inputImageMat.Height - size - 1, size, size));

            foreach (Mat mat in tmpMat)
            {
                var mean = mat.Mean()[0];
                if (mean > 250)
                {
                    return true;
                }
            }
            return false;
        }
        bool GetDetectedAreaList(LevCoreParammeter levCoreParammeter, Mat inputImageMat, Mat generatedImageMat, LevResult levResult)
        {
            levResult.DetectDataList.Clear();
            if (CheckEdgeAre(inputImageMat) == true)
            {
                return false;
            }
            normalize.Execute(levCoreParammeter.NormalizeType, ref inputImageMat, ref generatedImageMat);

            Mat detectMat = new Mat();
            Cv2.Absdiff(inputImageMat, generatedImageMat, detectMat);
            levResult.DiffImageMat = detectMat;

            Mat binary = levBinarize.Execute(levCoreParammeter, detectMat);

            //AddImage(binary, "Bin");

            var erodeMat = new Mat(binary.Rows, binary.Cols, MatType.CV_8UC1);
            var dilateMat = new Mat(binary.Rows, binary.Cols, MatType.CV_8UC1);

            Cv2.Dilate(binary, dilateMat, null, iterations: 2);
            binary = dilateMat;
            //AddImage(binary, "Dilate");

            Mat element = new Mat(3, 3, MatType.CV_8UC1);
            element.Set<byte>(0, 0, 0); element.Set<byte>(0, 1, 1); element.Set<byte>(0, 2, 0);
            element.Set<byte>(1, 0, 1); element.Set<byte>(1, 1, 1); element.Set<byte>(1, 2, 1);
            element.Set<byte>(2, 0, 0); element.Set<byte>(2, 1, 1); element.Set<byte>(2, 2, 0);

            Cv2.Erode(binary, erodeMat, element, iterations: 3);
            binary = erodeMat;
            //AddImage(binary, "Erode");

            Cv2.Dilate(binary, dilateMat, null, iterations: 1);
            binary = dilateMat;
            //AddImage(binary, "Dilate");


            binary = Mask(levCoreParammeter.MaskWidth, binary, inputImageMat);

            levResult.DetectMat = binary;

            ConnectedComponents cc = Cv2.ConnectedComponentsEx(binary);
            int cnt = 0;
            int areaTh = levCoreParammeter.AreaTh;
            int widthTh = levCoreParammeter.WidthTh;
            int heightTh = levCoreParammeter.HeightTh;

            int top = levCoreParammeter.MaskWidth;
            int left = levCoreParammeter.MaskWidth;
            int right = binary.Width - levCoreParammeter.MaskWidth - 1;
            int bottom = binary.Height - levCoreParammeter.MaskWidth - 1;


            foreach (var blob in cc.Blobs.Skip(1))
            {
                OpenCvSharp.Rect cornerRect = OpenCvSharp.Rect.Empty;
                int cornerHalfWidth = 20;
                var currentRect = blob.Rect;
                if (currentRect.Top == top && currentRect.Left == left)
                {
                    cornerRect = new OpenCvSharp.Rect(left - cornerHalfWidth, top - cornerHalfWidth, cornerHalfWidth * 2, cornerHalfWidth * 2);
                }
                else if (currentRect.Top == top && currentRect.Right == right)
                {
                    cornerRect = new OpenCvSharp.Rect(right - cornerHalfWidth, top - cornerHalfWidth, cornerHalfWidth * 2, cornerHalfWidth * 2);
                }
                else if (currentRect.Bottom == bottom && currentRect.Left == left)
                {
                    cornerRect = new OpenCvSharp.Rect(left - cornerHalfWidth, bottom - cornerHalfWidth, cornerHalfWidth * 2, cornerHalfWidth * 2);
                }
                else if (currentRect.Bottom == bottom && currentRect.Right == right)
                {
                    cornerRect = new OpenCvSharp.Rect(right - cornerHalfWidth, bottom - cornerHalfWidth, cornerHalfWidth * 2, cornerHalfWidth * 2);
                }
                if (cornerRect != OpenCvSharp.Rect.Empty)
                {
                    var cornerMat = inputImageMat.Clone(cornerRect);
                    var mean = cornerMat.Mean()[0];
                    if (mean > 250)
                    {
                        continue;
                    }
                }

                if (blob.Area < areaTh)
                {
                    continue;
                }
                if (blob.Width < widthTh)
                {
                    continue;
                }
                if (blob.Height < heightTh)
                {
                    continue;
                }
                levResult.DetectDataList.Add(new DetectObjectData(blob.Left, blob.Top, blob.Width, blob.Height));

                cnt++;
            }
            return true;
        }
        DetectGroupData DetectGroupContains(DetectObjectData obj, List<DetectGroupData> detectGroupDataList)
        {
            foreach (var detectGroup in detectGroupDataList)
            {
                if (detectGroup.RelatedObjectlist.Contains(obj))
                {
                    return detectGroup;
                }
            }
            return null;
        }
        void addDetectObject(DetectObjectData obj, List<DetectGroupData> detectGroupDataList, DetectGroupData currentGroupData)
        {
            if (DetectGroupContains(obj, detectGroupDataList) != null)
            {
                return;
            }
            DetectGroupData detectGroupData;
            if (currentGroupData == null)
            {
                detectGroupData = new DetectGroupData();
                detectGroupDataList.Add(detectGroupData);
            }
            else
            {
                detectGroupData = currentGroupData;
            }
            detectGroupData.RelatedObjectlist.Add(obj);

            foreach (var rObj in obj.RelatedObjectlist)
            {
                addDetectObject(rObj, detectGroupDataList, detectGroupData);
            }
        }

        bool Detect(LevCoreParammeter levCoreParammeter, Mat inputImageMat, Mat generatedImageMat, LevResult levResult)
        {
            levResult.DetectGroupDataList.Clear();

            bool status = GetDetectedAreaList(levCoreParammeter, inputImageMat, generatedImageMat, levResult);
            if (status == false)
            {
                levResult.ExecuteStatusType = ExecuteStatusTypes.DetectionError;
                return false;
            }

            // 近接した検出領域の関係付ける
            foreach (DetectObjectData obj1 in levResult.DetectDataList)
            {
                foreach (DetectObjectData obj2 in levResult.DetectDataList)
                {
                    if (obj1 == obj2)
                    {
                        continue;
                    }

                    double distance = rectDistance(obj1.Rect, obj2.Rect);
                    if (distance <= levCoreParammeter.DistanceTh)
                    {
                        if (!obj1.RelatedObjectlist.Contains(obj2))
                        {
                            obj1.RelatedObjectlist.Add(obj2);
                        }
                        if (!obj2.RelatedObjectlist.Contains(obj1))
                        {
                            obj2.RelatedObjectlist.Add(obj1);
                        }
                    }
                }
            }
            foreach (DetectObjectData obj1 in levResult.DetectDataList)
            {
                addDetectObject(obj1, levResult.DetectGroupDataList, null);
            }
            foreach (var datectGroupData in levResult.DetectGroupDataList)
            {
                //　外接矩形の座標計算
                datectGroupData.Update();
            }
            return true;
        }

        //bool DetectOLD(LevCoreParammeter levCoreParammeter, Mat inputImageMat, Mat generatedImageMat, LevResult levResult)
        //{
        //    levResult.DetectGroupDataList.Clear();

        //    bool status = GetDetectedAreaList(levCoreParammeter, inputImageMat, generatedImageMat, levResult);
        //    if (status == false)
        //    {
        //        levResult.ExecuteStatusType = ExecuteStatusTypes.DetectionError;
        //        return false;
        //    }
        //    // 近接した検出領域の関係付ける
        //    foreach (DetectObjectData obj1 in levResult.DetectDataList)
        //    {
        //        foreach (DetectObjectData obj2 in levResult.DetectDataList)
        //        {
        //            if (obj1 == obj2)
        //            {
        //                continue;
        //            }

        //            double distance = rectDistance(obj1.Rect, obj2.Rect);
        //            if (distance <= levCoreParammeter.DistanceTh)
        //            {
        //                if (!obj1.RelatedObjectlist.Contains(obj2))
        //                {
        //                    obj1.RelatedObjectlist.Add(obj2);
        //                }
        //                if (!obj2.RelatedObjectlist.Contains(obj1))
        //                {
        //                    obj2.RelatedObjectlist.Add(obj1);
        //                }
        //            }
        //        }
        //    }
        //    // 関係付けられた検出領域を検出グループにまとめる
        //    foreach (DetectObjectData obj in levResult.DetectDataList)
        //    {
        //        // 孤立した領域
        //        if (obj.RelatedObjectlist.Count == 0)
        //        {
        //            DetectGroupData detectGroupData = new DetectGroupData();
        //            detectGroupData.RelatedObjectlist.Add(obj);
        //            levResult.DetectGroupDataList.Add(detectGroupData);
        //        }
        //        else
        //        {
        //            DetectObjectData target = obj;

        //            foreach (var datectGropupData in levResult.DetectGroupDataList)
        //            {
        //                if (datectGropupData.RelatedObjectlist.Contains(obj))
        //                {
        //                    foreach (DetectObjectData obj2 in obj.RelatedObjectlist)
        //                    {
        //                        if (!datectGropupData.RelatedObjectlist.Contains(obj2))
        //                        {
        //                            datectGropupData.RelatedObjectlist.Add(obj2);
        //                        }
        //                    }
        //                    target = null;
        //                }
        //            }
        //            if (target != null)
        //            {
        //                DetectGroupData detectGroupData = new DetectGroupData();
        //                detectGroupData.RelatedObjectlist.Add(obj);
        //                foreach (DetectObjectData obj2 in obj.RelatedObjectlist)
        //                {
        //                    if (!detectGroupData.RelatedObjectlist.Contains(obj2))
        //                    {
        //                        detectGroupData.RelatedObjectlist.Add(obj2);
        //                    }
        //                }
        //                levResult.DetectGroupDataList.Add(detectGroupData);
        //            }
        //        }
        //    }
        //    foreach (var datectGroupData in levResult.DetectGroupDataList)
        //    {
        //        //　外接矩形の座標計算
        //        datectGroupData.Update();
        //    }

        //    return true;
        //}
        bool GetAnnotationData(LevCoreParammeter levCoreParammeter, string inputImageFilePath, string generatedImageFilePath, string annotationFilePath, LevResult levResult)
        {
            levResult.AnnotationDataList.Clear();
            int maskWidth = levCoreParammeter.MaskWidth;
            System.Windows.Rect ROI = new System.Windows.Rect(maskWidth, maskWidth, levResult.InputImageMat.Rows - maskWidth * 2, levResult.InputImageMat.Cols - maskWidth * 2);

            if (annotationFilePath == null || System.IO.File.Exists(annotationFilePath) == false)
            {
                levResult.ExecuteStatusType = ExecuteStatusTypes.NoAnnotation;
                return false;
            }
            PascalVocData pascalVocData = new PascalVocData();
            if (pascalVocData.Load(annotationFilePath) == false)
            {
                pascalVocData.Init(levResult.InputImageFilePath, levResult.InputImageMat.Rows, levResult.InputImageMat.Cols);
            }
            foreach (var obj in pascalVocData.ObjectList)
            {
                var box = obj.Box;
                System.Windows.Rect rect = new System.Windows.Rect(box.XMin, box.YMin, box.XMax - box.XMin, box.YMax - box.YMin);

                rect = System.Windows.Rect.Intersect(ROI, rect);
                if (rect != System.Windows.Rect.Empty)
                {
                    levResult.AnnotationDataList.Add(new AnnotationObjectData(rect.Left, rect.Top, rect.Width, rect.Height, obj.Difficult));
                }
            }

            if (levResult.AnnotationDataList.Count == 0)
            {
                levResult.ExecuteStatusType = ExecuteStatusTypes.NoAnnotation;
                //return;
            }
            return true;
        }
        public LevResult Execute(LevCoreParammeter levCoreParammeter, string inputImageFilePath, string generatedImageFilePath, string annotationFilePath)
        {
            LevResult levResult = new LevResult();

            levResult.InputImageFilePath = inputImageFilePath;
            levResult.GeneratedImageFilePath = generatedImageFilePath;
            levResult.AnnotationFilePath = annotationFilePath;

            if (inputImageFilePath == null || System.IO.File.Exists(inputImageFilePath) == false)
            {
                levResult.ExecuteStatusType = ExecuteStatusTypes.OtherError;
                return levResult;
            }
            Mat? inputImageMat = new OpenCvSharp.Mat(inputImageFilePath, ImreadModes.Grayscale);
            levResult.InputImageMat = inputImageMat;
            AddImage(inputImageMat, "OrgInput");

            if (generatedImageFilePath == null || System.IO.File.Exists(generatedImageFilePath) == false)
            {
                levResult.ExecuteStatusType = ExecuteStatusTypes.OtherError;
                return levResult;
            }
            Mat? generatedImageMat = new OpenCvSharp.Mat(generatedImageFilePath, ImreadModes.Grayscale);
            levResult.GeneratedImageMat = generatedImageMat;
            AddImage(generatedImageMat, "OrgGenerate");

            bool status = Detect(levCoreParammeter, inputImageMat, generatedImageMat, levResult);
            if (status == false)
            {
                levResult.ExecuteStatusType = ExecuteStatusTypes.DetectionError;
                return levResult;
            }

            GetAnnotationData(levCoreParammeter, inputImageFilePath, generatedImageFilePath, annotationFilePath, levResult);

            foreach (var datectGroupData in levResult.DetectGroupDataList)
            {
                //　外接矩形の座標計算
                //datectGroupData.Update();

                // しきい値以下で最も近接しているアノテーション領域の取得
                foreach (var AnnotationObjectData in levResult.AnnotationDataList)
                {
                    double distance = getDistance(datectGroupData.CenterPoint, AnnotationObjectData.CenterPoint);
                    if (distance < datectGroupData.Distannce)
                    {
                        datectGroupData.Distannce = distance;
                        datectGroupData.AnnotationObject = AnnotationObjectData;
                    }
                }
                // 近接しているアノテーション領域がない場合は過検出
                if (datectGroupData.AnnotationObject == null)
                {
                    datectGroupData.DetectObjectStatus = DetectObjectStatusTypes.OverDetection;
                }
                else
                {  // 近接しているアノテーション領域との距離がしきい値以下　または　中心点が他方の領域に含まれる場合　OK
                    if (datectGroupData.Distannce <= levCoreParammeter.JudgeTh
                        || datectGroupData.AnnotationObject.Rect.IntersectsWith(datectGroupData.CenterRect)
                        || datectGroupData.Rect.IntersectsWith(datectGroupData.AnnotationObject.CenterRect))
                    {
                        datectGroupData.AnnotationObject.ContainDetectGroupList.Add(datectGroupData);
                        datectGroupData.DetectObjectStatus = DetectObjectStatusTypes.OK;
                    }
                    else //　近接していない場合は過検出
                    {
                        datectGroupData.AnnotationObject = null;
                        datectGroupData.DetectObjectStatus = DetectObjectStatusTypes.OverDetection;
                    }
                }

                // 画像全体の評価に検出グループの評価を加える
                levResult.DetectStatus |= datectGroupData.DetectObjectStatus;

                if (levCoreParammeter.DetectSizeTh != 0)
                {
                    var size = Math.Sqrt(datectGroupData.WX * datectGroupData.WX + datectGroupData.WY * datectGroupData.WY);
                    if (size >= levCoreParammeter.DetectSizeTh)
                    {
                        levResult.DetectStatus |= DetectObjectStatusTypes.TooLargeObject;
                    }
                }
            }
            foreach (var anno in levResult.AnnotationDataList)
            {
                if (anno.ContainDetectGroupList.Count == 0)
                {
                    if (anno.Difficult)
                    {
                        levResult.DetectStatus |= DetectObjectStatusTypes.Whichever;
                    }
                    else
                    {
                        levResult.DetectStatus |= DetectObjectStatusTypes.Miss1;
                    }
                }
            }
            if (levCoreParammeter.DetectCountTh != 0)
            {
                if (levResult.DetectGroupDataList.Count >= levCoreParammeter.DetectCountTh)
                {
                    levResult.DetectStatus |= DetectObjectStatusTypes.TooManyDetectPoints;
                }
            }

            levResult.ExecuteStatusType = ExecuteStatusTypes.Executed;
            return levResult;
        }

        //public void Execute(LevCoreParammeter prData)
        //{
        //    string inputFilePath = prData.InputImageFilePath;
        //    string outputFilePath = prData.GeneratedImageFilePath;
        //    string annotationFilePath = prData.AnnotationFilePath;

        //    if (inputFilePath == null || System.IO.File.Exists(inputFilePath) == false)
        //    {
        //        prData.ExecuteStatusType = ExecuteStatusTypes.OtherError;
        //        return;
        //    }

        //    Mat? inputMat = new OpenCvSharp.Mat(inputFilePath, ImreadModes.Grayscale);
        //    prData.Images.Add(new Tuple<string, Mat>("OrginalInput", inputMat));

        //    //  double[] array = GrayMat2DoubleArray(inputMat);
        //    //  inputMat = DoubleArray2GrayMat(array, inputMat.Cols, inputMat.Rows);

        //    Mat? outputMat = null;
        //    if (outputFilePath == null || System.IO.File.Exists(outputFilePath) == false)
        //    {
        //        prData.InputImageMat = inputMat;
        //        prData.ExecuteStatusType = ExecuteStatusTypes.NoGeneratedImage;
        //        return;
        //    }
        //    outputMat = new OpenCvSharp.Mat(outputFilePath, ImreadModes.Grayscale);

        //    if (inputMat.Rows != outputMat.Rows || inputMat.Cols != outputMat.Cols)
        //    {
        //        outputMat = outputMat.Resize(new OpenCvSharp.Size(inputMat.Rows, inputMat.Cols), 0, 0, OpenCvSharp.InterpolationFlags.Cubic);
        //    }
        //    prData.Images.Add(new Tuple<string, Mat>("OrginalOutput", outputMat));



        //    prData.InputImageMat = inputMat;
        //    prData.GeneratedImageMat = outputMat;

        //    prData.AnnotationDataList.Clear();
        //    int maskWidth = prData.MaskWidth;
        //    System.Windows.Rect ROI = new System.Windows.Rect(maskWidth, maskWidth, prData.InputImageMat.Rows - maskWidth * 2, prData.InputImageMat.Cols - maskWidth * 2);
        //    //DetectionDataManager.PascalVocData? vocData = null;

        //    if (annotationFilePath != null && System.IO.File.Exists(annotationFilePath))
        //    {
        //        // vocData = new DetectionDataManager.PascalVocData(annotationFilePath);
        //        //foreach (var bbox in vocData.BBoxs)
        //        //{
        //        //    var box = bbox.Box
        //        //    System.Windows.Rect rect = new System.Windows.Rect(box.Left, box.Top, box.Width, box.Height);

        //        //    rect = System.Windows.Rect.Intersect(ROI, rect);
        //        //    if (rect != System.Windows.Rect.Empty)
        //        //    {
        //        //        prData.AnnotationDataList.Add(new AnnotationObjectData(rect.Left, rect.Top, rect.Width, rect.Height,bbox.));
        //        //    }
        //        //}
        //        PascalVocDataUtil.PascalVocData pascalVocData = new PascalVocDataUtil.PascalVocData();
        //        if (pascalVocData.Load(annotationFilePath) == false)
        //        {
        //            pascalVocData.Init(inputFilePath, prData.InputImageMat.Rows, prData.InputImageMat.Cols);
        //        }
        //        foreach (var obj in pascalVocData.ObjectList)
        //        {
        //            var box = obj.Box;
        //            System.Windows.Rect rect = new System.Windows.Rect(box.XMin, box.YMin, box.XMax - box.XMin, box.YMax - box.YMin);

        //            rect = System.Windows.Rect.Intersect(ROI, rect);
        //            if (rect != System.Windows.Rect.Empty)
        //            {
        //                prData.AnnotationDataList.Add(new AnnotationObjectData(rect.Left, rect.Top, rect.Width, rect.Height, obj.Difficult));
        //            }
        //        }
        //    }
        //    if (prData.AnnotationDataList.Count == 0)
        //    {
        //        prData.ExecuteStatusType = ExecuteStatusTypes.NoAnnotation;
        //        //return;
        //    }

        //    Mat detectMat = new Mat();
        //    Cv2.Absdiff(inputMat, outputMat, detectMat);
        //    prData.DiffImageMat = detectMat;

        //    LevBinarize levBinarize = new LevBinarize();
        //    if (this.AddImageFunc != null)
        //    {
        //        levBinarize.AddImageFunc = this.AddImageFunc;
        //    }
        //    Mat binary = levBinarize.Execute(prData, detectMat);

        //    AddImage(binary, "Bin");

        //    var erodeMat = new Mat(binary.Rows, binary.Cols, MatType.CV_8UC1);
        //    var dilateMat = new Mat(binary.Rows, binary.Cols, MatType.CV_8UC1);

        //    Cv2.Dilate(binary, dilateMat, null, iterations: 2);
        //    binary = dilateMat;
        //    AddImage(binary, "Dilate");

        //    Mat element = new Mat(3, 3, MatType.CV_8UC1);
        //    element.Set<byte>(0, 0, 0); element.Set<byte>(0, 1, 1); element.Set<byte>(0, 2, 0);
        //    element.Set<byte>(1, 0, 1); element.Set<byte>(1, 1, 1); element.Set<byte>(1, 2, 1);
        //    element.Set<byte>(2, 0, 0); element.Set<byte>(2, 1, 1); element.Set<byte>(2, 2, 0);

        //    Cv2.Erode(binary, erodeMat, element, iterations: 3);
        //    binary = erodeMat;
        //    AddImage(binary, "Erode");

        //    Cv2.Dilate(binary, dilateMat, null, iterations: 1);
        //    binary = dilateMat;
        //    AddImage(binary, "Dilate");


        //    binary = Mask(prData, binary, inputMat);

        //    prData.DetectMat = binary;

        //    ConnectedComponents cc = Cv2.ConnectedComponentsEx(binary);
        //    int cnt = 0;
        //    int areaTh = prData.AreaTh;
        //    int widthTh = prData.WidthTh;
        //    int heightTh = prData.HeightTh;

        //    foreach (var blob in cc.Blobs.Skip(1))
        //    {
        //        if (blob.Area < areaTh)
        //        {
        //            continue;
        //        }
        //        if (blob.Width < widthTh)
        //        {
        //            continue;
        //        }
        //        if (blob.Height < heightTh)
        //        {
        //            continue;
        //        }
        //        prData.DetectDataList.Add(new DetectObjectData(blob.Left, blob.Top, blob.Width, blob.Height));

        //        cnt++;
        //    }


        //    switch (prData.JudgeType)
        //    {
        //        case JudgeTypes.CenterPoint:

        //            // 近接した検出領域の関係付ける
        //            foreach (DetectObjectData obj1 in prData.DetectDataList)
        //            {
        //                foreach (DetectObjectData obj2 in prData.DetectDataList)
        //                {
        //                    if (obj1 == obj2)
        //                    {
        //                        continue;
        //                    }

        //                    double distance = rectDistance(obj1.Rect, obj2.Rect);
        //                    if (distance <= prData.DistanceTh)
        //                    {
        //                        if (!obj1.RelatedObjectlist.Contains(obj2))
        //                        {
        //                            obj1.RelatedObjectlist.Add(obj2);
        //                        }
        //                        if (!obj2.RelatedObjectlist.Contains(obj1))
        //                        {
        //                            obj2.RelatedObjectlist.Add(obj1);
        //                        }
        //                    }
        //                }
        //            }
        //            // 関係付けられた検出領域を検出グループにまとめる
        //            foreach (DetectObjectData obj in prData.DetectDataList)
        //            {
        //                // 孤立した領域
        //                if (obj.RelatedObjectlist.Count == 0)
        //                {
        //                    DetectGroupData detectGroupData = new DetectGroupData();
        //                    detectGroupData.RelatedObjectlist.Add(obj);
        //                    prData.DetectGroupDataList.Add(detectGroupData);
        //                }
        //                else
        //                {
        //                    DetectObjectData target = obj;

        //                    foreach (var datectGropupData in prData.DetectGroupDataList)
        //                    {
        //                        if (datectGropupData.RelatedObjectlist.Contains(obj))
        //                        {
        //                            foreach (DetectObjectData obj2 in obj.RelatedObjectlist)
        //                            {
        //                                if (!datectGropupData.RelatedObjectlist.Contains(obj2))
        //                                {
        //                                    datectGropupData.RelatedObjectlist.Add(obj2);
        //                                }
        //                            }
        //                            target = null;
        //                        }
        //                    }
        //                    if (target != null)
        //                    {
        //                        DetectGroupData detectGroupData = new DetectGroupData();
        //                        detectGroupData.RelatedObjectlist.Add(obj);
        //                        foreach (DetectObjectData obj2 in obj.RelatedObjectlist)
        //                        {
        //                            if (!detectGroupData.RelatedObjectlist.Contains(obj2))
        //                            {
        //                                detectGroupData.RelatedObjectlist.Add(obj2);
        //                            }
        //                        }
        //                        prData.DetectGroupDataList.Add(detectGroupData);
        //                    }
        //                }
        //            }


        //            foreach (var datectGroupData in prData.DetectGroupDataList)
        //            {
        //                //　外接矩形の座標計算
        //                datectGroupData.Update();

        //                // しきい値以下で最も近接しているアノテーション領域の取得
        //                foreach (var AnnotationObjectData in prData.AnnotationDataList)
        //                {
        //                    double distance = getDistance(datectGroupData.CenterPoint, AnnotationObjectData.CenterPoint);
        //                    if (distance < datectGroupData.Distannce)
        //                    {
        //                        datectGroupData.Distannce = distance;
        //                        datectGroupData.AnnotationObject = AnnotationObjectData;
        //                    }
        //                }
        //                // 近接しているアノテーション領域がない場合は過検出
        //                if (datectGroupData.AnnotationObject == null)
        //                {
        //                    datectGroupData.DetectObjectStatus = DetectObjectStatusTypes.OverDetection;
        //                }
        //                else
        //                {　　// 近接しているアノテーション領域との距離がしきい値以下　または　中心点が他方の領域に含まれる場合　OK
        //                    if (datectGroupData.Distannce <= prData.JudgeTh
        //                        || datectGroupData.AnnotationObject.Rect.IntersectsWith(datectGroupData.CenterRect)
        //                        || datectGroupData.Rect.IntersectsWith(datectGroupData.AnnotationObject.CenterRect))
        //                    {
        //                        datectGroupData.AnnotationObject.ContainDetectGroupList.Add(datectGroupData);
        //                        datectGroupData.DetectObjectStatus = DetectObjectStatusTypes.OK;
        //                    }
        //                    else　//　近接していない場合は過検出
        //                    {
        //                        datectGroupData.AnnotationObject = null;
        //                        datectGroupData.DetectObjectStatus = DetectObjectStatusTypes.OverDetection;
        //                    }
        //                }

        //                // 画像全体の評価に検出グループの評価を加える
        //                prData.DetectStatus |= datectGroupData.DetectObjectStatus;

        //                if (prData.DetectSizeTh != 0)
        //                {
        //                    var size = Math.Sqrt(datectGroupData.WX * datectGroupData.WX + datectGroupData.WY * datectGroupData.WY);
        //                    if (size >= prData.DetectSizeTh)
        //                    {
        //                        prData.DetectStatus |= DetectObjectStatusTypes.TooLargeObject;
        //                    }
        //                }
        //            }
        //            foreach (var anno in prData.AnnotationDataList)
        //            {
        //                if (anno.ContainDetectGroupList.Count == 0)
        //                {
        //                    if (anno.Difficult)
        //                    {
        //                        prData.DetectStatus |= DetectObjectStatusTypes.Whichever;
        //                    }
        //                    else
        //                    {
        //                        prData.DetectStatus |= DetectObjectStatusTypes.Miss1;
        //                    }
        //                }
        //            }
        //            if (prData.DetectCountTh != 0)
        //            {
        //                if (prData.DetectGroupDataList.Count >= prData.DetectCountTh)
        //                {
        //                    prData.DetectStatus |= DetectObjectStatusTypes.TooManyDetectPoints;
        //                }
        //            }
        //            break;

        //        default:
        //            break;
        //    }
        //}
        Mat Mask(int maskWidth, Mat binMat, Mat orgMat)
        {
            Mat destMat = new Mat(binMat.Rows, binMat.Cols, MatType.CV_8UC1, new Scalar(0, 0, 0));
            Mat bmaskMat = new Mat(binMat.Rows, binMat.Cols, MatType.CV_8UC1, new Scalar(0, 0, 0));
            //Mat wmaskMat = new Mat(binMat.Rows, binMat.Cols, MatType.CV_8UC1, new Scalar(0, 0, 0));
            //at tmaskMat = new Mat(binMat.Rows, binMat.Cols, MatType.CV_8UC1, new Scalar(0, 0, 0));
            Cv2.Rectangle(bmaskMat, new OpenCvSharp.Point(maskWidth, maskWidth), new OpenCvSharp.Point(binMat.Rows - maskWidth, binMat.Cols - maskWidth),
                new Scalar(255, 255, 255), -1);
            AddImage(bmaskMat, "bmask");

            //Mat wMat = orgMat.Threshold(254, 255, ThresholdTypes.BinaryInv);
            //AddImage(wMat, "wite");

            //Cv2.Erode(wMat, wmaskMat, null, iterations: 1);
            //AddImage(wmaskMat, "wmaskMat");

            //Cv2.BitwiseAnd(wmaskMat, bmaskMat, tmaskMat);
            //AddImage(tmaskMat, "tmaskMat");

            Cv2.BitwiseAnd(binMat, bmaskMat, destMat);
            //AddImage(bmaskMat, "mask");

            return destMat;
        }
        Mat MaskOLD2(int maskWidth, Mat binMat, Mat orgMat)
        {
            Mat destMat = new Mat(binMat.Rows, binMat.Cols, MatType.CV_8UC1, new Scalar(0, 0, 0));
            Mat bmaskMat = new Mat(binMat.Rows, binMat.Cols, MatType.CV_8UC1, new Scalar(0, 0, 0));
            Mat wmaskMat = new Mat(binMat.Rows, binMat.Cols, MatType.CV_8UC1, new Scalar(0, 0, 0));
            Mat tmaskMat = new Mat(binMat.Rows, binMat.Cols, MatType.CV_8UC1, new Scalar(0, 0, 0));
            Cv2.Rectangle(bmaskMat, new OpenCvSharp.Point(maskWidth, maskWidth), new OpenCvSharp.Point(binMat.Rows - maskWidth, binMat.Cols - maskWidth),
                new Scalar(255, 255, 255), -1);
            //AddImage(bmaskMat, "bmask");

            Mat wMat = orgMat.Threshold(254, 255, ThresholdTypes.BinaryInv);
            //AddImage(wMat, "wite");

            Cv2.Erode(wMat, wmaskMat, null, iterations: 1);
            //AddImage(wmaskMat, "wmaskMat");

            Cv2.BitwiseAnd(wmaskMat, bmaskMat, tmaskMat);
            //AddImage(tmaskMat, "tmaskMat");

            Cv2.BitwiseAnd(binMat, tmaskMat, destMat);
            //AddImage(bmaskMat, "mask");

            return destMat;
        }

        Mat MaskOLD(int maskWidth, Mat binMat, Mat orgMat)
        {
            Mat destMat = new Mat(binMat.Rows, binMat.Cols, MatType.CV_8UC1, new Scalar(0, 0, 0));
            Mat bmaskMat = new Mat(binMat.Rows, binMat.Cols, MatType.CV_8UC1, new Scalar(0, 0, 0));
            Mat wmaskMat = new Mat(binMat.Rows, binMat.Cols, MatType.CV_8UC1, new Scalar(0, 0, 0));
            Mat tmaskMat = new Mat(binMat.Rows, binMat.Cols, MatType.CV_8UC1, new Scalar(0, 0, 0));
            Cv2.Rectangle(bmaskMat, new OpenCvSharp.Point(maskWidth, maskWidth), new OpenCvSharp.Point(binMat.Rows - maskWidth, binMat.Cols - maskWidth),
                new Scalar(255, 255, 255), -1);
            //AddImage(bmaskMat, "bmask");

            Mat wMat = orgMat.Threshold(254, 255, ThresholdTypes.BinaryInv);
            //AddImage(wMat, "wite");

            Cv2.Erode(wMat, wmaskMat, null, iterations: 1);
            //AddImage(wmaskMat, "wmaskMat");

            Cv2.BitwiseAnd(wmaskMat, bmaskMat, tmaskMat);
            //AddImage(tmaskMat, "tmaskMat");

            Cv2.BitwiseAnd(binMat, tmaskMat, destMat);
            //AddImage(bmaskMat, "mask");

            return destMat;
        }
        static double getDistance(System.Windows.Point p1, System.Windows.Point p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }
        double rectDistance(System.Windows.Rect Rect1, System.Windows.Rect Rect2)
        {
            System.Windows.Rect ra;
            System.Windows.Rect rb;
            if (Rect1.Left < Rect2.Left)
            {
                ra = Rect1;
                rb = Rect2;
            }
            else
            {
                ra = Rect2;
                rb = Rect1;
            }
            double dx = rb.Left - (ra.Left + ra.Width);
            dx = Math.Max(0, dx);

            if (Rect1.Top < Rect2.Top)
            {
                ra = Rect1;
                rb = Rect2;
            }
            else
            {
                ra = Rect2;
                rb = Rect1;
            }
            double dy = rb.Top - (ra.Top + ra.Height);
            dy = Math.Max(0, dy);

            return Math.Sqrt(dx * dx + dy * dy);
        }


    }
}