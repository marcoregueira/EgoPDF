using Ego.PDF.Data;
using Microsoft.Xna.Framework;
using System.IO;

namespace Ego.PDF.Samples
{
    /// <summary>
    /// Modern invoice — minimalist monochrome variant.
    /// Same content as Sample8 but with no coloured header band: white
    /// background, single coral accent used sparingly, hierarchy driven
    /// by type weight and a few hairline dividers.
    /// </summary>
    public class Sample8b : FPdf
    {
        private static readonly Color BrandDark   = new Color(26, 29, 38);
        private static readonly Color BrandAccent = new Color(204, 105, 95);
        private static readonly Color TextMuted   = new Color(110, 115, 130);
        private static readonly Color HairLine    = new Color(220, 220, 224);

        private Sample8b(string file) : base(file)
        {
        }

        public static Stream GetSample(string file, string path)
        {
            using (var pdf = new Sample8b(file))
            {
                pdf.SetMargins(20, 20, 20);
                pdf.SetAutoPageBreak(false, 0);
                pdf.AddPage();

                DrawHeader(pdf);
                DrawParties(pdf);
                DrawInvoiceMeta(pdf);
                var endY = DrawItems(pdf);
                DrawTotals(pdf, endY + 8);
                DrawFooter(pdf);

                pdf.Close();
                return pdf.Buffer.BaseStream;
            }
        }

        private static void DrawHeader(Sample8b pdf)
        {
            // Wordmark on the left — "ego" dark + "Pdf" coral.
            pdf.SetXY(20, 20);
            pdf.SetFont("Helvetica", "B", 24);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(pdf.GetStringWidth("ego"), 11, "ego");
            pdf.SetTextColor(BrandAccent);
            pdf.Cell(pdf.GetStringWidth("Pdf"), 11, "Pdf");

            // INVOICE on the right.
            pdf.SetXY(20, 18);
            pdf.SetFont("Helvetica", "", 30);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(pdf.W - 40, 13, "INVOICE", "0", 0, AlignEnum.Right);

            pdf.SetXY(20, 33);
            pdf.SetFont("Helvetica", "", 9);
            pdf.SetTextColor(TextMuted);
            pdf.Cell(pdf.W - 40, 5, "#2026-001  ·  23 May 2026", "0", 0, AlignEnum.Right);

            // Hairline divider under the header.
            pdf.SetDrawColor(HairLine);
            pdf.SetLineWidth(0.2);
            pdf.Line(20, 44, pdf.W - 20, 44);
        }

        private static void DrawParties(Sample8b pdf)
        {
            const double bodyY = 56;
            DrawPartyBlock(pdf, "FROM", "Acme Studio", new[]
            {
                "100 Innovation Blvd",
                "San Francisco, CA 94110",
                "United States",
            }, x: 20, y: bodyY);

            DrawPartyBlock(pdf, "BILLED TO", "Globex Corporation", new[]
            {
                "200 Market Street",
                "Seattle, WA 98101",
                "United States",
            }, x: 115, y: bodyY);
        }

        private static void DrawPartyBlock(Sample8b pdf, string label, string name, string[] addressLines,
            double x, double y)
        {
            pdf.SetXY(x, y);
            pdf.SetFont("Helvetica", "B", 8);
            pdf.SetTextColor(BrandAccent);
            pdf.Cell(80, 5, label);

            pdf.SetXY(x, y + 6);
            pdf.SetFont("Helvetica", "B", 13);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(80, 6, name);

            pdf.SetFont("Helvetica", "", 10);
            pdf.SetTextColor(TextMuted);
            for (int i = 0; i < addressLines.Length; i++)
            {
                pdf.SetXY(x, y + 14 + i * 5);
                pdf.Cell(80, 5, addressLines[i]);
            }
        }

        private static void DrawInvoiceMeta(Sample8b pdf)
        {
            const double metaY = 104;
            var cells = new (string label, string value)[]
            {
                ("INVOICE #", "2026-001"),
                ("ISSUED",   "23 May 2026"),
                ("DUE",      "22 Jun 2026"),
            };
            double cellWidth = (pdf.W - 40) / 3.0;

            for (int i = 0; i < cells.Length; i++)
            {
                double x = 20 + i * cellWidth;
                pdf.SetXY(x, metaY);
                pdf.SetFont("Helvetica", "B", 8);
                pdf.SetTextColor(BrandAccent);
                pdf.Cell(cellWidth, 5, cells[i].label);

                pdf.SetXY(x, metaY + 6);
                pdf.SetFont("Helvetica", "B", 12);
                pdf.SetTextColor(BrandDark);
                pdf.Cell(cellWidth, 6, cells[i].value);
            }
        }

