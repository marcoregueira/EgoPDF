using Ego.PDF.Data;

namespace Ego.PDF.Samples
{
    public class Sample1
    {
        public static FPdf GetSample()
        {
            var pdf = new FPdf();
            pdf.AddPage(PageSizeEnum.A4);
            pdf.SetFont("Arial", "", 16);
            pdf.Cell(40, 10, "Hello World!");
            return pdf;
        }
    }
}