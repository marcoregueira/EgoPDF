using Ego.PDF.Data;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ego.PDF.Samples
{
    public class Sample6 : FPdf
    {
        private readonly Dictionary<string, int> _tagCount = new Dictionary<string, int>();

        private Sample6(string file) : base(file)
        {
        }

        public static Stream GetSample(string file, string path)
        {
            using (var pdf = new Sample6(file))
            {
                var html = @"You can now easily print text mixing different styles: <b>bold</b>, <i>italic</i>,
                    <u>underlined</u>, or <b><i><u>all at once</u></i></b>!<br><br>You can also insert links on
                    text, such as <a href='http://www.fpdf.org'>www.fpdf.org</a>, or on an image: click on the logo.";

                // First page
                pdf.AddPage();
                pdf.SetFont("Arial", "", 20);
                pdf.Write(5, "To find out what's new in this tutorial, click ");
                pdf.SetFont("", "U");
                var link = pdf.AddLink();
                pdf.Write(5, "here", link.InternalLink);
                pdf.SetFont(string.Empty);
                // Second page
                pdf.AddPage();
                pdf.SetLink(link);
                pdf.Image(System.IO.Path.Combine(path, "logo.png"), 10, 12, 30, 0, ImageTypeEnum.Default, "http://www.fpdf.org");
                pdf.SetLeftMargin(45);
                pdf.SetFontSize(14);
                pdf.WriteHtml(html);
                pdf.Close();
                return pdf.Buffer.BaseStream;
            }
        }

        public void WriteHtml(string html)
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
            WriteChildNode(doc.DocumentNode.ChildNodes);
        }

        public void WriteChildNode(HtmlNodeCollection nodes)
        {
            foreach (var node in (nodes))
            {
                if (node.Name == "#text")
                {
                    Write(5, node.InnerText);
                }
                else if (node.Name == "u")
                {
                    string style = FontStyle;
                    SetFont("", style + "U");
                    WriteChildNode(node.ChildNodes);
                    SetFont("", style);
                }
                else if (node.Name == "i")
                {
                    string style = FontStyle;
                    SetFont("", style + "I");
                    WriteChildNode(node.ChildNodes);
                    SetFont("", style);
                }
                else if (node.Name == "b")
                {
                    string style = FontStyle;
                    SetFont("", style + "B");
                    WriteChildNode(node.ChildNodes);
                    SetFont("", style);
                }
                else if (node.Name == "a")
                {
                    var url = node.GetAttributeValue("href", string.Empty);
                    PutLink(url, node.InnerText);
                }
                else if (node.Name == "br")
                {
                    Ln();
                }
                else
                {
                    if (node.ChildNodes.Count > 0)
                    {
                        WriteChildNode(node.ChildNodes);
                    }
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

        public void PutLink(string href, string text)
        {
            SetTextColor(220, 50, 50);
            SetTextColor(0, 0, 255);
            SetStyle("U", true);
            Write(5, text, href);
            SetStyle("U", false);
            SetTextColor(0);
        }
    }
}