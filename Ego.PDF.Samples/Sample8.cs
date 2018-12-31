using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Ego.PDF;
using Ego.PDF.Data;
using Microsoft.Xna.Framework;

namespace Ego.PDF.Samples
{
    public class Sample8 : FPdf
    {
        private readonly Dictionary<string, int> _tagCount = new Dictionary<string, int>();

        private Sample8(string file) : base(file)
        {
        }

        public static FPdf GetSample(string file, string path)
        {
            using (var pdf = new Sample8(file))
            {

                pdf.AddPage();

                pdf.SetMargins(20, 20, 20);
                pdf.SetFont("Courier", string.Empty, 14);

                pdf.Image(System.IO.Path.Combine(path, "logo.png"), 10, 12, 30, 0, ImageTypeEnum.Default, "http://www.fpdf.org");

                var rowHeader = new TableRow(22, 120)
                                                        .SetBorder("0")
                                                        .SetCellHeight(5);
                //pdf.PrintRow(rowHeader, "", @"Strada General Traian Moșoiu 24, <<--- characted not supported 
                pdf.Ln(3);
                pdf.SetFontSize(10);
                pdf.PrintRow(rowHeader, "", @"Strada General Traian Mosoiu 24, 
Bran 507025, 
Romania");

                pdf.SetY(pdf.TopMargin);
                pdf.SetFontSize(32);

                var alignRight = new TableRow(pdf.CurrentPageSize.Width - pdf.LeftMargin - pdf.RightMargin).SetBorder("");
                alignRight.Cells.Last().Align = AlignEnum.Right;
                pdf.PrintRow(alignRight, "INVOICE");

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
                pdf.PrintRow(clientRow, "", "CLIENT");
                pdf.PrintRow(clientRow, "", "Jonathan Harker");
                clientRow.CellHeight = 4;
                pdf.PrintRow(clientRow, "", "Lyndhurst Rd");
                pdf.PrintRow(clientRow, "", "Exeter");
                pdf.PrintRow(clientRow, "", "EX2 4PA");
                pdf.PrintRow(clientRow, "", "UK");




                pdf.SetY(100);

                DecoratorLine(pdf);

                var row = new TableRow(0.25, 0.95, 0.2, 0.3, 0.4);

                row.Cells[4].Align = AlignEnum.Right;
                row.Cells[3].Align = AlignEnum.Right;
                row.Cells[2].Align = AlignEnum.Right;
                row.Cells[1].Align = AlignEnum.Justified;

                row.NormalizeWidths(pdf.CurrentPageSize.Width - pdf.LeftMargin - pdf.RightMargin);
                pdf.PrintRow(row, "CODE", "DESCRIPTION", "QTY", "UNIT PRICE", "PRICE");
                pdf.PrintRow(row, "00002", "Journey to the Center of the Earth", "2", "$7.95", "$15.90");
                pdf.PrintRow(row, "00002", "Around the World in 80 Days", "1", "$7.95", "$7.95");
                pdf.PrintRow(row, "00002", "The Misterious Island", "1", "$7.95", "$7.95");



                for (int i = 0; i < 10; i++)
                {
                    pdf.PrintRow(row);

                }

                var totalsRow = new TableRow(1, 0.4, 0.3, 0.4)
                    .NormalizeWidths(pdf.CurrentPageSize.Width - pdf.LeftMargin - pdf.RightMargin)
                    .SetAlign(AlignEnum.Right);
                totalsRow.Cells[0].Border = "0";
                totalsRow.Cells[1].Border = "0";
                totalsRow.Cells[1].Align = AlignEnum.Left;

                pdf.PrintRow(totalsRow, "", "Invoiced amount", "", "$100.00");
                pdf.PrintRow(totalsRow, "", "Vat", "20%", "$20.00");
                pdf.PrintRow(totalsRow, "", "Due amount", "", "$120.00");


                pdf.Ln(16);

                var points = new[]
                {
                new DrawingPoint(0, 0),
                new DrawingPoint(5, -5),
                new DrawingPoint(5, -15),
                new DrawingPoint(120, -15),
                new DrawingPoint(120, 15),
                new DrawingPoint(5, 15),
                new DrawingPoint(5, 5),
                new DrawingPoint(0, 0)
            };

                foreach (var drawingPoint in points)
                {
                    drawingPoint.X = drawingPoint.X + pdf.LeftMargin + 50;
                    drawingPoint.Y = drawingPoint.Y + 5;
                }

                pdf.SetDrawColor(Color.Green);
                pdf.DrawArea(Color.Green, 0.00, points);

                pdf.SavePos();

                pdf.SetFontSize(24);
                pdf.WriteHtml("<b>Thank</b>", 6);
                pdf.WriteHtml("<b><i>you!</i></b>", 18);

                pdf.Y -= 20;
                pdf.X = pdf.LeftMargin + 70;
                pdf.SetTextColor(Color.White);
                pdf.SetFontSize(25);
                pdf.Cell(100, 40, "45,000.00 €", "", 0, AlignEnum.Right);
                pdf.Ln(10);
                pdf.SetFontSize(12);
                pdf.X = pdf.LeftMargin + 70;
                pdf.Cell(100, 40, "Pay by transfer to ES00 0300 0303 00303 03030", "", 0, AlignEnum.Right);
                pdf.Ln(6);
                pdf.SetFontSize(12);
                pdf.X = pdf.LeftMargin + 70;
                pdf.Cell(100, 40, "SWIFT ESW-123453", "", 0, AlignEnum.Right);

                pdf.SetY(270);
                DecoratorLine(pdf);

                return pdf.Close();
            }
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
            pdf.PrintRow(decorator, "", "", "", "", "");
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