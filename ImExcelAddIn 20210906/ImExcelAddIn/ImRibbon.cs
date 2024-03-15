using Microsoft.Office.Tools.Ribbon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Office.Interop.Excel;

namespace ImExcelAddIn
{
    public partial class ImRibbon
    {
        private void ImRibbon_Load(object sender, RibbonUIEventArgs e)
        {

        }

        private void btnInsertImage_Click(object sender, RibbonControlEventArgs e)
        {
            float imageZoom;
            float.TryParse(txtImageZoom.Text, out imageZoom);
            imageZoom = Math.Max(imageZoom, 0.0F);
            imageZoom = Math.Min(imageZoom, 8F);
            imageZoom = imageZoom == 0.0F ? 1.0F : imageZoom;
            txtImageZoom.Text = imageZoom.ToString();

            Workbook activeBook = Globals.ThisAddIn.Application.ActiveWorkbook;
            Worksheet activeSheet = Globals.ThisAddIn.Application.ActiveSheet;
            //Range activeCell = Globals.ThisAddIn.Application.ActiveCell;
            Range selectedRange = Globals.ThisAddIn.Application.Selection;
            activeSheet.Cells.VerticalAlignment = Microsoft.Office.Interop.Excel.Constants.xlCenter;

            int imageColumn = 0;
            if (IsLetter(txtDstColumn.Text))
            {
                // ColumnLetterToDigit
                int digit = ColumnLetterToDigit(txtDstColumn.Text);
                if (digit <= 16384)
                {
                    imageColumn = digit;
                }
                else
                {
                    txtDstColumn.Text = "1";
                }
            }
            if (IsNumeric(txtDstColumn.Text))
            {
                int columnOffset;
                Int32.TryParse(txtDstColumn.Text, out columnOffset);
                columnOffset = Math.Max(columnOffset, 1);
                txtDstColumn.Text = columnOffset.ToString();

                Range tmpRange = activeSheet.UsedRange;
                int columnMax = 0;
                foreach (var tc in tmpRange)
                {
                    Range cell = (Range)tc;
                    columnMax = Math.Max(columnMax, cell.Column);
                }
                //int imageColumn = activeSheet.UsedRange.End[XlDirection.xlToRight].Column + 1;
                imageColumn = columnMax + columnOffset;
            }

            Range imageHeaderCell = activeSheet.Cells[1, imageColumn];
            float testWidth = 20;
            imageHeaderCell.ColumnWidth = testWidth;
            System.Threading.Thread.Sleep(300);
            float cWidth = (float)imageHeaderCell.Width / testWidth;

            string imageHeaderStr = imageHeaderCell.Value2 == null ? "" : imageHeaderCell.Value2.ToString();
            string dirName = System.IO.Directory.Exists(imageHeaderStr) ? imageHeaderStr : ""; ;

            //      Stack<Tuple<Range, string>> cells = new Stack<Tuple<Range, string>>();
            List<Tuple<Range, string>> cells = new List<Tuple<Range, string>>();
            int errCnt = 0;
            int errMax = 10000;
            foreach (var tc in selectedRange.Cells)
            {
                Range cell = (Range)tc;
                string value2 = cell.Value2 == null ? "" : cell.Value2.ToString();
                if (value2 == "")
                {
                    errCnt++;
                    if (errCnt > errMax)
                    {
                        break;
                    }
                    continue;
                }
                if (cell.RowHeight == 0)
                {
                    continue;
                }

                if (System.IO.Directory.Exists(value2))
                {
                    dirName = value2;
                    errCnt = 0;
                    continue;
                }
                string fileName = System.IO.Path.Combine(dirName, value2);
                if (System.IO.File.Exists(fileName) == false)
                {
                    continue;
                }
                //  cells.Push(new Tuple<Range, string>(cell, fileName));
                cells.Add(new Tuple<Range, string>(cell, fileName));
                errCnt = 0;
            }
            float maxImageWidth = 0;
            float margin = 5.0F;

            while (cells.Count != 0)
            {
                //Tuple<Range, string> obj = cells.Pop();
                Tuple<Range, string> obj = cells[0];
                cells.RemoveAt(0);

                Range cell = (Range)obj.Item1;
                string fileName = (string)obj.Item2;

                float imageWidth = 0;
                float imageHeight = 0;
                using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(fileName))
                {
                    if (cbRotation.Checked == false)
                    {
                        imageWidth = bitmap.Width;
                        imageHeight = bitmap.Height;
                    }
                    else
                    {
                        imageWidth = bitmap.Height;
                        imageHeight = bitmap.Width;
                    }
                }
                int row = cell.Row;
                var ImageCell = activeSheet.Cells[row, imageColumn];
                ImageCell.RowHeight = imageHeight * imageZoom * 0.75F + margin * 2.0F;

                maxImageWidth = Math.Max(imageWidth, maxImageWidth);

                // Microsoft.Office.Interop.Excel.Shape shape = activeSheet.Shapes.AddPicture(fileName,
                //     Microsoft.Office.Core.MsoTriState.msoFalse,
                //     Microsoft.Office.Core.MsoTriState.msoTrue,
                //     (float)ImageCell.Left + margin, (float)ImageCell.Top + margin, -1, -1);
                Microsoft.Office.Interop.Excel.Shape shape = activeSheet.Shapes.AddPicture2(fileName,
                                 Microsoft.Office.Core.MsoTriState.msoFalse,
                                 Microsoft.Office.Core.MsoTriState.msoTrue,
                                 (float)ImageCell.Left + margin, (float)ImageCell.Top + margin, -1, -1,
                                 Microsoft.Office.Core.MsoPictureCompress.msoPictureCompressTrue);


                //shape.Placement = Microsoft.Office.Interop.Excel.XlPlacement.xlMoveAndSize;
                shape.Placement = Microsoft.Office.Interop.Excel.XlPlacement.xlMove;

                shape.ScaleHeight(imageZoom, Microsoft.Office.Core.MsoTriState.msoTrue);
                shape.ScaleWidth(imageZoom, Microsoft.Office.Core.MsoTriState.msoTrue);
            
                if (cbRotation.Checked)
                {
                    float w = (float)(imageWidth * imageZoom * 0.75F);
                float h = (float)(imageHeight * imageZoom * 0.75F);
                shape.Left += (w - h) / 2;
                shape.Top += (h - w) / 2;
               
                    shape.Rotation = 90F;
                }
            }
            float targetWidth = (float)(maxImageWidth * imageZoom * 0.75F + margin * 2.0F);
            int retryCount = 0;
            float retryOffset = 0.5F;
            do
            {
                float tmpWidth = targetWidth / cWidth + retryOffset * retryCount;
                if (tmpWidth > 255.0F)
                {
                    activeSheet.Columns[imageColumn].ColumnWidth = 255;
                    break;
                }
                Range r = activeSheet.Columns[imageColumn];

                activeSheet.Columns[imageColumn].ColumnWidth = tmpWidth;
                var tt = r.ColumnWidth;
                var t2 = r.Width;

                retryCount++;
            } while (targetWidth > (float)activeSheet.Columns[imageColumn].Width);         
        }

