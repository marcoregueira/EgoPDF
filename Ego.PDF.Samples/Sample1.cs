using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Ego.PDF;
using Ego.PDF.Data;

namespace Ego.PDF.Samples
{
    public class Sample1
    {
        public static FPdf GetSample()
        {
            FPdf pdf = new FPdf();
            pdf.AddPage(PageSizeEnum.A4);
            pdf.SetFont("Arial", "", 16);
            pdf.Cell(40, 10, "Hello");
            return pdf;
        }
    }
}