        private static double DrawItems(Sample8b pdf)
        {
            const double tableY = 130;
            const double rowH = 10;
            const double colItem = 90;
            const double colQty = 20;
            const double colUnit = 30;
            const double colAmount = 30;

            // Column headers — no fill, just dark bottom rule.
            pdf.SetXY(20, tableY);
            pdf.SetFont("Helvetica", "B", 9);
            pdf.SetTextColor(TextMuted);
            pdf.Cell(colItem,   8, "ITEM",       "0", 0, AlignEnum.Left);
            pdf.Cell(colQty,    8, "QTY",        "0", 0, AlignEnum.Right);
            pdf.Cell(colUnit,   8, "UNIT PRICE", "0", 0, AlignEnum.Right);
            pdf.Cell(colAmount, 8, "AMOUNT",     "0", 1, AlignEnum.Right);

            pdf.SetDrawColor(BrandDark);
            pdf.SetLineWidth(0.3);
            pdf.Line(20, tableY + 8, pdf.W - 20, tableY + 8);

            // Data rows.
            var items = new (string desc, string qty, string unit, string amount)[]
            {
                ("Custom report templates",         "5", "$120.00", "$600.00"),
                ("PDF rendering license (annual)",  "1", "$480.00", "$480.00"),
                ("On-site implementation support",  "8",  "$95.00", "$760.00"),
            };

            pdf.SetFont("Helvetica", "", 10);
            pdf.SetTextColor(BrandDark);
            pdf.SetDrawColor(HairLine);
            pdf.SetLineWidth(0.2);

            foreach (var item in items)
            {
                pdf.Cell(colItem,   rowH, item.desc,   "B", 0, AlignEnum.Left);
                pdf.Cell(colQty,    rowH, item.qty,    "B", 0, AlignEnum.Right);
                pdf.Cell(colUnit,   rowH, item.unit,   "B", 0, AlignEnum.Right);
                pdf.Cell(colAmount, rowH, item.amount, "B", 1, AlignEnum.Right);
            }

            return pdf.Y;
        }

        private static void DrawTotals(Sample8b pdf, double startY)
        {
            const double labelW = 50;
            const double valueW = 30;
            double labelX = pdf.W - pdf.RightMargin - labelW - valueW;

            pdf.SetXY(labelX, startY);
            pdf.SetFont("Helvetica", "", 10);
            pdf.SetTextColor(TextMuted);
            pdf.Cell(labelW, 6, "Subtotal", "0", 0, AlignEnum.Right);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(valueW, 6, "$1,840.00", "0", 1, AlignEnum.Right);

            pdf.SetX(labelX);
            pdf.SetTextColor(TextMuted);
            pdf.Cell(labelW, 6, "VAT 20%", "0", 0, AlignEnum.Right);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(valueW, 6, "$368.00", "0", 1, AlignEnum.Right);

            // Subtle rule above the total line.
            double ruleY = pdf.Y + 1;
            pdf.SetDrawColor(BrandDark);
            pdf.SetLineWidth(0.3);
            pdf.Line(labelX, ruleY, pdf.W - pdf.RightMargin, ruleY);

            pdf.Y += 3;
            pdf.SetX(labelX);
            pdf.SetFont("Helvetica", "B", 13);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(labelW, 9, "TOTAL", "0", 0, AlignEnum.Right);
            pdf.SetTextColor(BrandAccent);
            pdf.Cell(valueW, 9, "$2,208.00", "0", 1, AlignEnum.Right);
        }

        private static void DrawFooter(Sample8b pdf)
        {
            const double footerY = 268;

            pdf.SetDrawColor(HairLine);
            pdf.SetLineWidth(0.2);
            pdf.Line(20, footerY - 4, pdf.W - 20, footerY - 4);

            pdf.SetFont("Helvetica", "B", 9);
            pdf.SetTextColor(BrandDark);
            pdf.SetXY(20, footerY);
            pdf.Cell(80, 5, "PAYMENT");

            pdf.SetFont("Helvetica", "", 9);
            pdf.SetTextColor(TextMuted);
            pdf.SetXY(20, footerY + 6);
            pdf.Cell(120, 5, "ES00 0300 0303 00303 03030");
            pdf.SetXY(20, footerY + 11);
            pdf.Cell(120, 5, "SWIFT  ESW-123453");

            pdf.SetFont("Helvetica", "BI", 14);
            pdf.SetTextColor(BrandAccent);
            pdf.SetXY(120, footerY + 3);
            pdf.Cell(pdf.W - 140, 10, "Thank you for your business", "0", 0, AlignEnum.Right);
        }
    }
}
