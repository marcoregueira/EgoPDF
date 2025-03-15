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

        var font2 = pdf.LoadFont("Calligrapher", Path.Combine(GetPath(), "Fonts/Ceviche/CevicheOne-Regular.ttf"));
        pdf.LoadFont("Calligrapher", Path.Combine(GetPath(), "Fonts/calligra.ttf"));
        pdf.LoadFont("Ceviche", Path.Combine(GetPath(), "Fonts/Ceviche/CevicheOne-Regular.ttf"));
        pdf.LoadFont("Roboto", Path.Combine(GetPath(), "Fonts/Roboto/RobotoSlab-VariableFont_wght.ttf"));
        pdf.LoadFont("mytype", Path.Combine(GetPath(), "Fonts/mytype/mytype.ttf"));
        pdf.LoadFont("Poppins", Path.Combine(GetPath(), "Fonts/Poppins/Poppins-ExtraLight.ttf"));

        pdf.AddPage(PageSizeEnum.A4);
        
        pdf.AddFont("Roboto", "");
        PrintFontName(pdf, "Roboto Slab");
        pdf.SetFont("Roboto", "", 16);
        pdf.Cell(190, 10, "Enjoy new fonts with FPDF!");
        pdf.Ln(15);

        pdf.AddFont("Ceviche", "");
        PrintFontName(pdf, "Ceviche");
        pdf.SetFont("Ceviche", "", 45);
        pdf.Cell(190, 10, "Enjoy new fonts with FPDF!");
        pdf.Ln(15);

        pdf.AddFont("mytype", "");
        PrintFontName(pdf, "mytype");
        pdf.SetFont("mytype", "", 16);
        pdf.Cell(190, 10, "Enjoy new fonts with FPDF!");
        pdf.Ln(15);

        pdf.AddFont("Calligrapher", "");
        PrintFontName(pdf, "Calligrapher");
        pdf.SetFont("Calligrapher", "", 16);
        pdf.Cell(190, 10, "Enjoy new fonts with FPDF!");
        pdf.Ln(15);

        pdf.AddFont("Poppins", "");
        PrintFontName(pdf, "Poppins");
        pdf.SetFont("Poppins", "", 16);
        pdf.Cell(190, 10, "Enjoy new fonts with FPDF!");
        pdf.Ln(20); 

        PrintFontName(pdf, "Roboto Slab (double height)");
        pdf.Ln(3); 
        pdf.SetFont("Roboto", "", 16, FontScale.DoubleHeight);
        pdf.Cell(190, 10, "Enjoy new fonts with FPDF!");

        pdf.Ln(15);
        PrintFontName(pdf, "Roboto Slab (double width)");
        pdf.SetFont("Roboto", "", 16, FontScale.DoubleWidth);
        pdf.Cell(190, 10, "Enjoy new fonts with FPDF!");
        pdf.Ln(15);

        pdf.Close();
        return pdf.Buffer.BaseStream;
    }

    private static void PrintFontName(FPdf pdf, string v)
    {
        pdf.SetFont("Roboto", "", 9);
        pdf.Cell(190, 3, v);
        pdf.Ln();
    }

    private static string GetPath()
    {
        var codeBase = AppDomain.CurrentDomain.BaseDirectory;
        return Path.GetDirectoryName(codeBase);
    }
}