using Ego.PDF.Data;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;
using ZXing;
using ZXing.Aztec;
using ZXing.Common;
using ZXing.Datamatrix;
using ZXing.OneD;
using ZXing.PDF417;
using ZXing.QrCode;

namespace Ego.PDF.Samples;

/// <summary>
/// Gallery of barcode symbologies that the project can render through
/// ZXing.Net. Covers the 1D barcodes typically used in industry
/// (Code 128, Code 39, Code 93, ITF, Codabar, EAN/UPC) and the 2D
/// matrix barcodes (QR Code, Data Matrix, PDF417, Aztec). The page
/// uses the egoPdf brand palette so the output matches the other
/// samples in the demo.
///
/// All renderers share the same drawing primitives: AddBarcode for the
/// 1D writers (which return a single bool[] row of modules) and
/// AddMatrix for the 2D writers (which return a ZXing BitMatrix).
/// </summary>
public class Sample9: FPdf
{
    private static readonly Color BrandDark   = new Color(26, 29, 38);
    private static readonly Color BrandAccent = new Color(204, 105, 95);
    private static readonly Color BandSubText = new Color(180, 184, 196);
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

        // 1D — linear barcodes ------------------------------------------------
        AddPage(PageSizeEnum.A4);
        DrawPageHeader("linear 1D");

        DrawOneD("Code 128",            Encode1D(new Code128Writer(), "EGOPDF-128",     BarcodeFormat.CODE_128),   10, BrandDark);
        DrawOneD("Code 39",             Encode1D(new Code39Writer(),  "EGOPDF-39",      BarcodeFormat.CODE_39),    10, BrandAccent);
        DrawOneD("Code 93",             Encode1D(new Code93Writer(),  "EGOPDF-93",      BarcodeFormat.CODE_93),    10, BrandDark);
        DrawOneD("Interleaved 2 of 5",  Encode1D(new ITFWriter(),     "12345678901234", BarcodeFormat.ITF),        10, BrandAccent);
        DrawOneD("Codabar",             Encode1D(new CodaBarWriter(), "A12345678B",     BarcodeFormat.CODABAR),    10, BrandDark);
        DrawOneD("EAN-13",              Encode1D(new EAN13Writer(),   "0123456789128",  BarcodeFormat.EAN_13),     12, BrandAccent);
        DrawOneD("EAN-8",               Encode1D(new EAN8Writer(),    "12345670",       BarcodeFormat.EAN_8),      12, BrandDark);
        DrawOneD("UPC-A",               Encode1D(new UPCAWriter(),    "012345678905",   BarcodeFormat.UPC_A),      12, BrandAccent);

        // 2D — matrix barcodes ------------------------------------------------
        AddPage(PageSizeEnum.A4);
        DrawPageHeader("matrix 2D");

        const double matrixSize = 35; // mm, square
        DrawMatrix("QR Code",     new QRCodeWriter().encode("https://github.com/marcoregueira/egopdf",
                                                            BarcodeFormat.QR_CODE, 0, 0),
                   LeftMargin,                       Y, matrixSize, color: BrandDark);
        DrawMatrix("Data Matrix", new DataMatrixWriter().encode("egoPdf · Data Matrix · 2026",
                                                            BarcodeFormat.DATA_MATRIX, 0, 0),
                   LeftMargin + matrixSize + 30,     Y, matrixSize, color: BrandAccent);

