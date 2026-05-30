using System;
using Ego.PDF.Data;
using Microsoft.Xna.Framework;
using System.IO;

namespace Ego.PDF.Samples
{
    /// <summary>
    /// Modern invoice — modular "cards" variant.
    /// Same content as Sample8 but each block (FROM, BILLED TO, INVOICE
    /// INFO, items, totals) lives inside a soft tinted rectangle, like
    /// stacked widgets on a dashboard.
    /// </summary>
    public class Sample8c : FPdf
    {
        private static readonly Color BrandDark   = EgoPdfBrand.Dark;
        private static readonly Color BrandAccent = EgoPdfBrand.Accent;
        private static readonly Color TextMuted   = EgoPdfBrand.Muted;
        // Card-specific tints stay local: they're a single-sample look.
        private static readonly Color CardFill    = new Color(247, 238, 236); // --brand-bg-soft
        private static readonly Color CardBorder  = new Color(227, 214, 211); // --brand-border

        // Single PanelStyle reused across every card on the invoice, so
        // each section is one Panel(slot, "TITLE", Card, body) call
        // instead of the old DrawCardShell + manual title arithmetic.
        private static readonly PanelStyle Card = new()
        {
            FillColor       = CardFill,
            BorderColor     = CardBorder,
            TitleColor      = BrandAccent,
            LineWidth       = 0.2,
            Padding         = 5,
            TitleHeight     = 5,
            TitleHairline   = false,
        };

        private Sample8c(string file) : base(file)
        {
        }

        public static Stream GetSample(string file, string path)
        {
            using (var pdf = new Sample8c(file))
            {
                pdf.SetMargins(20, 20, 20);
                pdf.SetAutoPageBreak(false, 0);
                EgoPdfBrand.LoadPoppins(pdf);
                pdf.AddPage();

                DrawHeader(pdf);
                DrawInfoCards(pdf);
                DrawItemsCard(pdf);
                DrawTotalsCard(pdf);
                DrawFooter(pdf);

                pdf.Close();
                return pdf.Buffer.BaseStream;
            }
        }

        private static string GetBasePath()
        {
            return Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        }

        private static void DrawHeader(Sample8c pdf)
        {
            EgoPdfBrand.DrawWordmark(pdf, x: 20, y: 20, sizePt: 24,
                egoColor: BrandDark, pdfColor: BrandAccent, cellHeight: 11);

            pdf.SetXY(20, 18);
            pdf.SetFont("Poppins", "", 30);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(pdf.W - 40, 13, "INVOICE", "0", 0, AlignEnum.Right);

            pdf.SetXY(20, 33);
            pdf.SetFont("Helvetica", "", 9);
            pdf.SetTextColor(TextMuted);
            pdf.Cell(pdf.W - 40, 5, "#2026-001  ·  23 May 2026", "0", 0, AlignEnum.Right);
        }

        private static void DrawInfoCards(Sample8c pdf)
        {
            // Row carves the three card slots; Panel paints the shell
            // and hands a clean content rect to each filler.
            var slots = pdf.Row(new Rect(20, 50, pdf.W - 40, 48), 3, gap: 5);

            pdf.Panel(slots[0], "FROM", Card, content => FillPartyCard(pdf, content,
                "Acme Studio", new[]
                {
                    "100 Innovation Blvd",
                    "San Francisco, CA 94110",
                    "United States",
                }));

            pdf.Panel(slots[1], "BILLED TO", Card, content => FillPartyCard(pdf, content,
                "Globex Corporation", new[]
                {
                    "200 Market Street",
                    "Seattle, WA 98101",
                    "United States",
                }));

            pdf.Panel(slots[2], "INVOICE INFO", Card, content => FillInvoiceInfoCard(pdf, content));
        }

        private static void FillPartyCard(Sample8c pdf, Rect content, string name, string[] addressLines)
        {
            pdf.SetXY(content.X, content.Y);
            pdf.SetFont("Helvetica", "B", 12);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(content.W, 6, name);

            pdf.SetFont("Helvetica", "", 9);
            pdf.SetTextColor(TextMuted);
            for (int i = 0; i < addressLines.Length; i++)
            {
                pdf.SetXY(content.X, content.Y + 8 + i * 5);
                pdf.Cell(content.W, 5, addressLines[i]);
            }
        }

