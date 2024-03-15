using IjhCommonUtility;
using NetMQ.Sockets;
using NetMQ;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Size = OpenCvSharp.Size;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;
using ServerSoftware;
using LevCommon;
using System.Runtime.Serialization;

namespace Wafer
{

    /// <summary>
    /// 検出・検査関連の処理をpartial classとして分離
    /// </summary>
    [DataContract]
    public class WaferInspectByDLParameter
    {
        [DataMember]
        public Size CutImageSize = new Size(640, 640);
        [DataMember]
        public Size CutImageMarginSize = new Size(320, 320);
        [DataMember]
        public int OverlapMarginSize = 3;
        [DataMember]
        public double OverlapAreaRatio = 0.01;
        [DataMember]
        public int thresh_avoidBlackImage = 20;//真っ黒画像を避ける
        [DataMember]
        public int chunkSize = 500;//サーバーに送るときのひとまとめの単位
        [DataMember]
        public double thresh_score = 0.5;
        [DataMember]
        public Size dlImageSize = new Size(320, 320);
        [DataMember]
        public string port = "tcp://133.139.81.94:5556";
        [DataMember]
        public string readPath = "";
        [DataMember]
        public int inspModelId = 1;
        [DataMember]
        public int positionDetectModelId = 22;
        [DataMember]
        public int timeout_second = 100;
        [DataMember]
        public string PositionDetectParamPath = "";
        [DataMember]
        public string LevParamPath = "";
        public WaferInspectByDLParameter() { }
        public WaferInspectByDLParameter(string xmlPath)
        {
            DataContractReaderWriter.ReadXml_WithoutException(this, xmlPath);
            readPath = xmlPath;
        }
        public bool Save(string xmlPath)
        {
            return DataContractReaderWriter.WriteXml_WithoutException(this, xmlPath);
        }
    }
    public class WaferInspectByDL//とりあえずpartial classとして実装
    {
        public WaferInspectByDLParameter InspParam = new WaferInspectByDLParameter();
        public PositionDetectCommonParameter PositionDetectParam;
        public LevCoreParammeter LevParam;
        public WaferInspectByDL(string InspParamPath)
        {
            this.InspParam = new WaferInspectByDLParameter(InspParamPath);
            this.PositionDetectParam = PositionDetectCommonParameter.Create(this.InspParam.PositionDetectParamPath);
            this.LevParam = LevCoreParammeter.Create(this.InspParam.LevParamPath);
        }

        public void InspectWafer_Part(Mat img, string imgPath, string outputDir, int i, CutAreaParam cutParam)
        {
            double dupTh = 0.1;//重複除去の閾値（面積比）
            var sw = Stopwatch.StartNew();
            if (cutParam.CutAreas[0] == Rect.Empty) return;
            var fileNameBase = Path.GetFileNameWithoutExtension(imgPath);
            var outputDir2 = Path.Combine(outputDir, fileNameBase);
            Directory.CreateDirectory(outputDir2);
            //OKNG判定
            var tmpResults = this.InspectStrip(img, i, cutParam, this.InspParam);
            //NG結果の保存
            var ngTmpResults = tmpResults.Where(c => c.Score > this.InspParam.thresh_score).ToList();
            var csvHeader = ResultClass.GetHeader();
            var csvContents_tmp = new List<string>() { csvHeader };
            var inspResults = new List<ResultClass>();
            for (int j = 0; j < ngTmpResults.Count; j++)
            {
                var r = ngTmpResults[j];
                var cutImgPath = Path.Combine(outputDir2, $"i{r.Id}_d{j:d3}_RX{r.Rect.X}_RY{r.Rect.Y}_RW{r.Rect.Width}_RH{r.Rect.Height}_{r.Score:f3}.tif");
                ngTmpResults[j] = (r.Id, r.Rect, r.Img, r.Score, ImgPath: cutImgPath);
                var result = new ResultClass(r.Id, "","", j, r.Rect, r.Score, cutImgPath);
                csvContents_tmp.Add(result.GetString());
                inspResults.Add(result);
                r.Img.SaveImage(cutImgPath);
            }
            File.WriteAllLines(Path.Combine(outputDir2, "result_cutImgs.csv"), csvContents_tmp, Encoding.UTF8);
            //NGの詳細位置検出
            var detectResults = DetectPosition(ngTmpResults.Select(n => n.Img).ToList(), inspResults, dupTh);
            var csvContents = new List<string>() { csvHeader };
            csvContents.AddRange(detectResults.Select(d => d.GetString()));
            File.WriteAllLines(Path.Combine(outputDir2, "result.csv"), csvContents, Encoding.UTF8);
            tmpResults.ForEach(r => r.Img.Dispose());
            Trace.WriteLine($"{Path.GetFileName(imgPath)}:{sw.Elapsed} [s], {tmpResults.Count}cut, {ngTmpResults.Count}ngs");
        }

