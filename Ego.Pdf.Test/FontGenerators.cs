using Ego.PDF;
using Ego.PDF.Data;
using Ego.PDF.Font;
using System.IO;
using System.Reflection;
using Xunit;

namespace FontImport
{
    //TODO: MIGRATE TO .NET CORE 3.0
    public class FontGenerators
    {

        private string GetPath()
        {
            var codeBase = this.GetType().GetTypeInfo().Assembly.CodeBase.Replace("file:///", "");
            return Path.GetDirectoryName(codeBase);
        }

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

        public FontDefinition LoadFont(string name, string path)
        {
            return null;
            //    var font = new GlyphTypeface(new Uri("file:///" + path));
            //    var fontData = new FontDefinition
            //    {
            //        FontType = FontTypeEnum.TrueType,
            //        FontFile = path,
            //        Name = font.FamilyNames.FirstOrDefault().Value.ToLower(),
            //    };
            //
            //    foreach (var advance in font.AdvanceWidths)
            //    {
            //        var leftBearing = font.LeftSideBearings[advance.Key];
            //        var rightBearing = font.RightSideBearings[advance.Key];
            //        fontData.Widths[Convert.ToChar(advance.Key).ToString()] = advance.Value * 1000;
            //    }
            //
            //    FontBuilder.Fonts[name.ToLower()] = fontData;
            //
            //    return fontData;
        }
    }
}
