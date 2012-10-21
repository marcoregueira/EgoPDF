using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Ego.PDF;
using Ego.PDF.Data;

namespace Ego.PDF.WindowsFormsTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Sample1();
            Sample2();
            Sample3();
            Sample4();
            Close();
        }

        public void Sample1()
        {
            FPdf pdf = new FPdf();
            pdf.FPDF_FONTPATH = "c:/";
            pdf.AddPage(PageSizeEnum.A4);
            pdf.SetFont("Arial", "", 16);
            pdf.Cell(40, 10, "Hello");
            pdf.Output("sample1.pdf", OutputDevice.SaveToFile);
        }

        public void Sample2()
        {
            PdfSample2 p = new PdfSample2();
            p.AliasNbPages();
            p.AddPage();
            //TODO: Times
            p.SetFont("Arial", "", 12);

            for (int i = 1; i <= 40; i++)
            {
                p.Cell(0, 10, "Printing line number " + i.ToString(), "0", 1);
            }
            p.Output("sample2.pdf", OutputDevice.SaveToFile);
        }

        class PdfSample2 : FPdf
        {
            public override void Header()
            {
                base.Header();
                this.Image("logo.png", 10, 6, 30, 0);
                this.SetFont("Arial", "B", 15);
                this.Cell(80);
                this.Cell(30, 10, "Title", "1", 0, AlignEnum.Center);
                this.Ln(20);
            }

            public override void Footer()
            {
                {
                    this.SetY(-15);
                    // Arial italic 8
                    this.SetFont("Arial", "I", 8);
                    // Page number
                    this.Cell(0, 10, "Page " + this.PageNo() + "/{nb}", "0", 0, AlignEnum.Center);
                }
            }
        }


        public void Sample3()
        {
            var pdf = new PdfSample3();
            pdf.AliasNbPages();
            pdf.SetTitle("20000 Leagues Under the Seas");
            pdf.SetAuthor("Jules Verne");
            pdf.PrintChapter(1, "A RUNAWAY REEF", "20k_c1.txt");
            pdf.PrintChapter(2, "THE PROS AND CONS", "20k_c2.txt");
            pdf.Output("sample3.pdf", OutputDevice.SaveToFile);
        }

        class PdfSample3 : FPdf
        {
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
            }

            public override void Footer()
            {
                base.Footer();
                this.SetY(-15);
                this.SetFont("Arial", "I", 15);
                SetTextColor(128);
                // Page number
                this.Cell(0, 10, "Page " + this.PageNo() + "/{nb}", "0", 0, AlignEnum.Center);
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
            }

            public void ChapterBody(string file)
            {
                // Read text file
                string txt = System.IO.File.ReadAllText(file,System.Text.Encoding.UTF8);
                // Times 12
                //TODO: TIMES
                this.SetFont("Times", "", 12);
                // Output justified text
                this.MultiCell(0, 5, txt);
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

        public void Sample4()
        {
            var pdf = new PdfSample4();
            pdf.AliasNbPages();
            pdf.SetTitle("20000 Leagues Under the Seas");
            pdf.SetAuthor("Jules Verne");
            pdf.PrintChapter(1, "A RUNAWAY REEF", "20k_c1.txt");
            pdf.PrintChapter(2, "THE PROS AND CONS", "20k_c2.txt");
            pdf.Output("sample4.pdf", OutputDevice.SaveToFile);
        }

        class PdfSample4 : FPdf
        {
            double y0;
            int col = 0;

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
                string txt = System.IO.File.ReadAllText(file, System.Text.Encoding.UTF8);
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
}
