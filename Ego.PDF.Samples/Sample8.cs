using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Ego.PDF;
using Ego.PDF.Data;

namespace Ego.PDF.Samples
{
    public class Sample8 : FPdf
    {
        private Dictionary<string, int> TagCount = new Dictionary<string, int>();
        public string href;
        private string Path { get; set; }

        public static FPdf GetSample(string path)
        {
            var pdf = new Sample8();

            pdf.AddPage();

            pdf.SetMargins(20, 20, 20);
            pdf.SetFont("Arial", string.Empty, 14);

            pdf.Image(System.IO.Path.Combine(path, "logo.png"), 10, 12, 30, 0, ImageTypeEnum.Default, "http://www.fpdf.org");

            var rowHeader = new TableRow(22, 120)
                                                    .SetBorder("0")
                                                    .SetCellHeight(5);
            //pdf.PaintRow(rowHeader, "", @"Strada General Traian Moșoiu 24, <<--- characted not supported 
            pdf.Ln(3);
            pdf.SetFontSize(10);
            pdf.PaintRow(rowHeader, "", @"Strada General Traian Mosoiu 24, 
Bran 507025, 
Romania");

            pdf.SetY(pdf.TopMargin);
            pdf.SetFontSize(32);

            var alignRight = new TableRow(pdf.CurrentPageSize.Width - pdf.LeftMargin - pdf.RightMargin).SetBorder("");
            alignRight.Cells.Last().Align = AlignEnum.Right;
            pdf.PaintRow(alignRight, "INVOICE");

            pdf.SetY(50);
            pdf.SetFontSize(25);
            pdf.WriteHtml("<b>Happy Teeth</b>", 12);
            pdf.Ln();
            pdf.SetFontSize(14);
            pdf.WriteHtml("<i>Medical Care</i>", 8);
            pdf.Ln();
            pdf.SetFont("Arial", string.Empty, 10);

            pdf.Write(6, "VAT number: RO34.123.666-Z");
            pdf.Ln();

            pdf.SetY(50);
            var clientRow = new TableRow(100, 50).SetBorder();
            pdf.PaintRow(clientRow, "", "CLIENT");
            pdf.PaintRow(clientRow, "", "Jonathan Harker");
            clientRow.CellHeight = 4;
            pdf.PaintRow(clientRow, "", "Lyndhurst Rd");
            pdf.PaintRow(clientRow, "", "Exeter");
            pdf.PaintRow(clientRow, "", "EX2 4PA");
            pdf.PaintRow(clientRow, "", "UK");




            pdf.SetY(100);

            DecoratorLine(pdf);

            var row = new TableRow(0.25, 0.95, 0.2, 0.3, 0.4);

            row.Cells[4].Align = AlignEnum.Right;
            row.Cells[3].Align = AlignEnum.Right;
            row.Cells[2].Align = AlignEnum.Right;
            row.Cells[1].Align = AlignEnum.Justified;

            row.NormalizeWidths(pdf.CurrentPageSize.Width - pdf.LeftMargin - pdf.RightMargin);
            pdf.PaintRow(row, "CODE", "DESCRIPTION", "QTY", "UNIT PRICE", "PRICE");
            pdf.PaintRow(row, "00002", "Journey to the Center of the Earth", "2", "$7.95", "$15.90");
            pdf.PaintRow(row, "00002", "Around the World in 80 Days", "1", "$7.95", "$7.95");
            pdf.PaintRow(row, "00002", "The Misterious Island", "1", "$7.95", "$7.95");



            for (int i = 0; i < 10; i++)
            {
                pdf.PaintRow(row);

            }

            var totalsRow = new TableRow(1, 0.4, 0.3, 0.4)
                .NormalizeWidths(pdf.CurrentPageSize.Width - pdf.LeftMargin - pdf.RightMargin)
                .SetAlign(AlignEnum.Right);
            totalsRow.Cells[0].Border = "0";
            totalsRow.Cells[1].Border = "0";
            totalsRow.Cells[1].Align = AlignEnum.Left;

            pdf.PaintRow(totalsRow, "", "Invoiced amount", "", "$100.00");
            pdf.PaintRow(totalsRow, "", "Vat", "20%", "$20.00");
            pdf.PaintRow(totalsRow, "", "Due amount", "", "$20.00");

            pdf.Ln(2);
            DecoratorLine(pdf);

            string html = @"You can now easily print text mixing different styles: <b>bold</b>, <i>italic</i>,
                    <u>underlined</u>, or <b><i><u>all at once</u></i></b>!<br><br>You can also insert links on
                    text, such as <a href='http://www.fpdf.org'>www.fpdf.org</a>, or on an image: click on the logo.";


            pdf.SetFontSize(14);
            pdf.WriteHtml(html, 8);
            return pdf;
        }

        private static void DecoratorLine(Sample8 pdf)
        {
            var decorator = new TableRow(10, 1, 1, 0.5)
                .SetBorder()
                .SetCellHeight(2);
            decorator.NormalizeWidths(pdf.CurrentPageSize.Width - pdf.LeftMargin - pdf.RightMargin);
            decorator.Cells[0].Background = Color.Cyan;
            decorator.Cells[1].Background = Color.Green;
            decorator.Cells[2].Background = Color.Orange;
            decorator.Cells[3].Background = Color.YellowGreen;
            pdf.PaintRow(decorator, "", "", "", "", "");
            pdf.Ln(5);
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
            if (!TagCount.ContainsKey(tag))
            {
                TagCount[tag] = 0;
            }

            TagCount[tag] = TagCount[tag] + (enable ? 1 : -1);
            string style = string.Empty;

            foreach (var token in new[] { "B", "I", "U" })
            {
                if (TagCount.ContainsKey(token) && TagCount[token] > 0)
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