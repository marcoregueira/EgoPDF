using Ego.PDF.Data;
using System.IO;

namespace Ego.PDF.Samples
{
    public class Sample1
    {
        public static Stream GetSample(string filePath)
        {
            using (var pdf = new FPdf(filePath))
            {
                pdf.AddPage(PageSizeEnum.A4);
                pdf.SetFont("Arial", "", 16);
                pdf.Cell(40, 10, "Hello World!");
                pdf.Close();
                return pdf.Buffer.BaseStream;
            }
        }
    }
}