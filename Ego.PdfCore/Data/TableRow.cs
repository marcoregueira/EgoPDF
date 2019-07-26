using Ego.PDF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

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

        public TableRow NormalizeWidths(double width)
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
            return this;
        }

        public TableRow SetBold(bool bold = true)
        {
            foreach (var tableCell in this.Cells)
            {
                tableCell.Bold = bold;
            }
            return this;
        }

        public TableRow SetBorder(string border = "None")
        {
            foreach (var tableCell in this.Cells)
            {
                tableCell.Border = border;
            }
            return this;
        }

        public TableRow SetAlign(AlignEnum align)
        {
            foreach (var tableCell in this.Cells)
            {
                tableCell.Align = align;
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
        public static TableRow PrintRow2(this TableRow row, FPdf pdf, params Func<TableCell, string>[] texts)
        {
            pdf.PrintRow2(row, texts);
            return row;
        }

        public static FPdf PrintRow2(this FPdf pdf, TableRow row, params Func<TableCell, string>[] texts)
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
                col.Status = StatusEnum.Measure;
                var text = texts[pos]?.Invoke(col);

                string oldStyle = null;
                if (col.Bold && !pdf.FontStyle.Contains("b"))
                {
                    oldStyle = pdf.FontStyle;
                    pdf.SetFont("", pdf.FontStyle + "b");
                }

                var height = pdf.CellMeasure(col.Width, col.CellHeight ?? row.CellHeight, text);
                if (oldStyle != null)
                {
                    pdf.SetFont("", oldStyle);
                }

                if (maxHeight < height) maxHeight = height;
            }

            for (var pos = 0; pos < maxCol; pos++)
            {
                var col = row.Cells[pos];
                col.Status = StatusEnum.Draw;
                if (col.Fill) pdf.SetFillColor(col.Background);
                pdf.SavePos();
                var text = texts[pos]?.Invoke(col);
                pdf.GoBack();
                string oldStyle = null;
                if (col.Bold && !pdf.FontStyle.Contains("b"))
                {
                    oldStyle = pdf.FontStyle;
                    pdf.SetFont("", pdf.FontStyle + "b");
                }
                pdf.BoxedText(col.Width, col.CellHeight ?? row.CellHeight, maxHeight, text, row.Border ?? col.Border, 0, col.Align, col.Fill);
                if (oldStyle != null)
                {
                    pdf.SetFont("", oldStyle);
                }
            }

            for (var pos = maxCol; pos < row.Cells.Count; pos++)
            {
                var col = row.Cells[pos];
                if (col.Fill) pdf.SetFillColor(col.Background);
                pdf.BoxedText(col.Width, col.CellHeight ?? row.CellHeight, maxHeight, " ", row.Border ?? col.Border, 0, col.Align, col.Fill);
            }

            pdf.Ln();
            return pdf;
        }

        public static FPdf PrintRow(this FPdf pdf, TableRow row, params string[] texts)
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
                string oldStyle = null;
                if (col.Bold && !pdf.FontStyle.Contains("b"))
                {
                    oldStyle = pdf.FontStyle;
                    pdf.SetFont("", pdf.FontStyle + "b");
                }
                var height = pdf.CellMeasure(col.Width, row.CellHeight, texts[pos]);
                if (maxHeight < height) maxHeight = height;
                if (oldStyle != null)
                {
                    pdf.SetFont("", oldStyle);
                }
            }

            for (var pos = 0; pos < maxCol; pos++)
            {
                var col = row.Cells[pos];
                string oldStyle = null;
                if (col.Bold && !pdf.FontStyle.Contains("b"))
                {
                    oldStyle = pdf.FontStyle;
                    pdf.SetFont("", pdf.FontStyle + "b");
                }
                if (col.Fill) pdf.SetFillColor(col.Background);
                pdf.BoxedText(col.Width, row.CellHeight, maxHeight, texts[pos], row.Border ?? col.Border, 0, col.Align, col.Fill);
                if (oldStyle != null)
                {
                    pdf.SetFont("", oldStyle);
                }
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
        public bool Fill => Background != Color.Transparent && Background != Color.TransparentBlack;
        public double? CellHeight { get; set; }
        public StatusEnum Status { get; set; }
        public bool Bold { get; set; }

        public TableCell()
        {

        }

        public TableCell(double width) : this()
        {
            this.Width = width;
        }
    }

    public enum StatusEnum
    {
        Measure,
        Draw
    }
}
