using Ego.PDF.Data;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;
using ZXing.OneD;

namespace Ego.PDF.Samples;

public class Sample9: FPdf
{
    public static Stream GetSample(string filePath, string path)
    {
        using var pdf = new Sample9();
        pdf.PrintPdf();
        pdf.Close();
        return pdf.Buffer.BaseStream;
    }

    private void PrintPdf()
    {
        _ = LoadFont("Poppins", Path.Combine(GetPath(), "Fonts/Poppins/Poppins-ExtraLight.ttf"));
        _ = LoadFont("Roboto", Path.Combine(GetPath(), "Fonts/Roboto/RobotoSlab-VariableFont_wght.ttf"));

        AddFont("Poppins", "");
        AddFont("Roboto", "");

        AddPage(PageSizeEnum.A4);


        SetFont("Poppins", "", 22, new FontScale(1, 1.5));

        Ln(10);
        Cell(RightMargin - LeftMargin, 10, "Ego.PDF Barcodes!", AlignEnum.Center);

        Ln(30);
        SetFont("Roboto", "", 10);
        Cell(40, 5, "Code128");
        Ln();

        double w = 0.5;
        var barcode = new Code128Writer();
        var code128 = barcode.encode("0123456789");
        AddBarcode(w, code128, LeftMargin, y: null, 10);

        Ln(18);
        Cell(40, 5, "EAN13");
        Ln();
        var ean13w = new EAN13Writer();
        var ean13 = ean13w.encode("0123456789128");
        AddBarcode(w, ean13, LeftMargin, y: null, 10, Color.Navy);

        Ln(18);
        Cell(40, 5, "Code39");
        Ln();
        var code39w= new Code39Writer();
        var code39 = code39w.encode("123456");
        AddBarcode(w, code39, LeftMargin, y: null, 10, Color.Navy);

        Ln(18);
        Cell(40, 5, "CodeMsi");
        Ln();
        var msiw = new MSIWriter();
        var msi = code39w.encode("012345678");
        AddBarcode(w, msi, LeftMargin, y: null, 10, Color.Navy);
    }

    private void AddBarcode(double w, bool[] bitmap, double? x, double? y, int height, Color? color = null)
    {
        color = color ?? Color.Black;
        var index = 0;
        var count = bitmap.Length;
        var left = x ?? X;
        var top = y ?? 0;
        while (index < count)
        {
            if (bitmap[ index ])
            {
                var trueCount = bitmap.Skip(index + 1).TakeWhile(b => b).Count();
                var points = new[]
                {
                        new DrawingPoint(left + w * index, top),
                        new DrawingPoint(left + (w * index) + (w * (trueCount + 1)) , top ),
                        new DrawingPoint(left + (w * index) + (w * (trueCount + 1)) , top+height),
                        new DrawingPoint(left + w * index, top+height),
                    };

                DrawArea(color, 0.00, points);
                index += trueCount;
            }

            index++;
        }
    }

    private static string GetPath()
    {
        var codeBase = AppDomain.CurrentDomain.BaseDirectory;
        return Path.GetDirectoryName(codeBase);
    }
}
