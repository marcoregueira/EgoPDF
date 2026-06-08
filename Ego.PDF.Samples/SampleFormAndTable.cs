using Ego.PDF;
using Ego.PDF.Data;
using Microsoft.Xna.Framework;
using System;
using System.IO;

namespace Ego.PDF.Samples
{
    // Light grey used as the label background so the field captions read
    // as headings without overpowering the value beneath them. Same shade
    // for both halves of the form so the layout reads as one block.
    static class FormStyle
    {
        public static readonly Color LabelFill = new Color(238, 240, 244);
        public static readonly Color TitleFill = new Color(220, 224, 232);
    }

    /// <summary>
    /// End-to-end exercise of the new <see cref="TableRow"/> /
    /// <see cref="TableCell"/> fluent API. Two halves:
    ///
    ///  1. A pen-and-ink application form (instancia in Spanish admin
    ///     vocabulary). Heterogeneous rows: a title
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
            EgoPdfBrand.LoadPoppins(pdf);
            pdf.SetAutoPageBreak(true, PageMargin);
            pdf.AddPage(PageOrientation.Portrait, PageSizeEnum.A4);

            DrawBrandBand(pdf);
            pdf.SetFont("helvetica", "", 10);

            DrawInstanciaForm(pdf);
            pdf.Ln(10);
            DrawDataTable(pdf);

