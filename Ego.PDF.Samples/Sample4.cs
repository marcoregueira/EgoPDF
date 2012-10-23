using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Ego.PDF;
using Ego.PDF.Data;

namespace Ego.PDF.Samples
{
    public class Sample4 : FPdf
    {
        double y0;
        int col = 0;
        public string LocalPath { get; set; }

        public static FPdf GetSample(string path)
        {
            var pdf = new Sample4();

            pdf.LocalPath = path;
            pdf.AliasNbPages();
            pdf.SetTitle("20000 Leagues Under the Seas");
            pdf.SetAuthor("Jules Verne");
            pdf.PrintChapter(1, "A RUNAWAY REEF", "20k_c1.txt");
            pdf.PrintChapter(2, "THE PROS AND CONS", "20k_c2.txt");
            return pdf;
        }

        public override void Header()
        {
            base.Header();
            string title = "20000 Leagues Under the Seas";
            SetFont("Arial", "B", 15);
            var w = GetStringWidth(title) + 6;
            SetX((210 - w) / 2);
            this.SetDrawColor(0, 80, 180);
            this.SetFillColor(230, 230, 0);
            this.SetTextColor(220, 50, 50);
            SetLineWidth(1);
            Cell(w, 9, title, "1", 1, AlignEnum.Center, true, null);
            Ln(10);
            this.y0 = this.GetY();
        }

        public override void Footer()
        {
            base.Footer();
            this.SetY(-15);
            this.SetFont("Arial", "I", 8);
            SetTextColor(128);
            // Page number
            this.Cell(0, 10, "Page " + this.PageNo() + "/{nb}", "0", 0, AlignEnum.Center);
        }

        public void SetCol(int col)
        {
            this.col = col;
            int x = 10 + col * 65;
            this.SetLeftMargin(x);
            this.SetX(x);
        }

        public override bool AcceptPageBreak()
        {
            if (this.col < 2)
            {
                this.SetCol(this.col + 1);
                this.SetY(this.y0);
                return false;
            }
            else
            {
                this.SetCol(0);
                return true;
            }
        }

        public void ChapterTitle(int num, string label)
        {
            // Arial 12
            this.SetFont("Arial", "", 12);
            // Background color
            this.SetFillColor(200, 220, 255);
            // Title
            this.Cell(0, 6, "Chapter " + num.ToString() + " : " + label, "0", 1, AlignEnum.Left, true, null);
            // Line break
            this.Ln(4);
            this.y0 = this.GetY();
            this.SetCol(0);
        }

        public void ChapterBody(string file)
        {
            // Read text file
            string txt = System.IO.File.ReadAllText(System.IO.Path.Combine(this.LocalPath, file), System.Text.Encoding.UTF8);
            // Times 12
            //TODO: TIMES
            this.SetFont("Times", "", 12);
            // Output justified text
            this.MultiCell(60, 5, txt);
            // Line break
            this.Ln();
            // Mention in italics
            this.SetFont("", "I");
            this.Cell(0, 5, "(end of excerpt)");
        }

        public void PrintChapter(int num, string title, string file)
        {
            this.AddPage();
            this.ChapterTitle(num, title);
            this.ChapterBody(file);
        }

    }
}