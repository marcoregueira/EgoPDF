using Ego.PDF.Barcodes;
using Ego.PDF.Data;
using Microsoft.Xna.Framework;
using System;
using System.IO;

namespace Ego.PDF.Samples;

/// <summary>
/// Gallery of barcode symbologies that the project can render. The
/// drawing primitives all live in <see cref="BarcodeRenderer"/> (in
/// the EgoPDF.Barcodes package); this sample is just a two-column
/// layout that picks a payload and a brand colour for each
/// symbology.
/// </summary>
public class Sample9 : FPdf
{
    private static readonly Color BrandDark   = new Color(26, 29, 38);
    private static readonly Color BrandAccent = new Color(204, 105, 95);
    private static readonly Color TextMuted   = new Color(110, 115, 130);

    public static Stream GetSample(string filePath, string path)
    {
        using var pdf = new Sample9(filePath);
        pdf.PrintPdf();
        pdf.Close();
        return pdf.Buffer.BaseStream;
    }

    private Sample9(string filePath) : base(filePath) { }

    private void PrintPdf()
    {
        _ = LoadFont("Poppins", Path.Combine(GetPath(), "Fonts/Poppins/Poppins-ExtraLight.ttf"));
        _ = LoadFont("Roboto", Path.Combine(GetPath(), "Fonts/Roboto/RobotoSlab-VariableFont_wght.ttf"));
        AddFont("Poppins", "");
        AddFont("Roboto", "");

        AddPage(PageSizeEnum.A4);
        DrawPageHeader();

        // Two-column layout: 1D barcodes on the left take 2/3 of the
        // body width, matrix barcodes on the right take 1/3, with a
        // 10mm gutter between them. 20 + 107 + 10 + 53 + 20 = 210mm.
        const double leftX = 20;
        const double leftWidth = 107;
        const double columnGap = 10;
        const double rightX = leftX + leftWidth + columnGap;     // 137
        const double rightWidth = 53;
        const double columnsTop = 62;

        DrawLinearColumn(leftX, columnsTop, leftWidth);
        DrawMatrixColumn(rightX, columnsTop, rightWidth);
    }

    private void DrawPageHeader()
    {
        const double bandHeight = 44;
        SetFillColor(BrandDark);
        Rect(0, 0, W, bandHeight, "F");

        SetXY(20, 14);
        SetFont("Poppins", "", 22);
        SetTextColor(Color.White);
        Cell(GetStringWidth("ego"), 11, "ego");
        SetTextColor(BrandAccent);
        Cell(GetStringWidth("Pdf"), 11, "Pdf");

        SetFont("Poppins", "", 26);
        SetTextColor(Color.White);
        SetXY(20, 12);
        Cell(W - 40, 12, "BARCODES", "0", 0, AlignEnum.Right);

        SetTextColor(BrandDark);
    }

    private double DrawSectionLabel(double x, double y, double width, string text)
    {
        SetDrawColor(BrandAccent);
        SetLineWidth(0.3);
        Line(x, y, x + width, y);

        SetXY(x, y + 4);
        SetFont("Poppins", "", 10);
        SetTextColor(BrandAccent);
        Cell(width, 5, text);

        SetTextColor(BrandDark);
        return y + 18;
    }

    /// <summary>Visual indent between captions and codes so the codes hang under their labels.</summary>
    private const double CodeIndent = 3;

    /// <summary>Module width for every 1D code, in mm.</summary>
    private const double ModuleWidth = 0.5;

    private void DrawLinearColumn(double x, double y, double width)
    {
        double cursor = DrawSectionLabel(x, y, width, "LINEAR 1D");

        var rows = new (string label, Action<double, double, double, Color> draw, double height, Color color)[]
        {
            ("Code 128",           (rx, ry, h, c) => BarcodeRenderer.DrawCode128         (this, "EGOPDF-128",      rx, ry, ModuleWidth, h, c), 10, BrandDark),
            ("Code 39",            (rx, ry, h, c) => BarcodeRenderer.DrawCode39          (this, "EGOPDF-39",       rx, ry, ModuleWidth, h, c), 10, BrandAccent),
            ("Code 93",            (rx, ry, h, c) => BarcodeRenderer.DrawCode93          (this, "EGOPDF-93",       rx, ry, ModuleWidth, h, c), 10, BrandDark),
            ("Interleaved 2 of 5", (rx, ry, h, c) => BarcodeRenderer.DrawInterleaved2of5 (this, "12345678901234", rx, ry, ModuleWidth, h, c), 10, BrandAccent),
            ("Codabar",            (rx, ry, h, c) => BarcodeRenderer.DrawCodabar         (this, "A12345678B",     rx, ry, ModuleWidth, h, c), 10, BrandDark),
            ("EAN-13",             (rx, ry, h, c) => BarcodeRenderer.DrawEan13           (this, "0123456789128",  rx, ry, ModuleWidth, h, c), 12, BrandAccent),
            ("EAN-8",              (rx, ry, h, c) => BarcodeRenderer.DrawEan8            (this, "12345670",       rx, ry, ModuleWidth, h, c), 12, BrandDark),
            ("UPC-A",              (rx, ry, h, c) => BarcodeRenderer.DrawUpcA            (this, "012345678905",   rx, ry, ModuleWidth, h, c), 12, BrandAccent),
        };

        foreach (var row in rows)
        {
            SetFont("Roboto", "", 10);
            SetTextColor(TextMuted);
            SetXY(x, cursor);
            Cell(width, 4, row.label);

            row.draw(x + CodeIndent, cursor + 5, row.height, row.color);
            cursor += 5 + row.height + 4;
        }
    }

    private void DrawMatrixColumn(double x, double y, double width)
    {
        double cursor = DrawSectionLabel(x, y, width, "MATRIX 2D");

        const double matrixSize = 22.5; // mm, square cells (height); width = size * aspect.
        const double pdf417Aspect = 2.0;

        var matrices = new (string label, double matrixWidth, double matrixHeight, Action<double, double, Color> draw, Color color)[]
        {
            ("QR Code",     matrixSize, matrixSize,
                (mx, my, c) => BarcodeRenderer.DrawQrCode    (this, "https://github.com/marcoregueira/egopdf", mx, my, matrixSize, c),
                BrandDark),
            ("Data Matrix", matrixSize, matrixSize,
                (mx, my, c) => BarcodeRenderer.DrawDataMatrix(this, "egoPdf · Data Matrix · 2026",            mx, my, matrixSize, c),
                BrandAccent),
            ("PDF417",      matrixSize * pdf417Aspect, matrixSize,
                (mx, my, c) => BarcodeRenderer.DrawPdf417    (this, "egoPdf · PDF417 · 1234567890",           mx, my, matrixSize * pdf417Aspect, matrixSize, c),
                BrandDark),
            ("Aztec",       matrixSize, matrixSize,
                (mx, my, c) => BarcodeRenderer.DrawAztec     (this, "egoPdf · Aztec · 1234567890",            mx, my, matrixSize, c),
                BrandAccent),
        };

        foreach (var m in matrices)
        {
            // Caption above the matrix sits flush against the column edge.
            SetFont("Roboto", "", 10);
            SetTextColor(TextMuted);
            SetXY(x, cursor);
            Cell(m.matrixWidth, 5, m.label);

            m.draw(x + CodeIndent, cursor + 6, m.color);
            cursor += 5 + matrixSize + 8; // caption + matrix + gap
        }
    }

    private static string GetPath()
    {
        var codeBase = AppDomain.CurrentDomain.BaseDirectory;
        return Path.GetDirectoryName(codeBase);
    }
}
