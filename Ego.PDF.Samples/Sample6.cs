using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using HtmlAgilityPack;

using Ego.PDF;
using Ego.PDF.Data;

namespace Ego.PDF.Samples
{
    public class Sample6 : FPdf
    {
        string Path { get; set; }
        public string href;
        Dictionary<string, int>TagCount=new Dictionary<string,int>();

        public static FPdf GetSample(string path)
        {
            var pdf = new Sample6();
            string html = @"You can now easily print text mixing different styles: <b>bold</b>, <i>italic</i>,
                    <u>underlined</u>, or <b><i><u>all at once</u></i></b>!<br><br>You can also insert links on
                    text, such as <a href='http://www.fpdf.org'>www.fpdf.org</a>, or on an image: click on the logo.";

            // First page
            pdf.AddPage();
            pdf.SetFont("Arial", "", 20);
            pdf.Write(5, "To find out what's new in this tutorial, click ");
            pdf.SetFont("", "U");
            var link = pdf.AddLink();
            pdf.Write(5, "here", link);
            pdf.SetFont(string.Empty);
            // Second page
            pdf.AddPage();
            pdf.SetLink(link);
            pdf.Image( System.IO.Path.Combine( path, "logo.png"), 10, 12, 30, 0, ImageTypeEnum.Default, "http://www.fpdf.org");
            pdf.SetLeftMargin(45);
            pdf.SetFontSize(14);
            pdf.WriteHtml(html);
            return pdf;
        }

        public void WriteHtml(string html)
        {
            html = html.Replace("\n", string.Empty).Replace("\t", string.Empty).Replace("\r", string.Empty);

            int l;
            do
            {
                l = html.Length;
                html = html.Replace("  ", " ");
            }
            while (l > html.Length);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            WriteChildNode(doc.DocumentNode.ChildNodes);
        }

        public void WriteChildNode(HtmlAgilityPack.HtmlNodeCollection nodes)
        {
            foreach (var node in (nodes))
            {
                if (node.Name == "#text")
                {
                    this.Write(5, node.InnerText);
                }
                else if (node.Name == "u")
                {
                    string style = this.FontStyle;
                    this.SetFont("", style + "U");
                    WriteChildNode(node.ChildNodes);
                    this.SetFont("", style);
                }
                else if (node.Name == "i")
                {
                    string style = this.FontStyle;
                    this.SetFont("", style + "I");
                    WriteChildNode(node.ChildNodes);
                    this.SetFont("", style);
                }
                else if (node.Name == "b")
                {
                    string style = this.FontStyle;
                    this.SetFont("", style + "B");
                    WriteChildNode(node.ChildNodes);
                    this.SetFont("", style);
                }
                else if (node.Name == "a")
                {
                    var url=node.GetAttributeValue("href", string.Empty);
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
            if (!this.TagCount.ContainsKey(tag))
            {
                this.TagCount[tag] = 0;
            }

            this.TagCount[tag] = this.TagCount[tag] + (enable ? 1 : -1);
            string style = string.Empty;

            foreach (var token in new string[] { "B", "I", "U" })
            {
                if (this.TagCount.ContainsKey(token) && this.TagCount[token] > 0)
                {
                    style += token;
                }
            }
            this.SetFont(string.Empty, style);
        }

        public void PutLink(string href, string text)
        {
            this.SetTextColor(220, 50, 50);
            this.SetTextColor(0, 0, 255);
            this.SetStyle("U", true);
            this.Write(5, text, href);
            this.SetStyle("U", false);
            this.SetTextColor(0);
        }
    }
}
