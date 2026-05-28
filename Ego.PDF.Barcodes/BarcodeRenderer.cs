using System;
using System.Collections.Generic;
using Ego.PDF.Data;
using Microsoft.Xna.Framework;
using ZXing;
using ZXing.Aztec;
using ZXing.Common;
using ZXing.Datamatrix;
using ZXing.OneD;
using ZXing.PDF417;
using ZXing.QrCode;

namespace Ego.PDF.Barcodes;

/// <summary>
/// Public barcode rendering API on top of FPdf + ZXing.Net. Each method
/// encodes the data with the matching ZXing writer and stamps the
/// resulting modules onto the page using FPdf's drawing primitives.
///
/// Symbologies are split into two families:
///
/// * <b>1D (linear)</b>: Code 128, Code 39, Code 93, Interleaved 2 of 5,
///   Codabar, EAN-13, EAN-8, UPC-A. Caller supplies <c>moduleWidth</c>
///   (mm per narrow bar) and <c>height</c> (mm).
/// * <b>2D (matrix)</b>: QR Code, Data Matrix, Aztec — square,
///   parameterised by <c>size</c>. PDF417 is rectangular and takes
///   <c>width</c> + <c>height</c> independently.
///
/// All methods omit the quiet zone: the first dark module sits flush
/// against the <c>(x, y)</c> origin. If you need spec-compliant white
/// margin (e.g. for physical scanning), draw your own padding around
/// the call.
/// </summary>
public static class BarcodeRenderer
{
    // ---- 1D ----------------------------------------------------------------

    public static void DrawCode128(FPdf pdf, string data, double x, double y, double moduleWidth, double height, Color? color = null)
        => Draw1D(pdf, new Code128Writer(), BarcodeFormat.CODE_128, data, x, y, moduleWidth, height, color);

    public static void DrawCode39(FPdf pdf, string data, double x, double y, double moduleWidth, double height, Color? color = null)
        => Draw1D(pdf, new Code39Writer(), BarcodeFormat.CODE_39, data, x, y, moduleWidth, height, color);

    public static void DrawCode93(FPdf pdf, string data, double x, double y, double moduleWidth, double height, Color? color = null)
        => Draw1D(pdf, new Code93Writer(), BarcodeFormat.CODE_93, data, x, y, moduleWidth, height, color);

    public static void DrawInterleaved2of5(FPdf pdf, string data, double x, double y, double moduleWidth, double height, Color? color = null)
        => Draw1D(pdf, new ITFWriter(), BarcodeFormat.ITF, data, x, y, moduleWidth, height, color);

    public static void DrawCodabar(FPdf pdf, string data, double x, double y, double moduleWidth, double height, Color? color = null)
        => Draw1D(pdf, new CodaBarWriter(), BarcodeFormat.CODABAR, data, x, y, moduleWidth, height, color);

    public static void DrawEan13(FPdf pdf, string data, double x, double y, double moduleWidth, double height, Color? color = null)
        => Draw1D(pdf, new EAN13Writer(), BarcodeFormat.EAN_13, data, x, y, moduleWidth, height, color);

    public static void DrawEan8(FPdf pdf, string data, double x, double y, double moduleWidth, double height, Color? color = null)
        => Draw1D(pdf, new EAN8Writer(), BarcodeFormat.EAN_8, data, x, y, moduleWidth, height, color);

    public static void DrawUpcA(FPdf pdf, string data, double x, double y, double moduleWidth, double height, Color? color = null)
        => Draw1D(pdf, new UPCAWriter(), BarcodeFormat.UPC_A, data, x, y, moduleWidth, height, color);

    // ---- 2D ----------------------------------------------------------------

    public static void DrawQrCode(FPdf pdf, string data, double x, double y, double size, Color? color = null)
        => Draw2D(pdf, new QRCodeWriter(), BarcodeFormat.QR_CODE, data, x, y, size, size, color);

    public static void DrawDataMatrix(FPdf pdf, string data, double x, double y, double size, Color? color = null)
        => Draw2D(pdf, new DataMatrixWriter(), BarcodeFormat.DATA_MATRIX, data, x, y, size, size, color);

    public static void DrawAztec(FPdf pdf, string data, double x, double y, double size, Color? color = null)
        => Draw2D(pdf, new AztecWriter(), BarcodeFormat.AZTEC, data, x, y, size, size, color);

    public static void DrawPdf417(FPdf pdf, string data, double x, double y, double width, double height, Color? color = null)
        => Draw2D(pdf, new PDF417Writer(), BarcodeFormat.PDF_417, data, x, y, width, height, color);

