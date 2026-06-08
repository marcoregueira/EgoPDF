using System;
using System.Linq;

namespace Ego.PDF.Data
{
    /// <summary>
    /// FPdf extensions for <see cref="TableRow"/>. Two layers:
    ///
    ///  - <c>Render(pdf, ...)</c> + <c>Measure(pdf, ...)</c> -- the current
    ///    API. Accepts either <c>params string[]</c> (positional, applies
    ///    the column <see cref="CellRole"/>) or
    ///    <c>params Action&lt;TableCell&gt;[]</c> (one lambda per cell
    ///    that calls <c>c.Header(text)</c> / <c>c.Bolded(false)</c> etc.).
    ///    Always restores <c>pdf.X</c> to the column-start it captured at
    ///    entry, so consecutive <c>Render</c> calls don't drift sideways.
    ///
    ///  - <c>PrintRow</c> / <c>PrintRow2</c> / <c>MeasureThenPrintRow</c>
    ///    -- legacy. Marked <c>[Obsolete]</c>; new code should drop in
    ///    <c>Render</c> / <c>Measure</c> instead. Behaviour preserved so
    ///    the migrated valves report keeps compiling while it's ported.
    /// </summary>
    public static class FpdExtender
    {
        // ============================================================
        // New API: Render + Measure
        // ============================================================

        /// <summary>
        /// Draw the row at <c>(pdf.X, pdf.Y)</c> using each cell's
        /// pre-configured role. Each string becomes one cell's content;
        /// the cell's <see cref="TableCell.Role"/> picks the styling
        /// (default <see cref="CellRole.Data"/>). After drawing,
        /// <c>pdf.X</c> is restored to the value it had on entry and
        /// <c>pdf.Y</c> has advanced by the row's measured height.
        /// </summary>
        public static FPdf Render(this TableRow row, FPdf pdf, params string[] texts)
        {
            var actions = texts.Select<string, Action<TableCell>>(t => c => ApplyRole(c, t)).ToArray();
            return Render(row, pdf, actions);
        }

        /// <summary>
        /// Draw the row using one configuration lambda per cell. Each
        /// lambda receives the <see cref="TableCell"/> and is expected
        /// to call <c>c.Header(...)</c> / <c>c.Label(...)</c> / etc., plus
        /// any per-cell tweaks (<c>c.Bolded(false)</c>,
        /// <c>c.BackgroundColor(...)</c>). Auto-restores <c>pdf.X</c>.
        /// </summary>
        public static FPdf Render(this TableRow row, FPdf pdf, params Action<TableCell>[] configs)
        {
            var startX = pdf.X;
            try
            {
                var maxHeight = MeasureInternal(pdf, row, configs);
                DrawCells(pdf, row, configs, maxHeight);
                pdf.Ln();
            }
            finally
            {
                pdf.SetX(startX);
            }
            return pdf;
        }

        /// <summary>
        /// Measure the row's rendered height in user units without
        /// emitting any PDF content. Useful for "decide before drawing"
        /// flows: peek at the height to know whether the row + a
        /// signature box still fit on the current page, page-break
        /// first if not.
        /// </summary>
        public static double Measure(this TableRow row, FPdf pdf, params string[] texts)
        {
            var actions = texts.Select<string, Action<TableCell>>(t => c => ApplyRole(c, t)).ToArray();
            return MeasureInternal(pdf, row, actions);
        }

        /// <summary>
        /// Lambda-flavoured <see cref="Measure(TableRow, FPdf, string[])"/>.
        /// </summary>
        public static double Measure(this TableRow row, FPdf pdf, params Action<TableCell>[] configs)
            => MeasureInternal(pdf, row, configs);

        // --- internals -------------------------------------------------

        private static void ApplyRole(TableCell c, string text)
        {
            switch (c.Role)
            {
                case CellRole.Header: c.Header(text); break;
                case CellRole.Label:  c.Label(text);  break;
                case CellRole.Spacer: c.Spacer();     break;
                case CellRole.Data:
                default:              c.Data(text);   break;
            }
        }

