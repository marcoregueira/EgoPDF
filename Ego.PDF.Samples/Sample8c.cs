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
        private static readonly Color BrandDark   = new Color(26, 29, 38);
        private static readonly Color BrandAccent = new Color(204, 105, 95);
        private static readonly Color TextMuted   = new Color(110, 115, 130);
        private static readonly Color CardFill    = new Color(247, 238, 236); // --brand-bg-soft
        private static readonly Color CardBorder  = new Color(227, 214, 211); // --brand-border

        private Sample8c(string file) : base(file)
        {
        }

        public static Stream GetSample(string file, string path)
        {
            using (var pdf = new Sample8c(file))
            {
                pdf.SetMargins(20, 20, 20);
                pdf.SetAutoPageBreak(false, 0);
                _ = pdf.LoadFont("Poppins", Path.Combine(GetBasePath(), "Fonts/Poppins/Poppins-ExtraLight.ttf"));
                pdf.AddFont("Poppins", "");
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
            pdf.SetXY(20, 20);
            pdf.SetFont("Poppins", "", 24);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(pdf.GetStringWidth("ego"), 11, "ego");
            pdf.SetTextColor(BrandAccent);
            pdf.Cell(pdf.GetStringWidth("Pdf"), 11, "Pdf");

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
            const double cardsY = 50;
            const double cardH = 48;
            const double gap = 5;
            double usableW = pdf.W - 40;          // 170 mm
            double cardW = (usableW - 2 * gap) / 3.0;

            // From
            DrawCardShell(pdf, 20, cardsY, cardW, cardH);
            FillPartyCard(pdf, "FROM", "Acme Studio", new[]
            {
                "100 Innovation Blvd",
                "San Francisco, CA 94110",
                "United States",
            }, 20, cardsY, cardW);

            // Billed To
            double x2 = 20 + cardW + gap;
            DrawCardShell(pdf, x2, cardsY, cardW, cardH);
            FillPartyCard(pdf, "BILLED TO", "Globex Corporation", new[]
            {
                "200 Market Street",
                "Seattle, WA 98101",
                "United States",
            }, x2, cardsY, cardW);

            // Invoice Info
            double x3 = 20 + 2 * (cardW + gap);
            DrawCardShell(pdf, x3, cardsY, cardW, cardH);
            FillInvoiceInfoCard(pdf, x3, cardsY, cardW);
        }

        private static void DrawCardShell(Sample8c pdf, double x, double y, double w, double h)
        {
            // Outline + fill rectangle as a flat "card".
            pdf.SetFillColor(CardFill);
            pdf.SetDrawColor(CardBorder);
            pdf.SetLineWidth(0.2);
            pdf.Rect(x, y, w, h, "DF");
        }

        private static void FillPartyCard(Sample8c pdf, string label, string name, string[] addressLines,
            double cardX, double cardY, double cardW)
        {
            const double padX = 5;
            const double padY = 5;
            double x = cardX + padX;

            pdf.SetXY(x, cardY + padY);
            pdf.SetFont("Helvetica", "B", 8);
            pdf.SetTextColor(BrandAccent);
            pdf.Cell(cardW - 2 * padX, 5, label);

            pdf.SetXY(x, cardY + padY + 6);
            pdf.SetFont("Helvetica", "B", 12);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(cardW - 2 * padX, 6, name);

            pdf.SetFont("Helvetica", "", 9);
            pdf.SetTextColor(TextMuted);
            for (int i = 0; i < addressLines.Length; i++)
            {
                pdf.SetXY(x, cardY + padY + 14 + i * 5);
                pdf.Cell(cardW - 2 * padX, 5, addressLines[i]);
            }
        }

        private static void FillInvoiceInfoCard(Sample8c pdf, double cardX, double cardY, double cardW)
        {
            const double padX = 5;
            const double padY = 5;
            double x = cardX + padX;

            pdf.SetXY(x, cardY + padY);
            pdf.SetFont("Helvetica", "B", 8);
            pdf.SetTextColor(BrandAccent);
            pdf.Cell(cardW - 2 * padX, 5, "INVOICE INFO");

            var pairs = new (string label, string value)[]
            {
                ("Number",  "2026-001"),
                ("Issued",  "23 May 2026"),
                ("Due",     "22 Jun 2026"),
            };
            for (int i = 0; i < pairs.Length; i++)
            {
                double rowY = cardY + padY + 9 + i * 10;
                pdf.SetXY(x, rowY);
                pdf.SetFont("Helvetica", "", 9);
                pdf.SetTextColor(TextMuted);
                pdf.Cell(cardW - 2 * padX, 4, pairs[i].label);

                pdf.SetXY(x, rowY + 4);
                pdf.SetFont("Helvetica", "B", 11);
                pdf.SetTextColor(BrandDark);
                pdf.Cell(cardW - 2 * padX, 5, pairs[i].value);
            }
        }

        private static void DrawItemsCard(Sample8c pdf)
        {
            const double cardX = 20;
            const double cardY = 110;
            double cardW = pdf.W - 40;
            const double padX = 5;
            const double padY = 5;
            const double colItem = 80;
            const double colQty = 20;
            const double colUnit = 30;
            const double colAmount = 30;

            // Compute card height from row count.
            var items = new (string desc, string qty, string unit, string amount)[]
            {
                ("Custom report templates",         "5", "$120.00", "$600.00"),
                ("PDF rendering license (annual)",  "1", "$480.00", "$480.00"),
                ("On-site implementation support",  "8",  "$95.00", "$760.00"),
            };
            const double rowH = 9;
            double cardH = padY + 8 + items.Length * rowH + padY;

            DrawCardShell(pdf, cardX, cardY, cardW, cardH);

            // Header
            double headerY = cardY + padY;
            pdf.SetXY(cardX + padX, headerY);
            pdf.SetFont("Helvetica", "B", 9);
            pdf.SetTextColor(TextMuted);
            pdf.Cell(colItem,   7, "ITEM",       "0", 0, AlignEnum.Left);
            pdf.Cell(colQty,    7, "QTY",        "0", 0, AlignEnum.Right);
            pdf.Cell(colUnit,   7, "UNIT PRICE", "0", 0, AlignEnum.Right);
            pdf.Cell(colAmount, 7, "AMOUNT",     "0", 1, AlignEnum.Right);

            // Separator under header.
            pdf.SetDrawColor(CardBorder);
            pdf.SetLineWidth(0.2);
            pdf.Line(cardX + padX, headerY + 7, cardX + cardW - padX, headerY + 7);

            // Rows (no borders — separation comes from spacing).
            pdf.SetFont("Helvetica", "", 10);
            pdf.SetTextColor(BrandDark);
            double rowY = headerY + 8;
            foreach (var item in items)
            {
                pdf.SetXY(cardX + padX, rowY);
                pdf.Cell(colItem,   rowH, item.desc,   "0", 0, AlignEnum.Left);
                pdf.Cell(colQty,    rowH, item.qty,    "0", 0, AlignEnum.Right);
                pdf.Cell(colUnit,   rowH, item.unit,   "0", 0, AlignEnum.Right);
                pdf.Cell(colAmount, rowH, item.amount, "0", 1, AlignEnum.Right);
                rowY += rowH;
            }
        }

        private static void DrawTotalsCard(Sample8c pdf)
        {
            const double cardW = 80;
            const double cardH = 35;
            double cardX = pdf.W - pdf.RightMargin - cardW;
            const double cardY = 188;
            const double padX = 5;
            const double padY = 5;

            DrawCardShell(pdf, cardX, cardY, cardW, cardH);

            const double labelW = 40;
            double valueW = cardW - 2 * padX - labelW;
            double rowY = cardY + padY;

            // Subtotal
            pdf.SetXY(cardX + padX, rowY);
            pdf.SetFont("Helvetica", "", 10);
            pdf.SetTextColor(TextMuted);
            pdf.Cell(labelW, 6, "Subtotal", "0", 0, AlignEnum.Left);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(valueW, 6, "$1,840.00", "0", 1, AlignEnum.Right);

            // VAT
            pdf.SetX(cardX + padX);
            pdf.SetTextColor(TextMuted);
            pdf.Cell(labelW, 6, "VAT 20%", "0", 0, AlignEnum.Left);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(valueW, 6, "$368.00", "0", 1, AlignEnum.Right);

            // Divider above total
            double ruleY = pdf.Y + 1;
            pdf.SetDrawColor(CardBorder);
            pdf.SetLineWidth(0.3);
            pdf.Line(cardX + padX, ruleY, cardX + cardW - padX, ruleY);

            // Total
            pdf.Y += 3;
            pdf.SetX(cardX + padX);
            pdf.SetFont("Helvetica", "B", 12);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(labelW, 8, "TOTAL", "0", 0, AlignEnum.Left);
            pdf.SetTextColor(BrandAccent);
            pdf.Cell(valueW, 8, "$2,208.00", "0", 1, AlignEnum.Right);
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