    // ---- internals ---------------------------------------------------------

    private static readonly IDictionary<EncodeHintType, object> NoMargin =
        new Dictionary<EncodeHintType, object> { { EncodeHintType.MARGIN, 0 } };

    private static void Draw1D(FPdf pdf, Writer writer, BarcodeFormat format, string data,
        double x, double y, double moduleWidth, double height, Color? color)
    {
        // Use the universal BitMatrix overload (one writer handles every
        // format) and read row 0 as the module pattern.
        BitMatrix matrix;
        try { matrix = writer.encode(data, format, 0, 0); }
        catch { return; }

        var bits = new bool[ matrix.Width ];
        for (int i = 0; i < matrix.Width; i++) bits[ i ] = matrix[ i, 0 ];

        // Strip the leading/trailing "off" modules (quiet zone) so the
        // first dark bar sits flush against `x`.
        int first = 0;
        while (first < bits.Length && !bits[ first ]) first++;
        int last = bits.Length - 1;
        while (last > first && !bits[ last ]) last--;
        if (last < first) return;

        StampBars(pdf, bits, first, last, x, y, moduleWidth, height, color ?? Color.Black);
    }

    private static void Draw2D(FPdf pdf, Writer writer, BarcodeFormat format, string data,
        double x, double y, double widthMm, double heightMm, Color? color)
    {
        BitMatrix matrix;
        try { matrix = writer.encode(data, format, 0, 0, NoMargin); }
        catch { return; }

        var moduleMmX = widthMm / matrix.Width;
        var moduleMmY = heightMm / matrix.Height;
        StampMatrix(pdf, matrix, x, y, moduleMmX, moduleMmY, color ?? Color.Black);
    }

    /// <summary>
    /// Stamps a 1D module array as filled rectangles. Adjacent "on"
    /// modules are coalesced into a single rectangle so the PDF stays
    /// compact.
    /// </summary>
    private static void StampBars(FPdf pdf, bool[] bits, int first, int last,
        double x, double y, double w, double height, Color color)
    {
        // pdf.DrawArea translates point.Y by (H - pdf.Y), so we feed Y
        // values relative to pdf.Y. Saving and restoring pdf.Y lets the
        // caller place bars anywhere on the page without side effects.
        var savedY = pdf.Y;
        pdf.Y = y;

        int i = first;
        while (i <= last)
        {
            if (!bits[ i ]) { i++; continue; }
            int runEnd = i;
            while (runEnd + 1 <= last && bits[ runEnd + 1 ]) runEnd++;

            var left = x + (i - first) * w;
            var right = x + (runEnd - first + 1) * w;
            pdf.DrawArea(color, 0.00, new[]
            {
                new DrawingPoint(left,  0),
                new DrawingPoint(right, 0),
                new DrawingPoint(right, height),
                new DrawingPoint(left,  height),
            });

            i = runEnd + 1;
        }

        pdf.Y = savedY;
    }

    /// <summary>
    /// Stamps a 2D BitMatrix as a grid of filled rectangles. Adjacent
    /// "on" modules in the same row are coalesced into a single
    /// rectangle so the PDF stays compact.
    /// </summary>
    private static void StampMatrix(FPdf pdf, BitMatrix matrix, double x, double y,
        double moduleWidth, double moduleHeight, Color color)
    {
        var savedY = pdf.Y;
        pdf.Y = y;

        for (int row = 0; row < matrix.Height; row++)
        {
            int runStart = -1;
            for (int col = 0; col < matrix.Width; col++)
            {
                if (matrix[ col, row ])
                {
                    if (runStart < 0) runStart = col;
                }
                else if (runStart >= 0)
                {
                    StampMatrixRun(pdf, x, moduleWidth, moduleHeight, runStart, row, col - runStart, color);
                    runStart = -1;
                }
            }
            if (runStart >= 0)
                StampMatrixRun(pdf, x, moduleWidth, moduleHeight, runStart, row, matrix.Width - runStart, color);
        }

        pdf.Y = savedY;
    }

    private static void StampMatrixRun(FPdf pdf, double x, double mw, double mh,
        int col, int row, int length, Color color)
    {
        var left = x + col * mw;
        var right = left + length * mw;
        var top = row * mh;
        var bottom = top + mh;
        pdf.DrawArea(color, 0.00, new[]
        {
            new DrawingPoint(left,  top),
            new DrawingPoint(right, top),
            new DrawingPoint(right, bottom),
            new DrawingPoint(left,  bottom),
        });
    }
}