        //撮像したウエハ画像を検査する処理
        //出力はフォルダごとに分かれる
        public string InspectWafer(List<Mat> imgs, List<string> imgPaths, string outputDir, CutAreaParam cutParam, IProgress<string> progress = null)
        {
            var thresh_avoidBlackImage = 50;//真っ黒画像を避ける
            var chunkSize = 1000;//サーバーに送るときのひとまとめの単位
            var thresh_score = 0.5;
            var dlImageSize = new Size(320, 320);
            var port = "tcp://133.139.81.94:5556";
            var sw_insp = Stopwatch.StartNew();

            var waferDir = Path.GetDirectoryName(imgPaths[0]);
            var sw_wafer = Stopwatch.StartNew();
            //sw_wafer.Restart();
            var outputDir1 = Path.Combine(outputDir, Path.GetFileName(waferDir));
            Directory.CreateDirectory(outputDir1);
            for (int i = 0; i < imgPaths.Count; i++)
            {
                progress?.Report($"1.検査中...{i + 1}/{imgPaths.Count}");
                var sw_img = Stopwatch.StartNew();
                if (cutParam.CutAreas[0] == Rect.Empty) continue;
                var fileNameBase = Path.GetFileNameWithoutExtension(imgPaths[i]);
                var outputDir2 = Path.Combine(outputDir1, fileNameBase);
                Directory.CreateDirectory(outputDir2);
                var results = this.InspectStrip(imgs[i], i, cutParam, this.InspParam);
                var ngResults = results.Where(c => c.Score > thresh_score).ToList();
                var csvHeader = ResultClass.GetHeader();
                var csvContents = new List<string>() { csvHeader };
                for (int j = 0; j < ngResults.Count; j++)
                {
                    var r = ngResults[j];
                    var cutImgPath = Path.Combine(outputDir2, $"i{r.Id}_d{j:d3}_RX{r.Rect.X}_RY{r.Rect.Y}_RW{r.Rect.Width}_RH{r.Rect.Height}_{r.Score:f3}.tif");
                    ngResults[j] = (r.Id, r.Rect, r.Img, r.Score, ImgPath: cutImgPath);
                    var result = new ResultClass(r.Id, "","", j, r.Rect, r.Score, r.ImgPath);
                    csvContents.Add(result.GetString());
                    r.Img.SaveImage(cutImgPath);
                }
                File.WriteAllLines(Path.Combine(outputDir2, "result.csv"), csvContents, Encoding.UTF8);
                results.ForEach(r => r.Img.Dispose());
                Trace.WriteLine($"{Path.GetFileName(imgPaths[i])}:{sw_img.Elapsed} [s], {results.Count}cut");
            }
            Trace.WriteLine($"{Path.GetFileName(waferDir)}:{sw_wafer.Elapsed} [s]");
            Trace.WriteLine($"InspectTime:{sw_insp.Elapsed} [s]");
            return outputDir1;
        }
        /// <summary>
        /// 短冊状態の元画像を検査する処理
        /// </summary>
        /// <param name="src"></param>
        /// <param name="stripId"></param>
        /// <param name="cutParam"></param>
        /// <param name="thresh_avoidBlackImage"></param>
        /// <param name="chunkSize"></param>
        /// <param name="dlImageSize"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public List<(int Id, Rect Rect, Mat Img, double Score, string ImgPath)> InspectStrip(Mat src, int stripId, CutAreaParam cutParam, WaferInspectByDLParameter inspParam)
        {
            var rects = new List<Rect>();
            foreach (var area in cutParam.CutAreas)
            {
                rects.AddRange(WaferInspectCommon.CutToRect(area.Size, inspParam.CutImageSize, inspParam.CutImageMarginSize).Select(a => a.Shift(area.X, area.Y)).ToList());
            }
            List<(int Id, Rect Rect, Mat Img)> cutResults = new List<(int Id, Rect Rect, Mat Img)>();
            for (int j = 0; j < rects.Count; j++)
            {
                var r = rects[j];
                MatFunctions.ThrowIfRectOverSize(src.Size(), r);
                var roiMat = MatFunctions.RoiToMat(src, r);
                if (roiMat.Mean()[0] > inspParam.thresh_avoidBlackImage) cutResults.Add((stripId, r, roiMat));//真っ黒画像を除外する
            }
            var scores = new List<double>();
            foreach (var c in cutResults.Chunk(inspParam.chunkSize))
            {
                int retryCount = 3;
                List<float[]> tmp = null;
                for (int i = 0; i < retryCount; i++)//通信エラーが出る場合があるので暫定策
                {
                    try
                    {
                        tmp = GetDlResult_Float(c.Select(c => c.Img).ToList(), inspParam.dlImageSize, 0, 100, inspParam.port);
                        break;
                    }
                    catch (Exception e)
                    {
                        //デバッグ用
                        var errorDir = Path.Combine("error", DateTime.Now.ToString("yyyyMMddHHmmss"));
                        Directory.CreateDirectory(errorDir);
                        var tmpError = c.ToList();
                        tmpError.ForEach(c => c.Img.SaveImage(Path.Combine(errorDir, $"strip{stripId}_{c.Id}_X{c.Rect.X}_Y{c.Rect.Y}_i{c.Id}.tif")));
                        foreach (var t in tmpError)
                        {
                            if (t.Img.Size() == Size.Zero) Trace.WriteLine($"Size==0  i{t.Id}, {t.Rect}");
                        }
                        Trace.WriteLine($"通信に失敗したのでリトライします。{i + 1}/{retryCount}");
                    }
                }

                if (tmp == null)
                {
                    throw new Exception($"DLサーバー通信エラー: stripId {stripId}");
                }
                else scores.AddRange(tmp.Select(t => (double)t[0]));

            };
            //しきい値を超えるスコアの画像は保存
            var results = cutResults.Zip(scores, (c, s) => (c.Id, c.Rect, c.Img, Score: s, ImgPath: "")).ToList();
            return results;
        }

