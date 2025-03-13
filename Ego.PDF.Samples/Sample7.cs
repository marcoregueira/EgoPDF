using Ego.PDF.Data;
using Ego.PDF.Font;
using SkiaSharp;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Ego.PDF.Samples;

public class Sample7
{
    public static Stream GetSample(string filePath)
    {
        var pdf = new FPdf(filePath);

        var font2 = LoadFont("Calligrapher", Path.Combine(GetPath(), "Fonts/Ceviche/CevicheOne-Regular.ttf"));
        LoadFont("Calligrapher", Path.Combine(GetPath(), "Fonts/calligra.ttf"));
        LoadFont("Ceviche", Path.Combine(GetPath(), "Fonts/Ceviche/CevicheOne-Regular.ttf"));
        LoadFont("Roboto", Path.Combine(GetPath(), "Fonts/Roboto/RobotoSlab-VariableFont_wght.ttf"));
        LoadFont("mytype", Path.Combine(GetPath(), "Fonts/mytype/mytype.ttf"));

        pdf.AddPage(PageSizeEnum.A4);
        
        pdf.AddFont("Roboto", "");
        FontName(pdf, "Roboto");
        pdf.SetFont("Roboto", "", 16);
        pdf.Cell(190, 10, "Enjoy new fonts with FPDF!");
        pdf.Ln(15);

        pdf.AddFont("Ceviche", "");
        FontName(pdf, "Ceviche");
        pdf.SetFont("Ceviche", "", 45);
        pdf.Cell(190, 10, "Enjoy new fonts with FPDF!");
        pdf.Ln(15);

        pdf.AddFont("mytype", "");
        FontName(pdf, "mytype");
        pdf.SetFont("mytype", "", 16);
        pdf.Cell(190, 10, "Enjoy new fonts with FPDF!");
        pdf.Ln(15);

        pdf.AddFont("Calligrapher", "");
        FontName(pdf, "Calligrapher");
        pdf.SetFont("Calligrapher", "", 16);
        pdf.Cell(190, 10, "Enjoy new fonts with FPDF!");

        pdf.Ln(20); 
        FontName(pdf, "Roboto (doble height)");
        pdf.Ln(3); 
        pdf.SetFont("Roboto", "", 16, FontScale.DoubleHeight);
        pdf.Cell(190, 10, "Enjoy new fonts with FPDF!");

        pdf.Ln(15);
        FontName(pdf, "Roboto (doble width)");
        pdf.SetFont("Roboto", "", 16, FontScale.DoubleWidth);
        pdf.Cell(190, 10, "Enjoy new fonts with FPDF!");
        pdf.Ln(15);


        pdf.Close();
        return pdf.Buffer.BaseStream;
    }

    private static void FontName(FPdf pdf, string v)
    {
        pdf.SetFont("Roboto", "", 9);
        pdf.Cell(190, 3, v);
        pdf.Ln();
    }

    public static SKTypeface GetTypeface(string fullFontName)
    {
        var result = SKTypeface.FromStream(File.OpenRead(fullFontName));
        return result;
    }

    public static FontDefinition LoadFont(string name, string path)
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
            var advance = font.MeasureText(ch) * 100;
            fontData.Widths[ ch ] = advance;
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

        FontBuilder.Fonts[ name.ToLower() ] = fontData;

        return fontData;
    }

    private static string GetPath()
    {
        var codeBase = AppDomain.CurrentDomain.BaseDirectory;
        return Path.GetDirectoryName(codeBase);
    }
}