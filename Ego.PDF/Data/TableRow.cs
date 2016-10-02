using Ego.PDF;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ego.PDF.Data
{
    public class TableRow
    {
        public List<TableCell> Cells { get; set; } = new List<TableCell>();
        public double CellHeight { get; set; } = 6;

        public double MaxHeight;
        public string Border { get; set; }

        public TableRow()
        {

        }

        public TableRow(params double[] widths) : this()
        {
            foreach (var width in widths)
            {
                this.Cells.Add(new TableCell() { Width = width });
            }
        }

        public void NormalizeWidths(double width)
        {
            var sum = Cells.Sum(x => x.Width);
            if (width > 0)
            {
                var correction = width / sum;
                foreach (var tableCell in Cells)
                {
                    tableCell.Width = tableCell.Width / (sum / width);
                }
            }
        }

        public TableRow SerBorder(string border = "None")
        {
            foreach (var tableCell in this.Cells)
            {
                tableCell.Border = border;
            }
            return this;
        }

        public TableRow SetCellHeight(double height)
        {
            this.CellHeight = height;
            return this;
        }
    }

    public static class FpdExtender
    {
        public static FPdf PaintRow(this FPdf pdf, TableRow row, params string[] texts)
        {
            var maxCol = Math.Min(texts.Length, row.Cells.Count);
            var maxHeight = row.MaxHeight;

            if (!texts.Any())
            {
                maxHeight = pdf.CellMeasure(pdf.CurrentPageSize.Width, row.CellHeight, " ");
            }

            for (var pos = 0; pos < maxCol; pos++)
            {
                var col = row.Cells[pos];
                var height = pdf.CellMeasure(col.Width, row.CellHeight, texts[pos]);
                if (maxHeight < height) maxHeight = height;
            }

            for (var pos = 0; pos < maxCol; pos++)
            {
                var col = row.Cells[pos];
                if (col.Fill) pdf.SetFillColor(col.Background);
                pdf.BoxedText(col.Width, row.CellHeight, maxHeight, texts[pos], row.Border ?? col.Border, 0, col.Align, col.Fill);
            }

            for (var pos = maxCol; pos < row.Cells.Count; pos++)
            {
                var col = row.Cells[pos];
                if (col.Fill) pdf.SetFillColor(col.Background);
                pdf.BoxedText(col.Width, row.CellHeight, maxHeight, " ", row.Border ?? col.Border, 0, col.Align, col.Fill);
            }

            pdf.Ln();
            return pdf;
        }
    }




    public class TableCell
    {
        public string Border { get; set; } = "1";
        public double Width { get; set; }
        public AlignEnum Align { get; set; } = AlignEnum.Left;
        public Color Background { get; set; } = Color.Transparent;
        public bool Fill => Background != Color.Empty && Background != Color.Transparent;

        public TableCell()
        {

        }

        public TableCell(double width) : this()
        {
            this.Width = width;
        }
    }
}