        public List<Mat> GetDlResult_Mat(List<Mat> imgs, Size sizeForResize, int modelId, int timeoutSecond, string connectBindPort = "tcp://localhost:5556")
        {
            var msg = GetDlResultCore(imgs, sizeForResize, modelId, timeoutSecond, connectBindPort);
            var resizedSize = sizeForResize == Size.Zero ? imgs[0].Size() : sizeForResize;

            return msg.Select(m => ConvertBufferToMat(m, resizedSize)).ToList();

            Mat ConvertBufferToMat(byte[] buffer, Size size)
            {
                Mat result = new Mat(size.Height, size.Width, MatType.CV_8UC1, buffer).Clone();
                return result;
            }

        }
        public List<float[]> GetDlResult_Float(List<Mat> imgs, Size sizeForResize, int modelId, int timeoutSecond, string connectBindPort = "tcp://localhost:5556")
        {
            var msg = GetDlResultCore(imgs, sizeForResize, modelId, timeoutSecond, connectBindPort);
            return msg.Select(m => ConvertBufferToFloat(m)).ToList();

            float[] ConvertBufferToFloat(byte[] buffer)
            {
                var length = buffer.Length / 4;
                var result = new float[length];
                for (int i = 0; i < length; i++)
                {
                    result[i] = BitConverter.ToSingle(buffer, i * 4);
                }
                return result;
            }

        }
        private List<byte[]> GetDlResultCore(List<Mat> imgs, Size sizeForResize, int modelId, int timeoutSecond, string connectBindPort = "tcp://localhost:5556")
        {
            //if(this.DlParam.To3ch) imgs = imgs.Select(img=>img.CvtColor(ColorConversionCodes.GRAY2BGR)).ToList();   //全チャンネル同値なのでBGRとか気にしない
            if (imgs.Count == 0) throw new ArgumentException($"画像が見つかりません ");
            int rows = 0, cols = 0, channels = 0, imgCount = 0;
            if (sizeForResize != Size.Zero && sizeForResize != imgs[0].Size()) imgs = imgs.Select(img => img.Resize(sizeForResize)).ToList();
            rows = imgs[0].Rows;
            cols = imgs[0].Cols;
            channels = imgs[0].Channels();
            if (channels > 1) throw new NotImplementedException($"1Ch以外の画像は未対応です");
            imgCount = imgs.Count;
            var byteList = new List<byte[]>();
            byteList.AddRange(imgs.Select(img => new Mat<byte>(img).ToArray()));
            var imgData = new byte[byteList.Count * byteList[0].Length];
            for (int i = 0; i < byteList.Count; i++)
            {
                //Console.WriteLine(byteList[i].Length);
                Buffer.BlockCopy(byteList[i], 0, imgData, i * byteList[i].Length, byteList[i].Length);
            }
            using (var socket = new RequestSocket())
            {
                //socket.Connect("tcp://localhost:5556");
                socket.Connect(connectBindPort);
                var frames = BitConverter.GetBytes(modelId);
                socket.SendMoreFrame(frames);
                var frames1 = BitConverter.GetBytes(imgCount);
                socket.SendMoreFrame(frames1);
                var frames2 = BitConverter.GetBytes(rows);
                socket.SendMoreFrame(frames2);
                var frames3 = BitConverter.GetBytes(cols);
                socket.SendMoreFrame(frames3);
                var frames4 = BitConverter.GetBytes(channels);
                socket.SendMoreFrame(frames4);
                socket.SendFrame(imgData);

                var retryLimit = 1;
                for (int i = 0; i < retryLimit; i++)
                {
                    if (socket.Poll(PollEvents.PollIn, TimeSpan.FromSeconds(timeoutSecond)) == PollEvents.PollIn)
                    {
                        var results = new List<float[]>();
                        var msg = socket.ReceiveMultipartBytes();
                        return msg;
                    }
                    else if (i == retryLimit) throw new Exception("●DL処理サーバーとの通信に失敗しました。");
                }
                throw new Exception("●DL処理サーバーとの通信に失敗しました?");
            }
        }
        public List<ResultClass> DetectPosition(List<Mat> srcImgs, List<ResultClass> srcResults, double duplicateThresh)
        {
            if (srcImgs.Count == 0) return new List<ResultClass>();
            var imgs = srcImgs.Select(s => s.Resize(InspParam.dlImageSize, 0, 0, InterpolationFlags.Area)).ToList();
            var resizeRate = (double)InspParam.dlImageSize.Width / srcImgs[0].Size().Width;
            List<Mat> gens = GetDlResult_Mat(imgs, this.InspParam.dlImageSize, this.InspParam.positionDetectModelId, this.InspParam.timeout_second, this.InspParam.port);
            //var debugDir = @"I:\DevWork\GanOutput";
            //Directory.CreateDirectory(debugDir);
            //for(int i=0; i<imgs.Count; i++)
            //{
            //    var p = Path.Combine(debugDir, Path.GetFileName(srcResults[i].ImgPath));
            //    gens[i].SaveImage(p);
            //}
            List<ResultClass> results = new List<ResultClass>();
            for (int i = 0; i < imgs.Count; i++)
            {
                //(_, var tmpRects) = GetDetectPosition_Asano(imgs[i], gens[i]);
                var tmpRects = GetDetectPosition(imgs[i], gens[i]);
                List<Rect2d> scoreRects = tmpRects.Select(r => r.To2d()).ToList();
                if (scoreRects == null)
                {
                    results.Add(srcResults[i]);
                    continue;
                }
                foreach (Rect2d scoreRect in scoreRects)
                {
                    var s = srcResults[i];
                    var result = new ResultClass(s.StripId, s.ChipName, s.AreaName, s.DefectId, s.Rect, s.Score, s.ImgPath, s.ImgPath2);
                    result.Rect = scoreRect.ResizeFromZero(1.0 / resizeRate).Add(result.Rect.Location);
                    results.Add(result);
                }
            }
            //重複除去
            var mergedResults = WaferInspectCommon.MergeDuplicate(results, duplicateThresh);
            return mergedResults;
        }