        private static void FillInvoiceInfoCard(Sample8c pdf, Rect content)
        {
            var pairs = new (string label, string value)[]
            {
                ("Number",  "2026-001"),
                ("Issued",  "23 May 2026"),
                ("Due",     "22 Jun 2026"),
            };
            for (int i = 0; i < pairs.Length; i++)
            {
                double rowY = content.Y + i * 10;
                pdf.SetXY(content.X, rowY);
                pdf.SetFont("Helvetica", "", 9);
                pdf.SetTextColor(TextMuted);
                pdf.Cell(content.W, 4, pairs[i].label);

                pdf.SetXY(content.X, rowY + 4);
                pdf.SetFont("Helvetica", "B", 11);
                pdf.SetTextColor(BrandDark);
                pdf.Cell(content.W, 5, pairs[i].value);
            }
        }

        private static void DrawItemsCard(Sample8c pdf)
        {
            var items = new (string desc, string qty, string unit, string amount)[]
            {
                ("Custom report templates",         "5", "$120.00", "$600.00"),
                ("PDF rendering license (annual)",  "1", "$480.00", "$480.00"),
                ("On-site implementation support",  "8",  "$95.00", "$760.00"),
            };
            const double rowH = 9;
            double cardH = Card.Padding + 8 + items.Length * rowH + Card.Padding;

            pdf.Panel(new Rect(20, 110, pdf.W - 40, cardH), Card, content =>
            {
                const double colItem = 80, colQty = 20, colUnit = 30, colAmount = 30;

                // Table header.
                pdf.SetXY(content.X, content.Y);
                pdf.SetFont("Helvetica", "B", 9);
                pdf.SetTextColor(TextMuted);
                pdf.Cell(colItem,   7, "ITEM",       "0", 0, AlignEnum.Left);
                pdf.Cell(colQty,    7, "QTY",        "0", 0, AlignEnum.Right);
                pdf.Cell(colUnit,   7, "UNIT PRICE", "0", 0, AlignEnum.Right);
                pdf.Cell(colAmount, 7, "AMOUNT",     "0", 1, AlignEnum.Right);

                pdf.SetDrawColor(CardBorder);
                pdf.SetLineWidth(0.2);
                pdf.Line(content.X, content.Y + 7, content.Right, content.Y + 7);

                // Body rows.
                pdf.SetFont("Helvetica", "", 10);
                pdf.SetTextColor(BrandDark);
                double rowY = content.Y + 8;
                foreach (var item in items)
                {
                    pdf.SetXY(content.X, rowY);
                    pdf.Cell(colItem,   rowH, item.desc,   "0", 0, AlignEnum.Left);
                    pdf.Cell(colQty,    rowH, item.qty,    "0", 0, AlignEnum.Right);
                    pdf.Cell(colUnit,   rowH, item.unit,   "0", 0, AlignEnum.Right);
                    pdf.Cell(colAmount, rowH, item.amount, "0", 1, AlignEnum.Right);
                    rowY += rowH;
                }
            });
        }

        private static void DrawTotalsCard(Sample8c pdf)
        {
            const double cardW = 80;
            const double cardH = 35;
            double cardX = pdf.W - pdf.RightMargin - cardW;

            pdf.Panel(new Rect(cardX, 188, cardW, cardH), Card, content =>
            {
                const double labelW = 40;
                double valueW = content.W - labelW;

                // Subtotal
                pdf.SetXY(content.X, content.Y);
                pdf.SetFont("Helvetica", "", 10);
                pdf.SetTextColor(TextMuted);
                pdf.Cell(labelW, 6, "Subtotal", "0", 0, AlignEnum.Left);
                pdf.SetTextColor(BrandDark);
                pdf.Cell(valueW, 6, "$1,840.00", "0", 1, AlignEnum.Right);

                // VAT
                pdf.SetX(content.X);
                pdf.SetTextColor(TextMuted);
                pdf.Cell(labelW, 6, "VAT 20%", "0", 0, AlignEnum.Left);
                pdf.SetTextColor(BrandDark);
                pdf.Cell(valueW, 6, "$368.00", "0", 1, AlignEnum.Right);

                // Divider above total.
                double ruleY = pdf.Y + 1;
                pdf.SetDrawColor(CardBorder);
                pdf.SetLineWidth(0.3);
                pdf.Line(content.X, ruleY, content.Right, ruleY);

                // Total.
                pdf.Y += 3;
                pdf.SetX(content.X);
                pdf.SetFont("Helvetica", "B", 12);
                pdf.SetTextColor(BrandDark);
                pdf.Cell(labelW, 8, "TOTAL", "0", 0, AlignEnum.Left);
                pdf.SetTextColor(BrandAccent);
                pdf.Cell(valueW, 8, "$2,208.00", "0", 1, AlignEnum.Right);
            });
        }

        private static void DrawFooter(Sample8c pdf)
        {
            const double footerY = 268;

            pdf.SetDrawColor(CardBorder);
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
