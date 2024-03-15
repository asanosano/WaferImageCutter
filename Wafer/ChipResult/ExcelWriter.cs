using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ClosedXML.Excel;

namespace Wafer.ChipResult
{
    public class ExcelWriter
    {
        public static void WriteWaferResult(string filePath, List<string> csvContents)
        {
            List<string[]> dataList = new List<string[]>();
            foreach (var str in csvContents)
            {
                dataList.Add(str.Split(',').Select(s => s.Trim()).ToArray());
            }
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Judged");
            int row = 0;
            void SetTxt(int r, int col)
            {
                var cell = worksheet.Cell(r + 1, col + 1);
                cell.Value = dataList[r][col];
                cell.DataType = XLDataType.Text;
                cell.Style.NumberFormat.Format = "@";
            }
            void SetNum(int r, int col, string format)
            {
                var val = dataList[r][col] == "" ? "0" : dataList[r][col];//数値型のときは空欄の代わりに0をセット

                var cell = worksheet.Cell(r + 1, col + 1);
                cell.Value = dataList[r][col];
                cell.DataType = XLDataType.Text;
                cell.Style.NumberFormat.Format = "@";
            }

        }
        public static void WriteIjhResult(string filePath, List<string> csvContents)
        {
            List<string[]> dataList = new List<string[]>();
            foreach (var str in csvContents)
            {
                dataList.Add(str.Split(',').Select(s => s.Trim()).ToArray());
            }
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Judged");
            int row = 0;
            void SetTxt(int r, int col) => worksheet.Cell(r + 1, col + 1)
                                                                    .SetValue(dataList[r][col])
                                                                    .SetDataType(XLDataType.Text)
                                                                    .Style.NumberFormat.SetFormat("@");
            void SetNum(int r, int col, string format)
            {
                var val = dataList[r][col] == "" ? "0" : dataList[r][col];//数値型のときは空欄の代わりに0をセット
                worksheet.Cell(r + 1, col + 1)
                                .SetValue(val)
                                .SetDataType(XLDataType.Number)
                                .Style.NumberFormat.SetFormat(format);
            }
            void SetDateTime(int r, int col) => worksheet.Cell(r + 1, col + 1)
                                                                            .SetValue(DateTime.ParseExact(dataList[row][col], "yyyyMMddHHmmss", null))
                                                                            .SetDataType(XLDataType.DateTime)
                                                                            .Style.NumberFormat.SetFormat("yyyymmddhhmmss");
            //basepointのラベル
            for (int col = 0; col < 2; col++) SetTxt(row, col);
            row++;
            //basepointの数値
            for (int col = 0; col < 2; col++) SetNum(row, col, "0");
            row++;
            //ヘッダ
            for (int col = 0; col < dataList[row].Length; col++) SetTxt(row, col);
            row++;
            for (; row < dataList.Count; row++)
            {
                int col = 0;
                //DateTime
                SetDateTime(row, col++);
                //LotNo
                SetTxt(row, col++);
                //TrayID
                SetTxt(row, col++);
                //A
                SetTxt(row, col++);
                //C
                SetTxt(row, col++);
                //M
                SetTxt(row, col++);
                //N
                SetTxt(row, col++);
                //Area
                SetNum(row, col++, "0");
                //Std
                SetTxt(row, col++);
                //Cno
                SetNum(row, col++, "0");
                //CSX
                SetNum(row, col++, "0");
                //CSY
                SetNum(row, col++, "0");
                //CWX
                SetNum(row, col++, "0");
                //CWY
                SetNum(row, col++, "0");
                //SSX
                SetNum(row, col++, "0");
                //SSY
                SetNum(row, col++, "0");
                //SWX
                SetNum(row, col++, "0");
                //SWY
                SetNum(row, col++, "0");
                //Method
                SetNum(row, col++, "0");
                //DefectID
                SetNum(row, col++, "0");
                //DScore1
                SetNum(row, col++, "0.000");
                //DScore2
                SetNum(row, col++, "0.00");
                //DJudge
                SetTxt(row, col++);
                //DCmt
                SetTxt(row, col++);
            }
            worksheet.ColumnsUsed().AdjustToContents();
            workbook.SaveAs(filePath);
        }
        public static void WriteCsvToXlsx(string filePath, List<string> csvContents)
        {
            List<string[]> dataList = new List<string[]>();
            foreach (var str in csvContents)
            {
                dataList.Add(str.Split(',').Select(s => s.Trim()).ToArray());
            }
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Judged");
            XLDataType GetDataType(string str)
            {
                const int lengthLimit = 9;//Excelの仕様で12桁以上は指数表示になり、16桁以上は値が失われる
                var isNumerable = str.Length == 0//空欄は数値扱い（仮）
                                            || double.TryParse(str, out var dummy)
                                                    && (str.Length == 1 || !str.StartsWith("0") || str.StartsWith("0."));
                if (isNumerable && str.Length < lengthLimit) return XLDataType.Number;
                else if (isNumerable && str.Length == 14) return XLDataType.DateTime;//14桁の数値のみDateTimeに変換（汎用性×）
                else return XLDataType.Text;
            }
            string dateTimeFormat = "yyyyMMddHHmmss";
            for (int row = 0; row < dataList.Count; row++)
            {
                for (int col = 0; col < dataList[row].Length; col++)
                {
                    var dataType = GetDataType(dataList[row][col]);
                    switch (dataType)
                    {
                        case XLDataType.Text:
                        case XLDataType.Number:
                            worksheet.Cell(row + 1, col + 1)
                                           .SetValue(dataList[row][col])
                                           .SetDataType(dataType);
                            break;
                        case XLDataType.DateTime:
                            var dbg = DateTime.ParseExact(dataList[row][col], dateTimeFormat, null);
                            worksheet.Cell(row + 1, col + 1)
                                            .SetValue(DateTime.ParseExact(dataList[row][col], dateTimeFormat, null))
                                            .SetDataType(XLDataType.DateTime)
                                            .Style.DateFormat.SetFormat("yyyymmddhhmmss");
                            break;
                        case XLDataType.Boolean:
                        case XLDataType.TimeSpan:
                        default:
                            throw new NotImplementedException("未実装です");
                            break;
                    }
                }
            }
            worksheet.ColumnsUsed().AdjustToContents();
            workbook.SaveAs(filePath);
        }
        public static bool WriteFuncSample(string fileName, List<string> stringList)
        {
            List<string[]> dataList = new List<string[]>();
            foreach (var str in stringList)
            {
                dataList.Add(str.Split(','));
            }
            var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Judged");

            // basePointX, basePointY
            worksheet.Cell(1, 1)
                .SetValue(dataList[0][0])
                .SetDataType(XLDataType.Text);
            worksheet.Cell(1, 2)
                .SetValue(dataList[0][1])
                .SetDataType(XLDataType.Text);

            worksheet.Cell(2, 1)
               .SetValue(dataList[1][0])
               .SetDataType(XLDataType.Number);
            worksheet.Cell(2, 2)
                .SetValue(dataList[1][1])
                .SetDataType(XLDataType.Number);


            int c = 1;
            foreach (var str in dataList[2])
            {
                worksheet.Cell(3, c++)
                    .SetValue(str)
                    .SetDataType(XLDataType.Text);
            }
            var dateTimeFormat = "yyyyMMddHHmmss";
            int row = 4;
            for (int dataListIndex = 3; dataListIndex < dataList.Count; dataListIndex++)
            {
                int dataIndex = 0;
                int column = 1;
                //Date
                worksheet.Cell(row, column++)
                    .SetValue(DateTime.ParseExact(dataList[dataListIndex][dataIndex++], dateTimeFormat, null))
                    .SetDataType(XLDataType.DateTime)
                    .Style.DateFormat.SetFormat("yyyymmddhhmmss");
                //lotNo
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Text);
                //TrayID
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Text);
                //Adress
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Text);
                //Kind
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Text);
                //Machine
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Text);
                //ImageNo
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Text);
                //InspArea
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Text);
                //Standard
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Text);
                //CutNo
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Number)
                   .Style.NumberFormat.SetFormat("0");
                //CutSX
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Number)
                   .Style.NumberFormat.SetFormat("0");
                //CutSY
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Number)
                   .Style.NumberFormat.SetFormat("0");
                //CutWX
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Number)
                   .Style.NumberFormat.SetFormat("0");
                //CutWY
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Number)
                   .Style.NumberFormat.SetFormat("0");
                //ScoreSX
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Number)
                   .Style.NumberFormat.SetFormat("0");
                //ScoreSY
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Number)
                   .Style.NumberFormat.SetFormat("0");
                //ScoreWX
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Number)
                   .Style.NumberFormat.SetFormat("0");
                //ScoreWY
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Number)
                   .Style.NumberFormat.SetFormat("0");
                //Method
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Text);
                //DefectID
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Text);
                //Comment
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Text);
                //Score1
                worksheet.Cell(row, column++)
                    .SetValue(dataList[dataListIndex][dataIndex++])
                    .SetDataType(XLDataType.Number)
                    .Style.NumberFormat.SetFormat("0.00");
                //Score2
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Number)
                   .Style.NumberFormat.SetFormat("0.00");
                //Judge
                worksheet.Cell(row, column++)
                   .SetValue(dataList[dataListIndex][dataIndex++])
                   .SetDataType(XLDataType.Text);
                row++;
            }
            worksheet.ColumnsUsed().AdjustToContents();

            workbook.SaveAs(fileName);
            return true;
        }
        public static (Point bp, List<AutoInspectResult> strs) ReadIjhXlsx(string xlsxPath)
        {
            using var wb = new XLWorkbook(xlsxPath);
            var ws = wb.Worksheet(1);
            double bpX = ws.Cell(2, 1).GetDouble();
            double bpY = ws.Cell(2, 2).GetDouble();
            var bp = new Point(bpX, bpY);
            var results = new List<AutoInspectResult>();
            var cols = ws.ColumnsUsed().Count() + 1;
            var rows = ws.RowsUsed().Count() + 1;
            for (int y = 4; y < rows; y++)
            {
                var s = new List<string>();
                for (int x = 1; x < cols; x++)
                {
                    s.Add(ws.Cell(y, x).GetString());
                }
                var csvStr = string.Join(',', s);
                results.Add(new AutoInspectResult(csvStr));
            }
            return (bp, results);
        }
    }
}
