using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ego.PDF;
using Ego.PDF.Data;

namespace Ego.PDF.Samples
{
    public class Sample2 : FPdf
    {
        private string ImageFile { get; set; }

        public static FPdf GetSample(string imagefile)
        {
            var p = new Sample2();
            p.ImageFile = imagefile;
            p.AliasNbPages();
            p.AddPage();
            p.SetFont("Times", "", 12);
            for (var i = 1; i <= 40; i++)
            {
                p.Cell(0, 10, "Printing line number " + i, "0", 1);
            }
            return p;
        }

        public override void Header()
        {
            base.Header();
            Image(ImageFile, 10, 6, 30);
            SetFont("Arial", "B", 15);
            Cell(80);
            Cell(30, 10, "Title", "1", 0, AlignEnum.Center);
            Ln(20);
        }

        public override void Footer()
        {
            {
                SetY(-15);
                // Arial italic 8
                SetFont("Arial", "I", 8);
                // Page number
                Cell(0, 10, "Page " + PageNo() + "/{nb}", "0", 0, AlignEnum.Center);
            }
        }
    }
}