using Ego.PDF;
using Ego.PDF.Data;
using Ego.PDF.Font;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using Xunit;

namespace FontImport
{
    //TODO: MIGRATE TO .NET CORE 3.0
    public class FontGenerators
    {
        [Fact]
        public void GenerateArial()
        {
            var font2 = LoadFont("Calligrapher", Path.Combine(GetPath(), "CALLIGRA.TTF"));
            using (var pdf = new FPdf("samplefont.pdf"))
            {
                pdf.AddPage(PageSizeEnum.A4);
                //pdf.SetFont("Arial", "", 16);
                pdf.AddFont("Calligrapher", "", "file:///" + font2.FontFile);
                //pdf.SetFont("AR HERMANN", "", 16);
                pdf.Cell(40, 10, "Enjoy new fonts with FPDF!");
                pdf.Close();
            }
        }

        private string GetPath()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///","");
            return Path.GetDirectoryName(codeBase);
        }

        public FontDefinition LoadFont(string name, string path)
        {
            var font = new GlyphTypeface(new Uri("file:///" + path));
            var fontData = new FontDefinition
            {
                FontType = FontTypeEnum.TrueType,
                FontFile = path,
                Name = font.FamilyNames.FirstOrDefault().Value.ToLower(),
            };

            foreach (var advance in font.AdvanceWidths)
            {
                var leftBearing = font.LeftSideBearings[advance.Key];
                var rightBearing = font.RightSideBearings[advance.Key];
                fontData.Widths[Convert.ToChar(advance.Key).ToString()] = advance.Value * 1000;
            }

            FontBuilder.Fonts[name.ToLower()] = fontData;

            return fontData;
        }
    }
}
