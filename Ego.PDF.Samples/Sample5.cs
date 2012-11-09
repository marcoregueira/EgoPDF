using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Ego.PDF;
using Ego.PDF.Data;

namespace Ego.PDF.Samples
{
    public class Sample5 : FPdf
    {
        double y0;
        int col = 0;
        public string LocalPath { get; set; }
        List<string[]> Data { get; set; }


        public static FPdf GetSample(string path)
        {
            var pdf = new Sample5();
            string[] header = {"Country", "Capital", "Area (sq km)", "Pop. (thousands)"};
            pdf.LocalPath = path;

            pdf.LoadData("countries.txt");
            pdf.SetFont("Arial", string.Empty, 14);
            pdf.AddPage();
            pdf.BasicTable(header);
            pdf.AddPage();
            pdf.ImprovedTable(header);
            pdf.AddPage();
            pdf.FancyTable(header);
            return pdf;
        }

        public void LoadData(string file)
        {
            file = System.IO.Path.Combine(this.LocalPath, file);

            this.Data = new List<string[]>();
            FileStream stream = File.OpenRead(file);
            StreamReader reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                Data.Add(line.Split(new char[] { ';' }));
            }
            reader.Close();
        }

        public void BasicTable(IList<string> headers)
        {
            foreach (var header in headers)
            {
                this.Cell(40, 7, header, "1");
            }
            this.Ln();

            foreach (var row in this.Data)
            {
                foreach (var col in row)
                {
                    this.Cell(40, 6, col, "1");
                }
                this.Ln();
            }
        }

        public void ImprovedTable(IList<string> headers)
        {
            int[] w = { 40, 35, 40, 45 };
            for (int i=0; i < headers.Count(); i++)
            {
                this.Cell(w[i], 7, headers[i], "1", 0, AlignEnum.Center);
            }
            this.Ln();
            foreach (var row in this.Data)
            {
                this.Cell(w[0], 6, row[0], "LR");
                this.Cell(w[1], 6, row[1], "LR");
                this.Cell(w[2], 6, string.Format("{0:N0}", Convert.ToInt32(row[2])), "LR", 0, AlignEnum.Right);
                this.Cell(w[3], 6, string.Format("{0:N0}", Convert.ToInt32(row[3])), "LR", 0, AlignEnum.Right);
                this.Ln();
            }
            this.Cell(w.Sum(), 0, "", "T");
        }

        public void FancyTable(IList<string> headers)
        {
            SetFillColor(255, 0, 0);
            SetTextColor(255);
            SetDrawColor(128, 0, 0);
            SetLineWidth(0.3);
            SetFont("", "B");

            int[] w = { 40, 35, 40, 45 };
            for (int i = 0; i < headers.Count(); i++)
            {
                this.Cell(w[i], 7, headers[i], "1", 0, AlignEnum.Center, true);
            }
            this.Ln();
            SetFillColor(224,235,255);
            SetTextColor(0);
            SetFont("");
            
            bool fill = false;
            foreach (var row in this.Data)
            {
                this.Cell(w[0], 6, row[0], "LR", 0, AlignEnum.Left, fill);
                this.Cell(w[1], 6, row[1], "LR", 0, AlignEnum.Left, fill);
                this.Cell(w[2], 6, string.Format("{0:N0}", Convert.ToInt32( row[2])), "LR", 0, AlignEnum.Right, fill);
                this.Cell(w[3], 6, string.Format("{0:N0}", Convert.ToInt32( row[3])), "LR", 0, AlignEnum.Right, fill);
                this.Ln();
                fill = !fill;
            }
            this.Cell(w.Sum(), 0, "", "T");
        }
    }
}