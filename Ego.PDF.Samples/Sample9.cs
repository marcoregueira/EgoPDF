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

        // Row splits the body width into the 2:1 column layout (1D
        // gallery wide on the left, 2D matrices narrow on the right)
        // with a 10 mm gutter between them. Each column receives a
        // Rect and runs its own vertical cursor inside.
        var columns = Row(new Rect(20, 62, W - 40, H - 62 - 20),
                          new[] { 2.0, 1.0 }, gap: 10);

        DrawLinearColumn(columns[0]);
        DrawMatrixColumn(columns[1]);
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

    /// <summary>
    /// Coral hairline + accent-coloured section label at the top of a
    /// column. Returns the rect remaining below the label so the caller
    /// can keep stacking entries inside it.
    /// </summary>
    private Rect DrawSectionLabel(Rect column, string text)
    {
        using (PushState())
        {
            SetDrawColor(BrandAccent);
            SetLineWidth(0.3);
            Line(column.X, column.Y, column.Right, column.Y);

            SetXY(column.X, column.Y + 4);
            SetFont("Poppins", "", 10);
            SetTextColor(BrandAccent);
            Cell(column.W, 5, text);
        }
        return column.WithTopOffset(18);
    }

    /// <summary>Visual indent between captions and codes so the codes hang under their labels.</summary>
    private const double CodeIndent = 3;

    /// <summary>Module width for every 1D code, in mm.</summary>
    private const double ModuleWidth = 0.5;

    private void DrawLinearColumn(Rect column)
    {
        var body = DrawSectionLabel(column, "LINEAR 1D");

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

        double cursor = body.Y;
        foreach (var row in rows)
        {
            SetFont("Roboto", "", 10);
            SetTextColor(TextMuted);
            SetXY(body.X, cursor);
            Cell(body.W, 4, row.label);

            row.draw(body.X + CodeIndent, cursor + 5, row.height, row.color);
            cursor += 5 + row.height + 4;
        }
    }

    private void DrawMatrixColumn(Rect column)
    {
        var body = DrawSectionLabel(column, "MATRIX 2D");

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

        double cursor = body.Y;
        foreach (var m in matrices)
        {
            // Caption above the matrix sits flush against the column edge.
            SetFont("Roboto", "", 10);
            SetTextColor(TextMuted);
            SetXY(body.X, cursor);
            Cell(m.matrixWidth, 5, m.label);

            m.draw(body.X + CodeIndent, cursor + 6, m.color);
            cursor += 5 + matrixSize + 8; // caption + matrix + gap
        }
    }

    private static string GetPath()
    {
        var codeBase = AppDomain.CurrentDomain.BaseDirectory;
        return Path.GetDirectoryName(codeBase);
    }
}
