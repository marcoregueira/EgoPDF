using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Ego.PDF;
using Ego.PDF.Data;

namespace Ego.PDF.Samples
{
    public class Sample2: FPdf
    {
        string ImageFile { get; set; }

        public static FPdf GetSample(string imagefile)
        {
            var p = new Sample2();
            p.ImageFile = imagefile;
            p.AliasNbPages();
            p.AddPage();
            p.SetFont("Times", "", 12);
            for (int i = 1; i <= 40; i++)
            {
                p.Cell(0, 10, "Printing line number " + i.ToString(), "0", 1);
            }
            return p;
       
        }

        public override void Header()
        {
            base.Header();
            this.Image(this.ImageFile, 10, 6, 30, 0);
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
}