        private static double MeasureInternal(FPdf pdf, TableRow row, Action<TableCell>[] configs)
        {
            var maxHeight = row.MaxHeight;
            var maxCol = Math.Min(configs.Length, row.Cells.Count);

            if (configs.Length == 0)
                return pdf.CellMeasure(pdf.CurrentPageSize.Width, row.CellHeight, " ");

            for (var pos = 0; pos < maxCol; pos++)
            {
                var col = row.Cells[ pos ];
                col.Status = StatusEnum.Measure;
                configs[ pos ]?.Invoke(col);

                string oldStyle = null;
                if (col.Bold && !pdf.FontStyle.Contains("B"))
                {
                    oldStyle = pdf.FontStyle;
                    pdf.SetFont("", pdf.FontStyle + "B");
                }
                var h = pdf.CellMeasure(col.Width, col.CellHeight ?? row.CellHeight, col.Content);
                if (oldStyle != null) pdf.SetFont("", oldStyle);

                if (maxHeight < h) maxHeight = h;
            }
            return maxHeight;
        }

        private static void DrawCells(FPdf pdf, TableRow row, Action<TableCell>[] configs, double maxHeight)
        {
            var maxCol = Math.Min(configs.Length, row.Cells.Count);
            for (var pos = 0; pos < maxCol; pos++)
            {
                var col = row.Cells[ pos ];
                col.Status = StatusEnum.Draw;
                configs[ pos ]?.Invoke(col);
                if (col.Fill) pdf.SetFillColor(col.Background);

                string oldStyle = null;
                if (col.Bold && !pdf.FontStyle.Contains("B"))
                {
                    oldStyle = pdf.FontStyle;
                    pdf.SetFont("", pdf.FontStyle + "B");
                }
                pdf.BoxedText(col.Width, col.CellHeight ?? row.CellHeight, maxHeight, col.Content,
                    row.Border ?? col.Border, 0, col.Align, col.Fill);
                if (oldStyle != null) pdf.SetFont("", oldStyle);
            }
            // Pad the trailing columns the caller didn't supply content for.
            for (var pos = maxCol; pos < row.Cells.Count; pos++)
            {
                var col = row.Cells[ pos ];
                if (col.Fill) pdf.SetFillColor(col.Background);
                pdf.BoxedText(col.Width, col.CellHeight ?? row.CellHeight, maxHeight, " ",
                    row.Border ?? col.Border, 0, col.Align, col.Fill);
            }
        }

        // ============================================================
        // Legacy API: PrintRow / PrintRow2 / MeasureThenPrintRow
        // ============================================================

        [Obsolete("Use Render(pdf, ...) instead. PrintRow2 leaks FPDF's set-and-return-text-from-the-lambda dance and doesn't restore pdf.X between rows.")]
        public static TableRow PrintRow2(this TableRow row, FPdf pdf, params Func<TableCell, string>[] texts)
        {
            pdf.PrintRow2(row, texts);
            return row;
        }

        [Obsolete("Use Render(pdf, ...) instead.")]
        public static FPdf PrintRow2(this FPdf pdf, TableRow row, params Func<TableCell, string>[] texts)
        {
            var maxHeight = LegacyMeasureRow(pdf, row, texts);
            var maxCol = Math.Min(texts.Length, row.Cells.Count);

            for (var pos = 0; pos < maxCol; pos++)
            {
                var col = row.Cells[ pos ];
                col.Status = StatusEnum.Draw;
                if (col.Fill) pdf.SetFillColor(col.Background);
                pdf.SavePos();
                var text = texts[ pos ]?.Invoke(col);
                pdf.GoBack();
                string oldStyle = null;
                if (col.Bold && !pdf.FontStyle.Contains("B"))
                {
                    oldStyle = pdf.FontStyle;
                    pdf.SetFont("", pdf.FontStyle + "B");
                }
                pdf.BoxedText(col.Width, col.CellHeight ?? row.CellHeight, maxHeight, text, row.Border ?? col.Border, 0, col.Align, col.Fill);
                if (oldStyle != null) pdf.SetFont("", oldStyle);
            }

            for (var pos = maxCol; pos < row.Cells.Count; pos++)
            {
                var col = row.Cells[ pos ];
                if (col.Fill) pdf.SetFillColor(col.Background);
                pdf.BoxedText(col.Width, col.CellHeight ?? row.CellHeight, maxHeight, " ", row.Border ?? col.Border, 0, col.Align, col.Fill);
            }

            pdf.Ln();
            return pdf;
        }

