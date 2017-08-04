using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ego.PDF;
using Ego.PDF.Data;

namespace Ego.PDF.Samples
{
    public class Sample4 : FPdf
    {
        private int _col;

        private Sample4(string file) : base(file)
        {
        }

        public string LocalPath { get; set; }

        public static FPdf GetSample(string file, string path)
        {
            var pdf = new Sample4(file) { LocalPath = path };

            pdf.AliasNbPages();
            pdf.SetTitle("20000 Leagues Under the Seas");
            pdf.SetAuthor("Jules Verne");
            pdf.PrintChapter(1, "A RUNAWAY REEF", "20k_c1.txt");
            pdf.PrintChapter(2, "THE PROS AND CONS", "20k_c2.txt");
            return pdf.Close();
        }

        public override void Header()
        {
            base.Header();
            string title = "20000 Leagues Under the Seas";
            SetFont("Arial", "B", 15);
            var w = GetStringWidth(title) + 6;
            SetX((210 - w) / 2);
            SetDrawColor(0, 80, 180);
            SetFillColor(230, 230, 0);
            SetTextColor(220, 50, 50);
            SetLineWidth(1);
            Cell(w, 9, title, "1", 1, AlignEnum.Center, true, null);
            Ln(10);

            this.BelowHeaderY = GetY();
        }

        public override void Footer()
        {
            base.Footer();
            SetY(-15);
            SetFont("Arial", "I", 8);
            SetTextColor(128);
            // Page number
            Cell(0, 10, "Page " + PageNo() + "/{nb}", "0", 0, AlignEnum.Center);
        }

        public void SetCol(int col)
        {
            this._col = col;
            int x = 10 + col * 65;
            SetLeftMargin(x);
            SetX(x);
        }

        public override bool AcceptPageBreak()
        {
            if (_col < 2)
            {
                SetCol(_col + 1);
                SetY(BelowHeaderY);
                return false;
            }
            else
            {
                SetCol(0);
                return true;
            }
        }

        public void ChapterTitle(int num, string label)
        {
            // Arial 12
            SetFont("Arial", "", 12);
            // Background color
            SetFillColor(200, 220, 255);
            // Title
            Cell(0, 6, "Chapter " + num.ToString() + " : " + label, "0", 1, AlignEnum.Left, true, null);
            // Line break
            Ln(4);
            BelowHeaderY = GetY();
            SetCol(0);
        }

        public void ChapterBody(string file)
        {
            // Read text file
            string txt = File.ReadAllText(Path.Combine(LocalPath, file), Encoding.UTF8);
            // Times 12
            //TODO: TIMES
            SetFont("Times", "", 12);
            // Output justified text
            MultiCell(60, 5, txt);
            // Line break
            Ln();
            // Mention in italics
            SetFont("", "I");
            Cell(0, 5, "(end of excerpt)");
        }

        public void PrintChapter(int num, string title, string file)
        {
            AddPage();
            ChapterTitle(num, title);
            ChapterBody(file);
        }
    }
}