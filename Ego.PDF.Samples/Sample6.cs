using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
            //pdf.SetFont("", "U");
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

        int i;
        public void WriteHtml(string html)
        {
            html = html.Replace('\n', ' ').Replace("\t", " ");
            Regex r = new Regex("/<(.*)>/U");
            string[] fragments = r.Split(html);
            foreach (var fragment in fragments)
            {
                if ( i % 2 == 0)
                {
                    if (!string.IsNullOrEmpty(this.href))
                    {
                    }
                }
                else
                {
                }
            }
        }

        public void CloseTag(string tag)
        {
            if (tag == "B" || tag == "I" || tag == "U")
                this.SetStyle(tag, false);
            if (tag == "A")
                this.href = string.Empty;
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
            this.SetTextColor(0, 0, 255);
            this.SetStyle("U", true);
            this.Write(5, text, href);
            this.SetStyle("U", false);
            this.SetTextColor(0);
        }
    }
}
