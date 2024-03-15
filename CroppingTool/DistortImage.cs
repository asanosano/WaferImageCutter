using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using System.Diagnostics;
using IjhCommonUtility;

namespace PseudoDefectMaker
{
    public static class Extentions
    {
        public static Point2f To2f(this Point p)
        {
            return new Point2f(p.X, p.Y);
        }
    }
    public class MeshDistortion
    {
        Random rnd = new Random();
        int MeshResolution;
        public MeshDistortion(int meshResolution = 15)
        {
            this.MeshResolution = meshResolution;
        }
        public class Mesh
        {
            public MeshPoint[,] Points { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int MeshWidth { get; }
            public int MeshHeight { get; }
            public Mesh(int width, int height, int meshResolution = 15)
            {
                var aspect = Math.Max(width, height) / Math.Min(width, height);
                var smaller = meshResolution;
                var larger = meshResolution * aspect;
                var horizontal_tiles = width < height ? smaller : larger;//メッシュ形状がほぼ正方形になるよう個数を調整（長方形だと変形後乱れやすい）
                var vertical_tiles = height < width ? smaller : larger;
                var xPointCount = horizontal_tiles + 1;
                var yPointCount = vertical_tiles + 1;
                this.Width = width;
                this.Height = height;
                Points = new MeshPoint[xPointCount, yPointCount];
                (this.MeshWidth, this.MeshHeight) = (xPointCount, yPointCount);

                var width_of_square = (int)Math.Floor(width / (float)horizontal_tiles);
                var height_of_square = (int)Math.Floor(height / (float)vertical_tiles);

                var width_of_last_square = width - (width_of_square * (horizontal_tiles - 1));
                var height_of_last_square = height - (height_of_square * (vertical_tiles - 1));
                for (int x = 0; x < xPointCount; x++)
                {
                    for (int y = 0; y < yPointCount; y++)
                    {
                        var isEdge = (x == 0 || y == 0 || x == xPointCount - 1 || y == yPointCount - 1) ? true : false;
                        (var tmpX, var tmpY) = (x * width_of_square, y * height_of_square);
                        if (x == xPointCount - 1) tmpX = width - 1;//最後のブロックは大きめ
                        if (y == yPointCount - 1) tmpY = height - 1;
                        Points[x, y] = new MeshPoint(tmpX, tmpY, x, y, isEdge);
                    }
                }

            }
            public void ViewMesh()
            {
                var canvas = new Mat(this.Height, this.Width, MatType.CV_8UC1, new Scalar(0));
                for (int i = 0; i < this.MeshWidth; i++)
                {
                    for (int j = 0; j < this.MeshHeight; j++)
                    {
                        Cv2.Circle(canvas, this.Points[i, j].X, this.Points[i, j].Y, 2, 255, -1);
                    }
                }
                MatFunctions.ShowImage(canvas);
            }
            public List<List<Point2f>> GetMeshBlocks()
            {
                var r = new List<List<Point2f>>();
                var p = this.Points;
                var w = this.MeshWidth - 1;
                var h = this.MeshHeight - 1;
                Point2f To2f(Point point) => new Point2f(point.X, point.Y);
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        r.Add(new List<Point2f> { p[x, y].GetPoint2f(), p[x, y + 1].GetPoint2f(), p[x + 1, y + 1].GetPoint2f(), p[x + 1, y].GetPoint2f() });
                    }
                }
                return r;
            }
            public void MoveMeshPoint1(int meshX, int meshY, int moveX, int moveY)
            {
                var p = this.Points[meshX, meshY];
                p.X += moveX;
                p.Y += moveY;
            }
            public void MoveMeshPoint(int meshX, int meshY, int moveX, int moveY)
            {
                if (meshX < this.MeshWidth || meshY < this.MeshHeight) return;
                var ps = this.Points;
                var p = this.Points[meshX, meshY];
                var movedX = p.X + moveX;
                var movedY = p.Y + moveY;
                if (movedX < 0 || movedX >= this.Width || movedY < 0 || movedY >= this.Height) return;

                double Distance(int x1, int y1, int x2, int y2) => Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
                int MoveTo(int r1, int r2, int move)
                {
                    return move / (r1 - r2);
                }

            }
        }
        public class MeshPoint
        {
            public int X { get; set; }
            public int Y { get; set; }
            public Point MeshId { get; }
            public bool IsEdge { get; }
            public Point2f GetPoint2f() => new Point2f(X, Y);
            public Point GetPoint() => new Point(X, Y);
            public MeshPoint(int x, int y, int mx, int my, bool isEdge)
            {
                (X, Y, MeshId, IsEdge) = (x, y, new Point(mx, my), isEdge);
            }
            public void Move(int x, int y)
            {
                if (IsEdge) return;
                X += x;
                Y += y;
                if (X < 0) X = 0;
                if (Y < 0) Y = 0;
            }
            public void MoveTo(int x, int y)
            {
                if (IsEdge) return;
                X = x;
                Y = y;
                if (X < 0) X = 0;
                if (Y < 0) Y = 0;
            }

        }
        public Mat RandomDistort(Mat src, int moveMin, int moveMax, int seed = 0)
        {
            if (seed != 0) this.rnd = new Random(seed);
            var move = rnd.Next(moveMin, moveMax);
            if (move == 0) return src.Clone();
            var mesh = new Mesh(src.Width, src.Height, MeshResolution);
            var meshSize = Math.Min(mesh.MeshWidth, mesh.MeshHeight);
            var kernel = rnd.Next(2, meshSize / 2) * 2 + 1;
            var k = kernel / 2;
            var x = rnd.Next(k, mesh.MeshWidth - k);
            var y = rnd.Next(k, mesh.MeshHeight - k);
            if (rnd.Next(0, 2) == 0) move *= -1;
            var vertical = rnd.Next(0, 2) == 0;
            //Trace.WriteLine($"k{kernel}, m{move}");
            return Distort(src, x, y, kernel, move, vertical);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="meshPointX"></param>
        /// <param name="meshPointY"></param>
        /// <param name="kernel"></param>
        /// <param name="move"></param>
        /// <param name="Vertical">移動方向が縦(true)/横(false)</param>
        /// <param name="mesh"></param>
        /// <param name="spread">周辺メッシュの移動方向が放射(true)同じ(false)</param>
        /// <returns></returns>
        public Mat Distort(Mat src, int meshPointX, int meshPointY, int kernel, int move, bool isVertical = true, bool isSpread = true, Mesh mesh = null)
        {
            mesh = mesh ?? new Mesh(src.Width, src.Height, MeshResolution);
            var srcMesh = mesh.GetMeshBlocks();
            (var pointX, var pointY) = (meshPointX, meshPointY);
            double Distance(int x, int y)
            {
                return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
            }
            double Gauss(double sigma, double x) => Math.Exp(-Math.Pow(x, 2) / Math.Pow(sigma, 2) / 2);//最大値1となる正規分布
            int k = kernel / 2;
            for (int i = -kernel / 2; i < k + 1; i++)
            {
                for (int j = -kernel / 2; j < k + 1; j++)
                {
                    var m = move * Gauss((double)k / 2, Distance(i, j));
                    if (isSpread)
                    {
                        m = m < 0 ? -m : m;
                        if (isVertical) m = j < 0 ? -m : m;
                        else m = i < 0 ? -m : m;
                    }
                    if (isVertical) mesh.Points[pointX + i, pointY + j].Move(0, (int)m);
                    else mesh.Points[pointX + i, pointY + j].Move((int)m, 0);
                }
            }
            var dstMesh = mesh.GetMeshBlocks();
            //mesh.ViewMesh();
            return Distort(src, srcMesh, dstMesh);
        }
        public Mat Rotate(Mat src, Mesh mesh = null)
        {
            //簡易円周補正　０を原点に遠くに行くほど大きく回転させる
            var rotateOrgPoint = new Point(0, src.Height);
            rotateOrgPoint = new Point(0, 0);
            var maxRadian = -0.125;
            //var skew = 0.1;
            mesh = mesh ?? new Mesh(src.Width, src.Height, MeshResolution);
            var srcMesh = mesh.GetMeshBlocks();
            for (int j = 0; j < mesh.MeshHeight; j++)
            {
                for (int i = 0; i < mesh.MeshWidth; i++)
                {
                    var p = mesh.Points[i, j];
                    var radian = p.Y == 0 ? 0 : Math.PI - Math.Atan((double)p.X / p.Y);
                    var m = MatFunctions.RotatePoint_Clockwise(p.GetPoint(), rotateOrgPoint, maxRadian * p.X / src.Width);
                    //m = m.Add(new Point(Point.Distance(rotateOrgPoint,p.GetPoint()) * skew, 0));
                    p.MoveTo(m.X, m.Y);
                }
            }
            var dstMesh = mesh.GetMeshBlocks();
            //mesh.ViewMesh();
            return Distort(src, srcMesh, dstMesh);

        }

        public static void Main(Mat src)
        {
            // 入力画像中の四角形の頂点座標
            var srcPoints = new Point2f[] {
                new Point2f(50, 50),
                new Point2f(50, 100),
                new Point2f(100, 100),
                new Point2f(100, 50),
            };

            // srcで指定した4点が、出力画像中で位置する座標
            var dstPoints = new Point2f[] {
                new Point2f(50, 50),
                new Point2f(55, 105),
                new Point2f(100, 100),
                new Point2f(100, 50),
            };

            using (var matrix = Cv2.GetPerspectiveTransform(srcPoints, dstPoints))
            using (var dst = new Mat(new Size(640, 480), MatType.CV_8UC3))
            {
                // 透視変換
                Cv2.WarpPerspective(src, dst, matrix, dst.Size());
                using (new Window("result", dst))
                {
                    Cv2.WaitKey();
                }
            }
        }
        public static List<Point2f> ToOrigin(List<Point2f> points, int x, int y)
        {
            return points.Select(p => new Point2f(p.X - x, p.Y - y)).ToList();
        }
        public static Mat Distort(Mat srcImg, List<List<Point2f>> srcMesh, List<List<Point2f>> dstMesh)
        {


            var canvas = new Mat(srcImg.Size(), srcImg.Type(), 0);
            foreach (var n in Enumerable.Range(0, srcMesh.Count))
            {
                var roiRect = MatFunctions.GetBoundingBox(srcMesh[n]);
                var roi = MatFunctions.RoiToMat(srcImg, roiRect);//new Mat(srcImg, roiRect);
                var srcShape = ToOrigin(srcMesh[n], roiRect.X, roiRect.Y);
                if (srcMesh[n].SequenceEqual(dstMesh[n]))
                {
                    roi.CopyTo(new Mat(canvas, roiRect), roi);
                    continue;
                }
                var dstRoiRect = MatFunctions.GetBoundingBox(dstMesh[n]);
                dstRoiRect = MatFunctions.ShiftIfRectOverSize(canvas.Size(), dstRoiRect);
                var dstShape = ToOrigin(dstMesh[n], dstRoiRect.X, dstRoiRect.Y);
                var transform = Cv2.GetPerspectiveTransform(srcShape, dstShape);
                //Trace.WriteLine($"src:{string.Join(", ", srcMesh[n].Select(m => $"({m.X}, {m.Y})"))}");
                //Trace.WriteLine($"dst:{string.Join(", ", dstMesh[n].Select(m => $"({m.X}, {m.Y})"))}");
                var tmp = new Mat();
                Cv2.WarpPerspective(roi, tmp, transform, dstRoiRect.Size, InterpolationFlags.Cubic, BorderTypes.Replicate);
                var mask = new Mat(dstRoiRect.Size, MatType.CV_8UC1, 0);
                var x = dstShape.Select(p => new Point(p.X, p.Y)).ToArray();
                Cv2.FillConvexPoly(mask, x, 255);
                //MatFunctions.ShowImage(mask);
                //MatFunctions.ShowImage(tmp);
                tmp.CopyTo(new Mat(canvas, dstRoiRect), mask);//.Erode(new Mat(3,3,MatType.CV_8UC1,1)));
                //MatFunctions.ShowImage(canvas);
                tmp.Dispose();
                transform.Dispose();
            }
            return canvas;
        }
    }
    public class GaussianDistortion
    {
        /*
        This class performs randomised, elastic gaussian distortions on images.

        def __init__(self, probability, grid_width, grid_height, magnitude, corner, method, mex, mey, sdx, sdy)){

            As well as the probability, the granularity of the distortions
            produced by this class can be controlled using the width and
            height of the overlaying distortion grid.The larger the height
            && width of the grid, the smaller the distortions. This means
            that larger grid sizes can result in finer, less severe distortions.
            As well as this, the magnitude of the distortions vectors can
            also be adjusted.
            :param probability: Controls the probability that the operation is
             performed when it is invoked in the pipeline.
            :param grid_width: The width of the gird overlay, which is used
             by the class to apply the transformations to the image.
            :param grid_height: The height of the gird overlay, which is used
             by the class to apply the transformations to the image.
            :param magnitude: Controls the degree to which each distortion is
             applied to the overlaying distortion grid.
            :param corner: which corner of picture to distort.
             Possible values: "bell"(circular surface applied), "ul"(upper left),
             "ur"(upper right), "dl"(down left), "dr"(down right).
            :param method: possible values: "in"(apply max magnitude to the chosen
             corner), "out"(inverse of method in).
            :param mex: used to generate 3d surface for similar distortions.
             Surface is based on normal distribution.
            :param mey: used to generate 3d surface for similar distortions.
             Surface is based on normal distribution.
            :param sdx: used to generate 3d surface for similar distortions.
             Surface is based on normal distribution.
            :param sdy: used to generate 3d surface for similar distortions.
             Surface is based on normal distribution.
            :type probability: Float
            :type grid_width: Integer
            :type grid_height: Integer
            :type magnitude: Integer
            :type corner: String
            :type method: String
            :type mex: Float
            :type mey: Float
            :type sdx: Float
            :type sdy: Float
            For values :attr:`mex`, :attr:`mey`, :attr:`sdx`, && :attr:`sdy` the
            surface is based on the normal distribution:
            .. math::
             e^{ - \Big( \\frac{ (x -\\text{ mex})^2} {\\text{ sdx} } + \\frac{ (y -\\text{ mey})^2} {\\text{ sdy} } \Big) }
            */
        enum CornerOp { bell, ul, ur, dl, dr };
        enum MethodOp { IN, OUT };
        int grid_width = 5;
        int grid_height = 5;
        int magnitude = 1;
        int randomise;
        string corner = "ur";
        string method = "out";
        int mex;
        int mey;
        int sdx;
        int sdy;
        List<(int, int, int, int)> polygon_indices = new List<(int, int, int, int)>();
        Random rnd = new Random();
        public static Mat Distort(Mat srcImg, List<(int, int, int, int)> srcMesh, List<(int, int, int, int, int, int, int, int)> dstMesh)
        {
            List<List<Point2f>> ToPoints(IEnumerable<(int, int, int, int, int, int, int, int)> points)
            {
                return points.Select(p => new List<Point2f>() {
                    new Point2f(p.Item1, p.Item2),
                    new Point2f(p.Item3, p.Item4),
                    new Point2f(p.Item5, p.Item6),
                    new Point2f(p.Item7, p.Item8) }).ToList();
            }
            var src = ToPoints(srcMesh.Select(m => (m.Item1, m.Item2, m.Item1, m.Item4, m.Item3, m.Item4, m.Item3, m.Item2)));
            var dst = ToPoints(dstMesh);
            return Distort(srcImg, src, dst);
        }
        public static Mat Distort(Mat srcImg, List<List<Point2f>> srcMesh, List<List<Point2f>> dstMesh)
        {
            var canvas = new Mat(srcImg.Size(), srcImg.Type(), 0);
            foreach (var n in Enumerable.Range(0, srcMesh.Count))
            {
                var roiRect = MatFunctions.GetBoundingBox(srcMesh[n]);
                var roi = new Mat(srcImg, roiRect);
                if (srcMesh[n].SequenceEqual(dstMesh[n]))
                {
                    roi.CopyTo(new Mat(canvas, roiRect), roi);
                    continue;
                }
                var dstRoiRect = MatFunctions.GetBoundingBox(dstMesh[n]);
                var transform = Cv2.GetPerspectiveTransform(srcMesh[n], dstMesh[n]);
                var tmp = new Mat();
                Cv2.WarpPerspective(roi, tmp, transform, dstRoiRect.Size, InterpolationFlags.Cubic, BorderTypes.Constant, 0);
                tmp.CopyTo(new Mat(canvas, dstRoiRect), tmp);
            }
            return canvas;
        }

