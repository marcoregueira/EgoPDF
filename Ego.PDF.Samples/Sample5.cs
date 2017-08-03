using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ego.PDF.Data;

namespace Ego.PDF.Samples
{
    public class Sample5 : FPdf
    {
        public Sample5(string file) : base(file)
        {
        }

        public string LocalPath { get; set; }
        private List<string[]> Data { get; set; }


        public static FPdf GetSample(string file, string path)
        {
            var pdf = new Sample5(file);
            string[] header = { "Country", "Capital", "Area (sq km)", "Pop. (thousands)" };
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
            file = Path.Combine(LocalPath, file);

            Data = new List<string[]>();
            FileStream stream = File.OpenRead(file);
            StreamReader reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (line != null) Data.Add(line.Split(new[] { ';' }));
            }
            reader.Dispose();
        }

        public void BasicTable(IList<string> headers)
        {
            foreach (var header in headers)
            {
                Cell(40, 7, header, "1");
            }
            Ln();
            Data[0][0] += " loren tal y pas\ncuasld lde duarte";
            foreach (var row in Data)
            {
                var height = row.Max(x => CellMeasure(40, 6, x));

                foreach (var col in row)
                {
                    BoxedText(40, 6, height, col, "1", 0, AlignEnum.Left, false);
                }

                Ln();
            }
        }

        public void ImprovedTable(IList<string> headers)
        {
            int[] w = { 40, 35, 40, 45 };
            for (int i = 0; i < headers.Count(); i++)
            {
                Cell(w[i], 7, headers[i], "1", 0, AlignEnum.Center);
            }
            Ln();
            foreach (var row in Data)
            {
                Cell(w[0], 6, row[0], "LR");
                Cell(w[1], 6, row[1], "LR");
                Cell(w[2], 6, string.Format("{0:N0}", Convert.ToInt32(row[2])), "LR", 0, AlignEnum.Right);
                Cell(w[3], 6, string.Format("{0:N0}", Convert.ToInt32(row[3])), "LR", 0, AlignEnum.Right);
                Ln();
            }
            Cell(w.Sum(), 0, "", "T");
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
                Cell(w[i], 7, headers[i], "1", 0, AlignEnum.Center, true);
            }
            Ln();
            SetFillColor(224, 235, 255);
            SetTextColor(0);
            SetFont("");

            bool fill = false;
            foreach (var row in Data)
            {
                Cell(w[0], 6, row[0], "LR", 0, AlignEnum.Left, fill);
                Cell(w[1], 6, row[1], "LR", 0, AlignEnum.Left, fill);
                Cell(w[2], 6, string.Format("{0:N0}", Convert.ToInt32(row[2])), "LR", 0, AlignEnum.Right, fill);
                Cell(w[3], 6, string.Format("{0:N0}", Convert.ToInt32(row[3])), "LR", 0, AlignEnum.Right, fill);
                Ln();
                fill = !fill;
            }
            Cell(w.Sum(), 0, "", "T");
        }
    }
}