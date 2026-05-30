using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using Ego.PDF.Data;
using Microsoft.Xna.Framework;
using System.IO;

namespace Ego.PDF.Samples
{
    public class Sample8 : FPdf
    {
        private readonly Dictionary<string, int> _tagCount = new Dictionary<string, int>();

        // Brand palette extracted from the egoPdf logo.
        private static readonly Color BrandDark   = new Color(26, 29, 38);
        private static readonly Color BrandAccent = new Color(204, 105, 95);
        private static readonly Color TextMuted   = new Color(110, 115, 130);
        private static readonly Color BandSubText = new Color(180, 184, 196);
        private static readonly Color RowBorder   = new Color(220, 220, 224);

        private Sample8(string file) : base(file)
        {
        }

        public static Stream GetSample(string file, string path)
        {
            using (var pdf = new Sample8(file))
            {
                pdf.SetMargins(20, 20, 20);
                pdf.SetAutoPageBreak(false, 0);
                _ = pdf.LoadFont("Poppins", Path.Combine(GetBasePath(), "Fonts/Poppins/Poppins-ExtraLight.ttf"));
                pdf.AddFont("Poppins", "");
                pdf.AddPage();

                DrawHeaderBand(pdf);
                DrawParties(pdf);
                DrawInvoiceMeta(pdf);
                var endOfTableY = DrawItems(pdf);
                DrawTotals(pdf, endOfTableY + 8);
                DrawFooter(pdf);

                pdf.Close();
                return pdf.Buffer.BaseStream;
            }
        }

        private static string GetBasePath()
        {
            return Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        }

        private static void DrawHeaderBand(Sample8 pdf)
        {
            const double bandHeight = 44;
            pdf.SetFillColor(BrandDark);
            pdf.Rect(0, 0, pdf.W, bandHeight, "F");

            // Wordmark — "ego" white, "Pdf" coral, mimicking the logo.
            pdf.SetFont("Poppins", "", 28);
            pdf.SetXY(20, 12);
            pdf.SetTextColor(Color.White);
            var egoW = pdf.GetStringWidth("ego");
            pdf.Cell(egoW, 12, "ego");
            pdf.SetTextColor(BrandAccent);
            pdf.Cell(pdf.GetStringWidth("Pdf"), 12, "Pdf");

            // INVOICE title, right-aligned inside the band.
            pdf.SetFont("Poppins", "", 32);
            pdf.SetTextColor(Color.White);
            pdf.SetXY(20, 10);
            pdf.Cell(pdf.W - 40, 13, "INVOICE", "0", 0, AlignEnum.Right);

            pdf.SetFont("Helvetica", "", 9);
            pdf.SetTextColor(BandSubText);
            pdf.SetXY(20, 28);
            pdf.Cell(pdf.W - 40, 6, "#2026-001  ·  23 May 2026", "0", 0, AlignEnum.Right);
        }

