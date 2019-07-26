using System;
using System.Linq;

namespace Ego.PDF.Data
{
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
                if (col.Bold && !pdf.FontStyle.Contains("B"))
                {
                    oldStyle = pdf.FontStyle;
                    pdf.SetFont("", pdf.FontStyle + "B");
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
                if (col.Bold && !pdf.FontStyle.Contains("B"))
                {
                    oldStyle = pdf.FontStyle;
                    pdf.SetFont("", pdf.FontStyle + "B");
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
                if (col.Bold && !pdf.FontStyle.Contains("B"))
                {
                    oldStyle = pdf.FontStyle;
                    pdf.SetFont("", pdf.FontStyle + "B");
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
                if (col.Bold && !pdf.FontStyle.Contains("B"))
                {
                    oldStyle = pdf.FontStyle;
                    pdf.SetFont("", pdf.FontStyle + "B");
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
}