        private void btnRayout_Click(object sender, RibbonControlEventArgs e)
        {
            Worksheet activeSheet = Globals.ThisAddIn.Application.ActiveSheet;
            SortedDictionary<double, Microsoft.Office.Interop.Excel.Shape> shapeDict = new SortedDictionary<double, Microsoft.Office.Interop.Excel.Shape>();

            foreach (Microsoft.Office.Interop.Excel.Shape shape in activeSheet.Shapes)
            {
                double top = (double)shape.Top;
                while (shapeDict.ContainsKey(top))   //暫定だけど多分大丈夫
                {
                    top += 0.00001F;
                }

                shapeDict.Add(top, shape);
            }
            foreach (Microsoft.Office.Interop.Excel.Shape shape in shapeDict.Values)
            {
                shape.ZOrder(Microsoft.Office.Core.MsoZOrderCmd.msoBringToFront);
            }
        }
        public static bool IsNumeric(string src)
        {
            return src.All(char.IsDigit);
        }
        public bool IsLetter(string src)
        {
            return src.All(char.IsLetter);
        }

        int ColumnLetterToDigit(string src)
        {
            if (IsLetter(src) == false)
            {
                return 0;
            }
            int value = 0;
            src = src.ToLower();
            for (int i = 0; i < src.Length; i++)
            {
                int c = src[src.Length - 1 - i];
                c = c - 0x60;
                double d = Math.Pow(26.0, (double)i);
                value += c * (int)d;
            }
            return value;
        }
    }
}