            pdf.Close();
            return pdf.Buffer.BaseStream;
        }

        /// <summary>
        /// EgoPDF brand band, same shape the Markdown / PV samples use:
        /// dark strip across the top, wordmark on the left, document
        /// title + subtitle on the right. The form starts a few dots
        /// below it.
        /// </summary>
        private static void DrawBrandBand(SampleFormAndTable pdf)
        {
            const double bandHeight = 44;
            pdf.SetFillColor(EgoPdfBrand.Dark);
            pdf.Rect(0, 0, pdf.W, bandHeight, "F");

            EgoPdfBrand.DrawWordmark(pdf, x: 20, y: 14, sizePt: 22,
                egoColor: Color.White, pdfColor: EgoPdfBrand.Accent, cellHeight: 11);

            pdf.SetFont("Poppins", "", 22);
            pdf.SetTextColor(Color.White);
            pdf.SetXY(20, 12);
            pdf.Cell(pdf.W - 40, 12, "FORM & DATA TABLE", "0", 0, AlignEnum.Right);

            pdf.SetFont("Helvetica", "", 9);
            pdf.SetTextColor(EgoPdfBrand.SubText);
            pdf.SetXY(20, 28);
            pdf.Cell(pdf.W - 40, 6, "Pen-and-ink application form + paginated data table", "0", 0, AlignEnum.Right);

            pdf.SetY(bandHeight + 10);
            pdf.SetX(PageMargin);
            pdf.SetTextColor(EgoPdfBrand.Dark);
        }

        // -----------------------------------------------------------------
        // 1. "Instancia" — pen-and-ink form
        // -----------------------------------------------------------------

        private static void DrawInstanciaForm(FPdf pdf)
        {
            double width = pdf.CurrentPageSize.Width - 2 * PageMargin;
            pdf.SetX(PageMargin);

            // The default FPdf line width (~0.2 dots at this scale) draws
            // borders that look fragmented next to bold text and grey fill;
            // 0.3 pt is still a hairline but reads as a deliberate stroke.
            pdf.SetLineWidth(0.3);

            // Title band.
            new TableRow(1).Width(width).SetCellHeight(10)
                .Bordered(Borders.All)
                .Render(pdf, c => c.Header("SOLICITUD DE INSCRIPCIÓN").BackgroundColor(FormStyle.TitleFill));

            pdf.Ln(BlockGap);

            // ---- Personal data block ---------------------------------
            // Row 1: NOMBRE | APELLIDO 1 | APELLIDO 2 | DNI
            pdf.SetX(PageMargin);
            FormPair4(pdf, width, props: (3, 3, 3, 2),
                labels: ("NOMBRE", "APELLIDO 1", "APELLIDO 2", "DNI"),
                values: ("MARÍA",  "GARCÍA",     "LÓPEZ",      "12345678X"));

            // Row 2: FECHA NACIMIENTO | LOCALIDAD | ESTADO CIVIL
            pdf.SetX(PageMargin);
            FormPair3(pdf, width, props: (3, 4, 3),
                labels: ("FECHA DE NACIMIENTO", "LOCALIDAD",     "ESTADO CIVIL"),
                values: ("1992-04-17",          "PORRIÑO",       "Soltera"));

            pdf.Ln(BlockGap);

            // ---- Address block ---------------------------------------
            // Row 3a: TIPO DE VÍA | NOMBRE DE LA VÍA | Nº | Piso | Pta.
            // Short labels for the narrow trailing columns so they don't
            // wrap onto a second line — the same shorthand a paper
            // paper application form would use.
            pdf.SetX(PageMargin);
            FormPair5(pdf, width, props: (2, 6, 1, 1, 1),
                labels: ("TIPO DE VÍA", "NOMBRE DE LA VÍA",    "Nº", "PISO", "PTA."),
                values: ("CALLE",       "MAYOR",               "1",  "3º",   "B"));

            // Row 3b: CÓDIGO POSTAL | LOCALIDAD | PROVINCIA | PAÍS
            pdf.SetX(PageMargin);
            FormPair4(pdf, width, props: (2, 3, 3, 3),
                labels: ("CÓDIGO POSTAL", "LOCALIDAD", "PROVINCIA", "PAÍS"),
                values: ("28013",         "MADRID",    "MADRID",    "ESPAÑA"));

            pdf.Ln(SignatureGap);

            // ---- Signature block (extra gap above) -------------------
            pdf.SetX(PageMargin);
            new TableRow(2, 2, 3).Width(width).SetCellHeight(LabelHeight)
                .Render(pdf,
                    c => Label("FECHA", c).Bordered(Borders.All & ~Borders.Bottom),
                    c => Label("LUGAR", c).Bordered(Borders.All & ~Borders.Bottom),
                    c => Label("FIRMA", c).Bordered(Borders.All & ~Borders.Bottom));

            pdf.SetX(PageMargin);
            new TableRow(2, 2, 3).Width(width).SetCellHeight(SignatureHeight)
                .Render(pdf,
                    c => c.Data("2026-06-08").Bordered(Borders.All & ~Borders.Top),
                    c => c.Data("MADRID").Bordered(Borders.All & ~Borders.Top),
                    c => c.Data("").Bordered(Borders.All & ~Borders.Top));   // ink space
        }

        private static void FormPair3(FPdf pdf, double width,
            (double, double, double) props,
            (string, string, string) labels,
            (string, string, string) values)
        {
            new TableRow(props.Item1, props.Item2, props.Item3).Width(width).SetCellHeight(LabelHeight)
                .Render(pdf,
                    c => Label(labels.Item1, c).Bordered(Borders.All & ~Borders.Bottom),
                    c => Label(labels.Item2, c).Bordered(Borders.All & ~Borders.Bottom),
                    c => Label(labels.Item3, c).Bordered(Borders.All & ~Borders.Bottom));
            pdf.SetX(PageMargin);
            new TableRow(props.Item1, props.Item2, props.Item3).Width(width).SetCellHeight(ValueHeight)
                .Render(pdf,
                    c => c.Data(values.Item1).Bordered(Borders.All & ~Borders.Top),
                    c => c.Data(values.Item2).Bordered(Borders.All & ~Borders.Top),
                    c => c.Data(values.Item3).Bordered(Borders.All & ~Borders.Top));
        }

        private static void FormPair4(FPdf pdf, double width,
            (double, double, double, double) props,
            (string, string, string, string) labels,
            (string, string, string, string) values)
        {
            new TableRow(props.Item1, props.Item2, props.Item3, props.Item4).Width(width).SetCellHeight(LabelHeight)
                .Render(pdf,
                    c => Label(labels.Item1, c).Bordered(Borders.All & ~Borders.Bottom),
                    c => Label(labels.Item2, c).Bordered(Borders.All & ~Borders.Bottom),
                    c => Label(labels.Item3, c).Bordered(Borders.All & ~Borders.Bottom),
                    c => Label(labels.Item4, c).Bordered(Borders.All & ~Borders.Bottom));
            pdf.SetX(PageMargin);
            new TableRow(props.Item1, props.Item2, props.Item3, props.Item4).Width(width).SetCellHeight(ValueHeight)
                .Render(pdf,
                    c => c.Data(values.Item1).Bordered(Borders.All & ~Borders.Top),
                    c => c.Data(values.Item2).Bordered(Borders.All & ~Borders.Top),
                    c => c.Data(values.Item3).Bordered(Borders.All & ~Borders.Top),
                    c => c.Data(values.Item4).Bordered(Borders.All & ~Borders.Top));
        }

        private static void FormPair5(FPdf pdf, double width,
            (double, double, double, double, double) props,
            (string, string, string, string, string) labels,
            (string, string, string, string, string) values)
        {
            new TableRow(props.Item1, props.Item2, props.Item3, props.Item4, props.Item5)
                .Width(width).SetCellHeight(LabelHeight)
                .Render(pdf,
                    c => Label(labels.Item1, c).Bordered(Borders.All & ~Borders.Bottom),
                    c => Label(labels.Item2, c).Bordered(Borders.All & ~Borders.Bottom),
                    c => Label(labels.Item3, c).Bordered(Borders.All & ~Borders.Bottom),
                    c => Label(labels.Item4, c).Bordered(Borders.All & ~Borders.Bottom),
                    c => Label(labels.Item5, c).Bordered(Borders.All & ~Borders.Bottom));
            pdf.SetX(PageMargin);
            new TableRow(props.Item1, props.Item2, props.Item3, props.Item4, props.Item5)
                .Width(width).SetCellHeight(ValueHeight)
                .Render(pdf,
                    c => c.Data(values.Item1).Bordered(Borders.All & ~Borders.Top),
                    c => c.Data(values.Item2).Bordered(Borders.All & ~Borders.Top),
                    c => c.Data(values.Item3).Bordered(Borders.All & ~Borders.Top),
                    c => c.Data(values.Item4).Bordered(Borders.All & ~Borders.Top),
                    c => c.Data(values.Item5).Bordered(Borders.All & ~Borders.Top));
        }

        private const double LabelHeight     = 6;     // taller than the old 4 so the text breathes
        private const double ValueHeight     = 8;
        private const double SignatureHeight = 16;
        private const double BlockGap        = 3;     // between thematic blocks (personal / address / signature)
        private const double SignatureGap    = 10;    // bigger separation before the signature block

        /// <summary>
        /// Convenience wrapper around <see cref="TableCell.Label(string)"/>
        /// that also paints the light-grey fill the form uses for every
        /// caption. Kept as a local helper rather than baking the colour
        /// into <c>TableCell.Label</c> because the colour is a per-document
        /// style decision, not a library default.
        /// </summary>
        private static TableCell Label(string text, TableCell c) =>
            c.Label(text).BackgroundColor(FormStyle.LabelFill);

        /// <summary>
        /// Helper for the two-column label-on-top / value-below pair. Two
        /// independent <see cref="TableRow"/>s rendered back-to-back; the
        /// label row drops the bottom border and the value row drops the
        /// top one so they share a seam.
        /// </summary>
        private static void FormPair(FPdf pdf, double width,
            string leftLabel, string leftValue, string rightLabel, string rightValue)
        {
            new TableRow(1, 1).Width(width).SetCellHeight(LabelHeight)
                .Render(pdf,
                    c => Label(leftLabel, c).Bordered(Borders.All & ~Borders.Bottom),
                    c => Label(rightLabel, c).Bordered(Borders.All & ~Borders.Bottom));
            pdf.SetX(PageMargin);
            new TableRow(1, 1).Width(width).SetCellHeight(ValueHeight)
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