        public GaussianDistortion()
        {
        }
        /// <summary>
        /// 標準正規分布に従う乱数
        /// </summary>
        /// <returns>N(0,1)に従う乱数</returns>
        public double GetNormRandom()
        {
            double dR1 = Math.Abs(rnd.NextDouble() * 2 - 1);
            double dR2 = Math.Abs(rnd.NextDouble() * 2 - 1);
            return (Math.Sqrt(-2 * Math.Log(dR1, Math.E)) * Math.Cos(2 * Math.PI * dR2));
        }

        /// <summary>
        /// 正規分布(μ・σ)に従う乱数
        /// </summary>
        /// <param name="m">平均:μ</param>
        /// <param name="s">標準偏差:σ</param>
        /// <returns>N(m,s)に従う乱数</returns>
        public double GetNormRandom(double m, double s)
        {
            return (m + s * GetNormRandom());
        }
        public Mat perform_operation(Mat image)
        {
            /*
            Distorts the passed image(s) according to the parameters supplied
            during instantiation, returning the newly distorted image.
            :param images: The image(s) to be distorted.
            :type images: List containing PIL.Image object(s).
            :return: The transformed image(s) as a list of object(s) of type
             PIL.Image.
            */
            (var w, var h) = (image.Width, image.Height);

            var horizontal_tiles = this.grid_width;
            var vertical_tiles = this.grid_height;

            var width_of_square = (int)Math.Floor(w / (float)horizontal_tiles);
            var height_of_square = (int)Math.Floor(h / (float)vertical_tiles);

            var width_of_last_square = w - (width_of_square * (horizontal_tiles - 1));
            var height_of_last_square = h - (height_of_square * (vertical_tiles - 1));
            var dimensions = new List<(int x1, int y1, int x2, int y2)>();

            for (int vertical_tile = 0; vertical_tile < vertical_tiles; vertical_tile++)
            {
                for (int horizontal_tile = 0; horizontal_tile < horizontal_tiles; horizontal_tile++)
                {
                    if (vertical_tile == (vertical_tiles - 1) && horizontal_tile == (horizontal_tiles - 1))
                    {
                        dimensions.Add((horizontal_tile * width_of_square,
                                       vertical_tile * height_of_square,
                                       width_of_last_square + (horizontal_tile * width_of_square),
                                       height_of_last_square + (height_of_square * vertical_tile)));
                    }
                    else if (vertical_tile == (vertical_tiles - 1))
                    {
                        dimensions.Add((horizontal_tile * width_of_square,
                                                   vertical_tile * height_of_square,
                                                   width_of_square + (horizontal_tile * width_of_square),
                                                   height_of_last_square + (height_of_square * vertical_tile)));
                    }
                    else if (horizontal_tile == (horizontal_tiles - 1))
                    {
                        dimensions.Add((horizontal_tile * width_of_square,
                                           vertical_tile * height_of_square,
                                           width_of_last_square + (horizontal_tile * width_of_square),
                                           height_of_square + (height_of_square * vertical_tile)));
                    }
                    else
                    {
                        dimensions.Add((horizontal_tile * width_of_square,
                                           vertical_tile * height_of_square,
                                           width_of_square + (horizontal_tile * width_of_square),
                                           height_of_square + (height_of_square * vertical_tile)));
                    }
                }
            }

            var last_column = new List<dynamic>();
            for (int i = 0; i < vertical_tiles; i++)
            {
                last_column.Add((horizontal_tiles - 1) + horizontal_tiles * i);
            }

            var last_row = Enumerable.Range((horizontal_tiles * vertical_tiles) - horizontal_tiles, horizontal_tiles * vertical_tiles);

            var polygons = new List<(int, int, int, int, int, int, int, int)>();
            foreach (var dim in dimensions) polygons.Add((dim.x1, dim.y1, dim.x1, dim.y2, dim.x2, dim.y2, dim.x2, dim.y1));


            foreach (int i in Enumerable.Range(0, (vertical_tiles * horizontal_tiles) - 1))
            {
                if (!last_row.Contains(i) && !last_column.Contains(i))
                    polygon_indices.Add((i, i + 1, i + horizontal_tiles, i + 1 + horizontal_tiles));
            }

            double sigmoidf(double x, double y, double sdx = 0.05, double sdy = 0.05, double mex = 0.5, double mey = 0.5, int constant = 1)
            {
                double sigmoid(double x1, double y1) => (constant * Math.Exp(-(Math.Pow(x1 - mex, 2) / sdx + Math.Pow(y1 - mey, 2) / sdy)) + Math.Max(0, -constant) - Math.Max(0, constant));
                double[] linspace(double min, double max, int n)//等間隔でn個の数列を返す
                {
                    var r = new double[n];
                    foreach (var i in Enumerable.Range(0, n)) r[i] = (min + (max - min) / n * i);
                    return r;
                }

                var xl = linspace(0, 1, 50);
                var yl = linspace(0, 1, 50);
                var X = new Mat();
                var Y = new Mat();

                Cv2.Repeat(new Mat<double>(1, xl.Length, xl), yl.Length, 1, X);
                Cv2.Repeat(new Mat<double>(yl.Length, 1, yl), 1, xl.Length, Y);
                var Zs = new List<double>();
                var XArray = new Mat<double>(X).ToArray();
                var YArray = new Mat<double>(Y).ToArray();
                foreach (var i in Enumerable.Range(0, XArray.Length))
                {
                    Zs.Add(sigmoid(XArray[i], YArray[i]));
                }
                var mino = Zs.Min();
                var maxo = Zs.Max();
                var res = sigmoid(x, y);
                res = Math.Max(((((res - mino) * (1 - 0)) / (maxo - mino)) + 0), 0.01) * this.magnitude;
                return res;
            }
            double corner(double x, double y, string corner_str = "ul", string method = "out", double sdx = 0.05, double sdy = 0.05, double mex = 0.5, double mey = 0.5)
            {
                var tu = (x: 0, y: 0);
                var ll = new Dictionary<string, double[]>() {
                    { "dr", new []{0, 0.5, 0, 0.5 } },
                    { "dl", new []{0.5, 1, 0, 0.5 } },
                    {"ur", new []{0, 0.5, 0.5, 1 } },
                    {"ul", new []{0.5, 1, 0.5, 1 } },
                    { "bell", new double[]{0, 1, 0, 1 } }};
                var new_c = ll[corner_str];
                var new_x = (((x - 0) * (new_c[1] - new_c[0])) / (1 - 0)) + new_c[0];
                var new_y = (((y - 0) * (new_c[3] - new_c[2])) / (1 - 0)) + new_c[2];
                var constant = (method == "in") ? 1 : -1;
                var res = sigmoidf(x = new_x, y = new_y, sdx, sdy, mex, mey, constant);

                return res;
            }

            Mat process(Mat img)
            {

                foreach ((var a, var b, var c, var d) in polygon_indices)
                {
                    (var x1, var y1, var x2, var y2, var x3, var y3, var x4, var y4) = polygons[a];

                    var sigmax = corner((double)x3 / w, (double)y3 / h);//, this.corner, this.method, this.sdx, this.sdy, this.mex, this.mey);
                    var dx = this.GetNormRandom(0, sigmax);
                    var dy = this.GetNormRandom(0, sigmax);

                    polygons[a] = (x1, y1,
                                   x2, y2,
                                   x3 + (int)(dx + 0.5), y3 + (int)(dy + 0.5),
                                   x4, y4);

                    (x1, y1, x2, y2, x3, y3, x4, y4) = polygons[b];
                    polygons[b] = (x1, y1,
                                   x2 + (int)(dx + 0.5), y2 + (int)(dy + 0.5),
                                   x3, y3,
                                   x4, y4);

                    (x1, y1, x2, y2, x3, y3, x4, y4) = polygons[c];
                    polygons[c] = (x1, y1,
                                   x2, y2,
                                   x3, y3,
                                   x4 + (int)(dx + 0.5), y4 + (int)(dy + 0.5));

                    (x1, y1, x2, y2, x3, y3, x4, y4) = polygons[d];
                    polygons[d] = (x1 + (int)(dx + 0.5), y1 + (int)(dy + 0.5),
                                   x2, y2,
                                   x3, y3,
                                   x4, y4);
                }

                var generated_mesh = new List<dynamic>();
                foreach (var i in Enumerable.Range(0, dimensions.Count))
                {
                    generated_mesh.Add((dimensions[i], polygons[i]));
                }
                return Distort(img, dimensions, polygons);
                //return image.transform(image.size, Image.MESH, generated_mesh, resample = Image.BICUBIC);
            }

            //var augmented_images = new List<dynamic>();

            //    foreach(var image in images):
            //        augmented_images.Add(process (image));
            return process(image);
        }
    }
}
