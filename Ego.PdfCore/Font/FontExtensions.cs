using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Ego.PDF;
using Ego.PDF.Data;
using SkiaSharp;

namespace Ego.PDF.Font
{
    public static class FontExtensions
    {
        public static FPdf LoadFont(this FPdf pdf, string name, string path)
        {
            if (FontBuilder.Fonts.ContainsKey(name.ToLower()))
            {
                return pdf;
                //return FontBuilder.Fonts[name.ToLower()];
            }

            var chars = FontBuilder.Fonts.OrderBy(x => x.Value.Widths.Count).SelectMany(x => x.Value.Widths.Keys).Distinct().ToArray();

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
            var paint = new SKPaint(font);
            var lineSpacing = font.GetFontMetrics(out var metrics);
            fontData.Flags = 32;
            fontData.FontBBox = new Rectangle(0, 0, 0, 0);
            fontData.ItalicAngle = 0;
            fontData.Ascent = Math.Abs(metrics.Ascent * 100);
            fontData.Descent = metrics.Descent * 100;
            fontData.StemV = 0;
            fontData.CapHeight = metrics.CapHeight * 100;

            //fontData.Flags = 32;
            //fontData.FontBBox = new Rectangle(0, 0, 0, 0);
            //fontData.ItalicAngle = 0;
            //fontData.Ascent = 920;
            //fontData.Descent = 230;
            //fontData.StemV = 0;
            //fontData.CapHeight = 0;
            /// FontFile2 17 0 R


            foreach (var ch in chars)
            {
                var advance = paint.MeasureText(ch) * 100;
                fontData.Widths[ch] = advance;
            }

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

            FontBuilder.Fonts[name.ToLower()] = fontData;

            return pdf;
        }

        public static SKTypeface GetTypeface(string fullFontName)
        {
            var result = SKTypeface.FromStream(File.OpenRead(fullFontName));
            return result;
        }

    }
}