        Y += matrixSize + 20;
        DrawMatrix("PDF417",      new PDF417Writer().encode("egoPdf · PDF417 · 1234567890",
                                                            BarcodeFormat.PDF_417, 0, 0),
                   LeftMargin,                       Y, matrixSize, aspect: 3.0, color: BrandDark);
        DrawMatrix("Aztec",       new AztecWriter().encode("egoPdf · Aztec · 1234567890",
                                                            BarcodeFormat.AZTEC, 0, 0),
                   LeftMargin + matrixSize * 3 + 10, Y, matrixSize, color: BrandAccent);
    }

    /// <summary>
    /// Draws the navy egoPdf header band at the top of the current page.
    /// Wordmark on the left ("ego" white + "Pdf" coral), large
    /// "BARCODES" title on the right and the section subtitle (passed
    /// in) underneath in light gray. Cursor is left just below the band
    /// so the caller can start writing content immediately.
    /// </summary>
    private void DrawPageHeader(string subtitle)
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

        SetFont("Roboto", "", 10);
        SetTextColor(BandSubText);
        SetXY(20, 29);
        Cell(W - 40, 5, subtitle, "0", 0, AlignEnum.Right);

        SetXY(LeftMargin, bandHeight + 10);
        SetTextColor(BrandDark);
    }

    /// <summary>
    /// Wrap any ZXing writer (including the few that don't expose
    /// encode(string) -> bool[]) into a single row of modules by going
    /// through the universal BitMatrix overload and reading row 0.
    /// </summary>
    private static bool[] Encode1D(Writer writer, string contents, BarcodeFormat format)
    {
        var matrix = writer.encode(contents, format, 0, 0);
        var bits = new bool[ matrix.Width ];
        for (int i = 0; i < matrix.Width; i++)
            bits[ i ] = matrix[ i, 0 ];
        return bits;
    }

    private void DrawOneD(string label, bool[] modules, int barcodeHeight, Color color)
    {
        SetFont("Roboto", "", 10);
        SetTextColor(TextMuted);
        Cell(60, 5, label);
        Ln();
        AddBarcode(0.5, modules, LeftMargin, y: null, barcodeHeight, color);
        // The bars don't advance the cursor on their own; bump Y past the
        // barcode plus a small gap so the next label doesn't collide.
        Y += barcodeHeight + 4;
    }

    private void DrawMatrix(string label, BitMatrix matrix, double x, double y, double targetMm,
        double aspect = 1.0, Color? color = null)
    {
        var drawColor = color ?? BrandDark;

        // Caption above the matrix.
        SetFont("Roboto", "", 10);
        SetTextColor(TextMuted);
        SetXY(x, y);
        Cell(targetMm * aspect, 5, label);

        // Each ZXing module → moduleMm user units. PDF417 isn't square so
        // we let the caller pass an aspect ratio that scales the matrix
        // width by `aspect` while keeping the height equal to targetMm.
        var moduleMmX = (targetMm * aspect) / matrix.Width;
        var moduleMmY = targetMm / matrix.Height;
        AddMatrix(matrix, x, y + 6, moduleMmX, moduleMmY, drawColor);
    }

    private void AddBarcode(double w, bool[] bitmap, double? x, double? y, int height, Color? color = null)
    {
        color = color ?? BrandDark;
        var index = 0;
        var count = bitmap.Length;
        var left = x ?? X;
        // DrawArea translates point.Y by H - pdf.Y, so a relative top of 0
        // lands on pdf.Y itself. Don't seed `top` with Y or the bars end
        // up double-translated.
        var top = y ?? 0;
        while (index < count)
        {
            if (bitmap[ index ])
            {
                var trueCount = bitmap.Skip(index + 1).TakeWhile(b => b).Count();
                var points = new[]
                {
                    new DrawingPoint(left + w * index, top),
                    new DrawingPoint(left + (w * index) + (w * (trueCount + 1)), top),
                    new DrawingPoint(left + (w * index) + (w * (trueCount + 1)), top + height),
                    new DrawingPoint(left + w * index, top + height),
                };
                DrawArea(color, 0.00, points);
                index += trueCount;
            }
            index++;
        }
    }

    /// <summary>
    /// Stamps a 2D BitMatrix as a grid of small filled rectangles. Adjacent
    /// "on" modules in the same row are coalesced into a single rectangle
    /// so the PDF stays compact.
    /// </summary>
    private void AddMatrix(BitMatrix matrix, double x, double y, double moduleWidth, double moduleHeight, Color color)
    {
        for (int row = 0; row < matrix.Height; row++)
        {
            int runStart = -1;
            for (int col = 0; col < matrix.Width; col++)
            {
                var on = matrix[ col, row ];
                if (on)
                {
                    if (runStart < 0) runStart = col;
                }
                else if (runStart >= 0)
                {
                    StampRun(x, y, moduleWidth, moduleHeight, runStart, row, col - runStart, color);
                    runStart = -1;
                }
            }
            if (runStart >= 0)
                StampRun(x, y, moduleWidth, moduleHeight, runStart, row, matrix.Width - runStart, color);
        }
    }

    private void StampRun(double x, double y, double mw, double mh, int col, int row, int length, Color color)
    {
        // DrawArea applies an implicit (H - pdf.Y) - point.Y translation,
        // so to position the matrix at the absolute (x, y) we passed in,
        // we have to feed Y values relative to pdf.Y here.
        var left = x + col * mw;
        var top = (y - Y) + row * mh;
        var right = left + length * mw;
        var bottom = top + mh;
        var points = new[]
        {
            new DrawingPoint(left,  top),
            new DrawingPoint(right, top),
            new DrawingPoint(right, bottom),
            new DrawingPoint(left,  bottom),
        };
        DrawArea(color, 0.00, points);
    }

    private static string GetPath()
    {
        var codeBase = AppDomain.CurrentDomain.BaseDirectory;
        return Path.GetDirectoryName(codeBase);
    }
}
