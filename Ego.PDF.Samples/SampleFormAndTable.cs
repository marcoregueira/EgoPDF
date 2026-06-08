using Ego.PDF;
using Ego.PDF.Data;
using Microsoft.Xna.Framework;
using System;
using System.IO;

namespace Ego.PDF.Samples
{
    /// <summary>
    /// End-to-end exercise of the new <see cref="TableRow"/> /
    /// <see cref="TableCell"/> fluent API. Two halves:
    ///
    ///  1. A pen-and-ink "instancia" form. Heterogeneous rows: a title
    ///     band, label + value pairs across two columns, a wide
    ///     domicilio field that wraps, a signature row at the bottom.
    ///     Uses the new <c>Borders.All &amp; ~Borders.Bottom</c> /
    ///     <c>~Borders.Top</c> trick to render each label-value pair as
    ///     a single visual box without a divider in the middle.
    ///
    ///  2. A flat data table -- columns Producto / Cantidad / Precio /
    ///     Total. Uses <see cref="TableRow.Roles(CellRole[])"/> +
    ///     positional <see cref="FpdExtender.Render(TableRow,FPdf,string[])"/>
    ///     so the caller passes raw strings without lambdas. Also shows
    ///     the <c>row.Measure</c> escape: before each data row the loop
    ///     checks whether the row + the footer "TOTAL" still fits on
    ///     the current page; if not, it forces a page break and reprints
    ///     the header on the new page.
    /// </summary>
    public class SampleFormAndTable: FPdf
    {
        private SampleFormAndTable(string file) : base(file) { }

        private const double PageMargin = 15;
        private const double TableLineHeight = 6;

        public static Stream GetSample(string file)
        {
            using var pdf = new SampleFormAndTable(file);
            pdf.SetAutoPageBreak(true, PageMargin);
            pdf.AddPage(PageOrientation.Portrait, PageSizeEnum.A4);
            pdf.SetFont("helvetica", "", 10);

            DrawInstanciaForm(pdf);
            pdf.Ln(10);
            DrawDataTable(pdf);

            pdf.Close();
            return pdf.Buffer.BaseStream;
        }

        // -----------------------------------------------------------------
        // 1. "Instancia" — pen-and-ink form
        // -----------------------------------------------------------------

        private static void DrawInstanciaForm(FPdf pdf)
        {
            double width = pdf.CurrentPageSize.Width - 2 * PageMargin;
            pdf.SetX(PageMargin);

            // Title band: one cell, all width, centred header.
            new TableRow(1).Width(width).SetCellHeight(10)
                .Bordered(Borders.All)
                .Render(pdf, c => c.Header("SOLICITUD DE INSCRIPCIÓN").BackgroundColor(new Color(230, 230, 240)));

            pdf.Ln(2);

            // Two-column form pairs. Each pair = a label row (no bottom
            // border) on top of a value row (no top border) so the pair
            // looks like one box with the label as a small caption.
            pdf.SetX(PageMargin);
            FormPair(pdf, width, leftLabel: "NOMBRE", leftValue: "MARÍA",
                                 rightLabel: "APELLIDOS", rightValue: "GARCÍA LÓPEZ");

            pdf.SetX(PageMargin);
            FormPair(pdf, width, leftLabel: "DNI", leftValue: "12345678X",
                                 rightLabel: "FECHA DE NACIMIENTO", rightValue: "1992-04-17");

            // One-column wide row — domicilio wraps onto two lines.
            pdf.SetX(PageMargin);
            new TableRow(1).Width(width).SetCellHeight(4)
                .Render(pdf, c => c.Label("DOMICILIO").Bordered(Borders.All & ~Borders.Bottom));
            pdf.SetX(PageMargin);
            new TableRow(1).Width(width).SetCellHeight(6)
                .Render(pdf, c => c.Data("CALLE MAYOR 1, 3º B — 28013 MADRID (MADRID), ESPAÑA")
                                    .Bordered(Borders.All & ~Borders.Top));

            pdf.Ln(2);

            // Three-column row for date + place + signature.
            pdf.SetX(PageMargin);
            new TableRow(2, 2, 3).Width(width).SetCellHeight(4)
                .Render(pdf,
                    c => c.Label("FECHA").Bordered(Borders.All & ~Borders.Bottom),
                    c => c.Label("LUGAR").Bordered(Borders.All & ~Borders.Bottom),
                    c => c.Label("FIRMA").Bordered(Borders.All & ~Borders.Bottom));

            pdf.SetX(PageMargin);
            new TableRow(2, 2, 3).Width(width).SetCellHeight(14)   // taller — room for ink
                .Render(pdf,
                    c => c.Data("2026-06-08").Bordered(Borders.All & ~Borders.Top),
                    c => c.Data("MADRID").Bordered(Borders.All & ~Borders.Top),
                    c => c.Data("").Bordered(Borders.All & ~Borders.Top));   // ink space
        }