        private List<Rect> GetDetectPosition(Mat img, Mat gen)
        {
            List<Rect> result = new List<Rect>();
            //ここで中山さんソフトから情報を吸いだす
            result = new LevCore().GetDetectedRectList(this.LevParam, img, gen);
            return result;
        }

        //浅野独自で位置検出も作ってみたがイマイチ、、
        public (Mat, List<Rect>) GetDetectPosition_Asano(Mat src1, Mat src2)
        {
            int DifferenceTh_MinDiff = 12;
            double DifferenceTh_RateFromMaxDiff = 0.5;
            var filtSize = new Size(3, 3);
            double standardVal = 128;
            double ignoreAreaRate = 0.9;

            //前処理（平均値を揃える）
            double ave1 = src1.Mean()[0];
            double ave2 = src2.Mean()[0];
            using var tmpSrc1 = src1.Mul(standardVal / ave1).ToMat();
            using var tmpSrc2 = src2.Mul(standardVal / ave2).ToMat();
            //using var tmpSrc1 = src1.EqualizeHist();//ヒストグラム平滑化（没）
            //using var tmpSrc2 = src2.EqualizeHist();

            //MatFunctions.ShowImage(tmpSrc1,"1");
            //MatFunctions.ShowImage(tmpSrc2,"2");

            //差分画像作成＆後処理
            var diff = new Mat();
            Cv2.Absdiff(tmpSrc1, tmpSrc2, diff);
            Cv2.Blur(diff, diff, filtSize);
            //MatFunctions.ShowImage(diff,"diff");
            diff.MinMaxLoc(out double minVal, out double diffMaxVal);
            var th = Math.Max(DifferenceTh_MinDiff, diffMaxVal * DifferenceTh_RateFromMaxDiff);
            Cv2.Threshold(diff, diff, th, 255, ThresholdTypes.Binary);
            //MatFunctions.ShowImage(diff, "diff");
            Cv2.MorphologyEx(diff, diff, MorphTypes.Erode, null);
            Cv2.MorphologyEx(diff, diff, MorphTypes.Dilate, null, null, 2);//ちょっと大きめに出す
            var cc = Cv2.ConnectedComponentsEx(diff, PixelConnectivity.Connectivity8);
            var rects = cc.Blobs.Skip(1).Select(b => b.Rect).ToList();
            var roi = new Rect(new Point(0, 0), src1.Size()).ResizeFromCenter(ignoreAreaRate);
            rects = rects.Where(r => roi.IntersectsWith(r)).ToList();
            if (rects.Count == 0) rects.Add(new Rect(new Point(0, 0), src1.Size()));
            return (diff, rects);
        }

    }
}