        [Obsolete("Use row.Measure(pdf, ...) + your own if/else; the callback pattern conflates measure with policy.")]
        public static FPdf MeasureThenPrintRow(
            this FPdf pdf,
            TableRow row,
            Func<double, bool> beforePrint,
            params Func<TableCell, string>[] texts)
        {
            var maxHeight = LegacyMeasureRow(pdf, row, texts);
            var print = beforePrint?.Invoke(maxHeight) ?? true;
            if (!print) return pdf;

            var maxCol = Math.Min(texts.Length, row.Cells.Count);
            for (var pos = 0; pos < maxCol; pos++)
            {
                var col = row.Cells[ pos ];
                col.Status = StatusEnum.Draw;
                if (col.Fill) pdf.SetFillColor(col.Background);
                pdf.SavePos();
                var text = texts[ pos ]?.Invoke(col);
                pdf.GoBack();
                string oldStyle = null;
                if (col.Bold && !pdf.FontStyle.Contains("B"))
                {
                    oldStyle = pdf.FontStyle;
                    pdf.SetFont("", pdf.FontStyle + "B");
                }
                pdf.BoxedText(col.Width, col.CellHeight ?? row.CellHeight, maxHeight, text, row.Border ?? col.Border, 0, col.Align, col.Fill);
                if (oldStyle != null) pdf.SetFont("", oldStyle);
            }
            for (var pos = maxCol; pos < row.Cells.Count; pos++)
            {
                var col = row.Cells[ pos ];
                if (col.Fill) pdf.SetFillColor(col.Background);
                pdf.BoxedText(col.Width, col.CellHeight ?? row.CellHeight, maxHeight, " ", row.Border ?? col.Border, 0, col.Align, col.Fill);
            }
            pdf.Ln();
            return pdf;
        }

        private static double LegacyMeasureRow(this FPdf pdf, TableRow row, Func<TableCell, string>[] texts)
        {
            var maxHeight = row.MaxHeight;
            var maxCol = Math.Min(texts.Length, row.Cells.Count);
            if (!texts.Any())
                maxHeight = pdf.CellMeasure(pdf.CurrentPageSize.Width, row.CellHeight, " ");

            for (var pos = 0; pos < maxCol; pos++)
            {
                var col = row.Cells[ pos ];
                col.Status = StatusEnum.Measure;
                var text = texts[ pos ]?.Invoke(col);
                string oldStyle = null;
                if (col.Bold && !pdf.FontStyle.Contains("B"))
                {
                    oldStyle = pdf.FontStyle;
                    pdf.SetFont("", pdf.FontStyle + "B");
                }
                var height = pdf.CellMeasure(col.Width, col.CellHeight ?? row.CellHeight, text);
                if (oldStyle != null) pdf.SetFont("", oldStyle);
                if (maxHeight < height) maxHeight = height;
            }
            return maxHeight;
        }

        [Obsolete("Use Render(pdf, params string[]) instead. PrintRow doesn't restore pdf.X between rows.")]
        public static FPdf PrintRow(this FPdf pdf, TableRow row, params string[] texts)
        {
            var maxCol = Math.Min(texts.Length, row.Cells.Count);
            var maxHeight = row.MaxHeight;

            if (!texts.Any())
                maxHeight = pdf.CellMeasure(pdf.CurrentPageSize.Width, row.CellHeight, " ");

            for (var pos = 0; pos < maxCol; pos++)
            {
                var col = row.Cells[ pos ];
                string oldStyle = null;
                if (col.Bold && !pdf.FontStyle.Contains("B"))
                {
                    oldStyle = pdf.FontStyle;
                    pdf.SetFont("", pdf.FontStyle + "B");
                }
                var height = pdf.CellMeasure(col.Width, row.CellHeight, texts[ pos ]);
                if (maxHeight < height) maxHeight = height;
                if (oldStyle != null) pdf.SetFont("", oldStyle);
            }

            for (var pos = 0; pos < maxCol; pos++)
            {
                var col = row.Cells[ pos ];
                string oldStyle = null;
                if (col.Bold && !pdf.FontStyle.Contains("B"))
                {
                    oldStyle = pdf.FontStyle;
                    pdf.SetFont("", pdf.FontStyle + "B");
                }
                if (col.Fill) pdf.SetFillColor(col.Background);
                pdf.BoxedText(col.Width, row.CellHeight, maxHeight, texts[ pos ], row.Border ?? col.Border, 0, col.Align, col.Fill);
                if (oldStyle != null) pdf.SetFont("", oldStyle);
            }

            for (var pos = maxCol; pos < row.Cells.Count; pos++)
            {
                var col = row.Cells[ pos ];
                if (col.Fill) pdf.SetFillColor(col.Background);
                pdf.BoxedText(col.Width, row.CellHeight, maxHeight, " ", row.Border ?? col.Border, 0, col.Align, col.Fill);
            }

            pdf.Ln();
            return pdf;
        }
    }
}
