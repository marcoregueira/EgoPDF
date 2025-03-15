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
            LoadFont("Calligrapher", Path.Combine(GetPath(), "calligra.ttf"));
            LoadFont("Ceviche", Path.Combine(GetPath(), "Ceviche/CevicheOne-Regular.ttf"));
            LoadFont("Roboto", Path.Combine(GetPath(), "Roboto/RobotoSlab-VariableFont_wght.ttf"));
            LoadFont("mytype", Path.Combine(GetPath(), "mytype/mytype.ttf"));

            using var pdf = new FPdf(DateTime.Now.Ticks + ".samplefont.pdf");
            pdf.AddPage(PageSizeEnum.A4);
            //pdf.SetFont("Times", "", 16);
            //pdf.AddFont("Calligrapher", "", "file:///" + font2.FontFile);
            pdf.AddFont("mytype", "");
            pdf.SetFont("mytype", "", 45);
            pdf.Cell(40, 10, "Enjoy new fonts with FPDF!");
            pdf.Close();
        }

        public static SKTypeface GetTypeface(string fullFontName)
        {
            var result = SKTypeface.FromStream(File.OpenRead(fullFontName));
            return result;
        }

        public FontDefinition LoadFont(string name, string path, string variants)
        {
            var chars = FontBuilder.Fonts
                .OrderBy(x => x.Value.Widths.Count)
                .SelectMany(x => x.Value.Widths.Keys)
                .Distinct()
                .ToArray();

            var fontFace = GetTypeface(path);
            var fontData = new FontDefinition
            {
                FontType = FontTypeEnum.TrueType,
                FontFile = path,
                Name = fontFace.FamilyName.ToLower(),
                up = -100,
                ut = 50
            };

            var font = new SKFont(fontFace, 10);
            var lineSpacing = font.GetFontMetrics(out var metrics);
            fontData.Flags = 32;
            fontData.FontBBox = new Rectangle(0, 0, 0, 0);
            fontData.ItalicAngle = 0;
            fontData.Ascent = Math.Abs(metrics.Ascent * 100);
            fontData.Descent = metrics.Descent * 100;
            fontData.StemV = 0;
            fontData.CapHeight = metrics.CapHeight * 100;


            foreach (var ch in chars)
            {
                var advance = font.MeasureText(ch) * 100;
                fontData.Widths[ ch ] = advance;
            }


            //fontData.Flags = 32;
            //fontData.FontBBox = new Rectangle(0, 0, 0, 0);
            //fontData.ItalicAngle = 0;
            //fontData.Ascent = 920;
            //fontData.Descent = 230;
            //fontData.StemV = 0;
            //fontData.CapHeight = 0;
            /// FontFile2 17 0 R
            //var gg = font.GetGlyphs("asa".AsSpan());

            var tables = fontFace.TableCount;
            //var m = font.MeasureText();
            //foreach (var g in metrics)
            //{
            //
            //    var leftBearing = g.LeftSideBearing;
            //    var rightBearing = g.RightSideBearing;
            //    fontData.Widths[g.Character.ToString()] = advance * 1000;
            //}
            //
            //foreach (var advance in fontFace.AdvanceWidths)
            //{
            //    var leftBearing = fontFace.LeftSideBearings[advance.Key];
            //    var rightBearing = fontFace.RightSideBearings[advance.Key];
            //    fontData.Widths[Convert.ToChar(advance.Key).ToString()] = advance.Value * 1000;
            //}

            FontBuilder.Fonts[ name.ToLower() ] = fontData;

            return fontData;
        }
    }
}