        private static void DrawParties(Sample8 pdf)
        {
            const double bodyY = 60;
            const double rightX = 115;

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
            }, x: rightX, y: bodyY);
        }

        private static void DrawPartyBlock(Sample8 pdf, string label, string name, string[] addressLines,
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

        private static void DrawInvoiceMeta(Sample8 pdf)
        {
            const double metaY = 108;
            var cells = new (string label, string value)[]
            {
                ("INVOICE #", "2026-001"),
                ("ISSUED",   "23 May 2026"),
                ("DUE",      "22 Jun 2026"),
            };

            // Row() carves the meta strip into N equal slots without the
            // manual (W - margins) / N + offset arithmetic.
            var slots = pdf.Row(new Rect(20, metaY, pdf.W - 40, 12), cells.Length);

            for (int i = 0; i < cells.Length; i++)
            {
                var slot = slots[i];
                pdf.SetXY(slot.X, slot.Y);
                pdf.SetFont("Helvetica", "B", 8);
                pdf.SetTextColor(BrandAccent);
                pdf.Cell(slot.W, 5, cells[i].label);

                pdf.SetXY(slot.X, slot.Y + 6);
                pdf.SetFont("Helvetica", "B", 12);
                pdf.SetTextColor(BrandDark);
                pdf.Cell(slot.W, 6, cells[i].value);
            }
        }

        private static double DrawItems(Sample8 pdf)
        {
            const double tableY = 134;
            const double rowH = 10;
            const double colItem = 90;
            const double colQty = 20;
            const double colUnit = 30;
            const double colAmount = 30;

            // Header row — dark fill, white text.
            pdf.SetXY(20, tableY);
            pdf.SetFillColor(BrandDark);
            pdf.SetTextColor(Color.White);
            pdf.SetFont("Helvetica", "B", 9);
            pdf.Cell(colItem,   9, "ITEM",       "0", 0, AlignEnum.Left,  true);
            pdf.Cell(colQty,    9, "QTY",        "0", 0, AlignEnum.Right, true);
            pdf.Cell(colUnit,   9, "UNIT PRICE", "0", 0, AlignEnum.Right, true);
            pdf.Cell(colAmount, 9, "AMOUNT",     "0", 1, AlignEnum.Right, true);

            // Rows — thin border-bottom in muted gray.
            var items = new (string desc, string qty, string unit, string amount)[]
            {
                ("Custom report templates",         "5", "$120.00", "$600.00"),
                ("PDF rendering license (annual)",  "1", "$480.00", "$480.00"),
                ("On-site implementation support",  "8",  "$95.00", "$760.00"),
            };

            pdf.SetFont("Helvetica", "", 10);
            pdf.SetTextColor(BrandDark);
            pdf.SetDrawColor(RowBorder);
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

        private static void DrawTotals(Sample8 pdf, double startY)
        {
            // Align the value column with the AMOUNT column of the items
            // table (right edge at x = page width - right margin = 190 mm).
            const double labelW = 50;
            const double valueW = 30;
            double labelX = pdf.W - pdf.RightMargin - labelW - valueW;

            // Subtotal
            pdf.SetXY(labelX, startY);
            pdf.SetFont("Helvetica", "", 10);
            pdf.SetTextColor(TextMuted);
            pdf.Cell(labelW, 6, "Subtotal", "0", 0, AlignEnum.Right);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(valueW, 6, "$1,840.00", "0", 1, AlignEnum.Right);

            // VAT
            pdf.SetX(labelX);
            pdf.SetTextColor(TextMuted);
            pdf.Cell(labelW, 6, "VAT 20%", "0", 0, AlignEnum.Right);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(valueW, 6, "$368.00", "0", 1, AlignEnum.Right);

            // Total — bigger, accent color on value.
            pdf.Y += 2;
            pdf.SetX(labelX);
            pdf.SetFont("Helvetica", "B", 13);
            pdf.SetTextColor(BrandDark);
            pdf.Cell(labelW, 9, "TOTAL", "0", 0, AlignEnum.Right);
            pdf.SetTextColor(BrandAccent);
            pdf.Cell(valueW, 9, "$2,208.00", "0", 1, AlignEnum.Right);
        }

        private static void DrawFooter(Sample8 pdf)
        {
            const double footerY = 268;

            // Thin coral divider above the footer.
            pdf.SetDrawColor(BrandAccent);
            pdf.SetLineWidth(0.4);
            pdf.Line(20, footerY - 4, pdf.W - 20, footerY - 4);

            // Payment details (left).
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

            // Thank-you note (right).
            pdf.SetFont("Helvetica", "BI", 14);
            pdf.SetTextColor(BrandAccent);
            pdf.SetXY(120, footerY + 3);
            pdf.Cell(pdf.W - 140, 10, "Thank you for your business", "0", 0, AlignEnum.Right);
        }

        public void WriteHtml(string html, int height = 5)
        {
            html = html.Replace("\n", string.Empty).Replace("\t", string.Empty).Replace("\r", string.Empty);

            int l;
            do
            {
                l = html.Length;
                html = html.Replace("  ", " ");
            } while (l > html.Length);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            WriteChildNode(doc.DocumentNode.ChildNodes, height);
        }

        public void WriteChildNode(HtmlNodeCollection nodes, int height)
        {
            foreach (var node in (nodes))
            {
                switch (node.Name)
                {
                    case "#text":
                        Write(height, node.InnerText);
                        break;
                    case "u":
                        {
                            string style = FontStyle;
                            SetFont("", style + "U");
                            WriteChildNode(node.ChildNodes, height);
                            SetFont("", style);
                        }
                        break;
                    case "i":
                        {
                            string style = FontStyle;
                            SetFont("", style + "I");
                            WriteChildNode(node.ChildNodes, height);
                            SetFont("", style);
                        }
                        break;
                    case "b":
                        {
                            string style = FontStyle;
                            SetFont("", style + "B");
                            WriteChildNode(node.ChildNodes, height);
                            SetFont("", style);
                        }
                        break;
                    case "a":
                        var url = node.GetAttributeValue("href", string.Empty);
                        PutLink(url, node.InnerText, height);
                        break;
                    case "br":
                        Ln();
                        break;
                    default:
                        if (node.ChildNodes.Count > 0)
                        {
                            WriteChildNode(node.ChildNodes, height);
                        }
                        break;
                }
            }
        }

        public void SetStyle(string tag, bool enable)
        {
            tag = tag.ToUpper();
            if (!_tagCount.ContainsKey(tag))
            {
                _tagCount[tag] = 0;
            }

            _tagCount[tag] = _tagCount[tag] + (enable ? 1 : -1);
            string style = string.Empty;

            foreach (var token in new[] { "B", "I", "U" })
            {
                if (_tagCount.ContainsKey(token) && _tagCount[token] > 0)
                {
                    style += token;
                }
            }
            SetFont(string.Empty, style);
        }

        public void PutLink(string href, string text, int height = 5)
        {
            SetTextColor(220, 50, 50);
            SetTextColor(0, 0, 255);
            SetStyle("U", true);
            Write(height, text, href);
            SetStyle("U", false);
            SetTextColor(0);
        }
    }
}
