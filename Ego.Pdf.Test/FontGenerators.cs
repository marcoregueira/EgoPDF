using Ego.PDF;
using Ego.PDF.Data;
using Ego.PDF.Font;
using SkiaSharp;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FontImport
{
    public class FontGenerators
    {

        private string GetPath()
        {
            var codeBase = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(codeBase);
        }

        [Fact]
        public void GenerateArial()
        {
            //var font2 = LoadFont("Calligrapher", Path.Combine(GetPath(), "Ceviche/CevicheOne-Regular.ttf"));
            using var pdf = new FPdf(DateTime.Now.Ticks + ".samplefont.pdf");
            pdf.LoadFont("Calligrapher", Path.Combine(GetPath(), "calligra.ttf"));
            pdf.LoadFont("Ceviche", Path.Combine(GetPath(), "Ceviche/CevicheOne-Regular.ttf"));
            pdf.LoadFont("Roboto", Path.Combine(GetPath(), "Roboto/RobotoSlab-VariableFont_wght.ttf"));
            pdf.LoadFont("mytype", Path.Combine(GetPath(), "mytype/mytype.ttf"));
            
            pdf.AddPage(PageSizeEnum.A4);
            //pdf.SetFont("Times", "", 16);
            //pdf.AddFont("Calligrapher", "", "file:///" + font2.FontFile);
            pdf.AddFont("mytype", "");
            pdf.SetFont("mytype", "", 45);
            pdf.Cell(40, 10, "Enjoy new fonts with FPDF!");
            pdf.Close();
        }
    }
}