        /// <summary>
        /// Helper for the two-column label-on-top / value-below pair. Two
        /// independent <see cref="TableRow"/>s rendered back-to-back; the
        /// label row drops the bottom border and the value row drops the
        /// top one so they share a seam.
        /// </summary>
        private static void FormPair(FPdf pdf, double width,
            string leftLabel, string leftValue, string rightLabel, string rightValue)
        {
            new TableRow(1, 1).Width(width).SetCellHeight(4)
                .Render(pdf,
                    c => c.Label(leftLabel).Bordered(Borders.All & ~Borders.Bottom),
                    c => c.Label(rightLabel).Bordered(Borders.All & ~Borders.Bottom));
            pdf.SetX(PageMargin);
            new TableRow(1, 1).Width(width).SetCellHeight(7)
                .Render(pdf,
                    c => c.Data(leftValue).Bordered(Borders.All & ~Borders.Top),
                    c => c.Data(rightValue).Bordered(Borders.All & ~Borders.Top));
        }

        // -----------------------------------------------------------------
        // 2. Flat data table with measure-before-render page break
        // -----------------------------------------------------------------

        private static void DrawDataTable(FPdf pdf)
        {
            double width = pdf.CurrentPageSize.Width - 2 * PageMargin;
            pdf.SetX(PageMargin);
            pdf.SetFont("helvetica", "B", 10);
            pdf.Cell(width, 6, "DETALLE DE ARTÍCULOS", "", 1, AlignEnum.Left);
            pdf.SetFont("helvetica", "", 10);

            var header = new TableRow(3, 1, 1, 1)
                .Width(width)
                .SetCellHeight(TableLineHeight)
                .SetAlign(AlignEnum.Center)
                .Roles(CellRole.Header, CellRole.Header, CellRole.Header, CellRole.Header);

            // Body row template -- left-aligned product, right-aligned numbers.
            // We swap the per-column align in the lambda for the numeric cells.
            var body = new TableRow(3, 1, 1, 1)
                .Width(width)
                .SetCellHeight(TableLineHeight);

            // Data
            var items = new (string name, int qty, decimal unit)[]
            {
                ("Tornillo M4×20 ZNK",                 120,  0.045m),
                ("Tuerca M4 DIN 934",                  120,  0.020m),
                ("Arandela plana M4",                  240,  0.015m),
                ("Brida de PVC negra 200 mm",          50,   0.180m),
                ("Cable RJ45 Cat 6 — 1 m",             10,   2.500m),
                ("Cable RJ45 Cat 6 — 2 m",             10,   3.250m),
                ("Conector hembra RJ45 cat 6",         30,   0.900m),
                ("Caja de empalmes IP65 100×100×50",   8,    4.250m),
                ("Tubo flexible corrugado Ø20 — 25 m", 4,    9.150m),
                ("Cinta aislante PVC negra 10 m",      12,   0.650m),
                ("Cinta americana negra 25 m",         5,    3.800m),
                ("Embellecedor enchufe schuko blanco", 18,   0.450m),
            };

            const double footerHeight = TableLineHeight + 4;

            pdf.SetX(PageMargin);
            header.Render(pdf, "Producto", "Cantidad", "Unidad (€)", "Total (€)");

            decimal grandTotal = 0m;
            foreach (var (name, qty, unit) in items)
            {
                var total = qty * unit;
                grandTotal += total;

                // Measure FIRST: if this row + the footer wouldn't fit
                // on the current page, break and reprint the header.
                var rowHeight = body.Measure(pdf, name, qty.ToString(), unit.ToString("0.000"), total.ToString("0.00"));
                if (pdf.Y + rowHeight + footerHeight > pdf.PageBreakTrigger)
                {
                    pdf.AddPage();
                    pdf.SetX(PageMargin);
                    header.Render(pdf, "Producto", "Cantidad", "Unidad (€)", "Total (€)");
                }

                pdf.SetX(PageMargin);
                body.Render(pdf,
                    c => c.Data(name).Aligned(AlignEnum.Left),
                    c => c.Data(qty.ToString()).Aligned(AlignEnum.Right),
                    c => c.Data(unit.ToString("0.000")).Aligned(AlignEnum.Right),
                    c => c.Data(total.ToString("0.00")).Aligned(AlignEnum.Right));
            }

            // Footer total row, right-aligned label + right-aligned value.
            pdf.SetX(PageMargin);
            new TableRow(3, 1, 1, 1).Width(width).SetCellHeight(TableLineHeight)
                .Render(pdf,
                    c => c.Spacer(),
                    c => c.Spacer(),
                    c => c.Label("TOTAL").Aligned(AlignEnum.Right).Bordered(Borders.Top | Borders.Left | Borders.Bottom),
                    c => c.Data(grandTotal.ToString("0.00 €")).Aligned(AlignEnum.Right).Bolded().Bordered(Borders.All));
        }
    }
}
