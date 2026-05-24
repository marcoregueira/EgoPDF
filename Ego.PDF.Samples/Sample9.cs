using Ego.PDF.Data;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
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
/// ZXing.Net. Two-column layout on a single A4 page: linear barcodes
/// on the left, matrix barcodes on the right, both branded with the
/// egoPdf palette.
///
/// All renderers share the same drawing primitives: AddBarcode for the
/// 1D writers (which return a single bool[] row of modules) and
/// AddMatrix for the 2D writers (which return a ZXing BitMatrix).
/// </summary>
public class Sample9: FPdf
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

    /// <summary>
    /// Draws the navy egoPdf header band at the top of the page.
    /// Wordmark on the left ("ego" white + "Pdf" coral) and large
    /// "BARCODES" title on the right.
    /// </summary>
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
    /// Coral hairline + small-caps coral label, used at the top of each column.
    /// Returns the Y position from which the column body should start.
    /// </summary>
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

    /// <summary>
    /// Visual indent between the column label/title and the actual bars
    /// or matrix. Labels stay flush against the column origin; codes are
    /// nudged right so they hang under their captions like a list entry.
    /// </summary>
    private const double CodeIndent = 3;

    private void DrawLinearColumn(double x, double y, double width)
    {
        double cursor = DrawSectionLabel(x, y, width, "LINEAR 1D");

        var rows = new (string label, bool[] modules, int height, Color color)[]
        {
            ("Code 128",           Encode1D(new Code128Writer(), "EGOPDF-128",     BarcodeFormat.CODE_128),   10, BrandDark),
            ("Code 39",            Encode1D(new Code39Writer(),  "EGOPDF-39",      BarcodeFormat.CODE_39),    10, BrandAccent),
            ("Code 93",            Encode1D(new Code93Writer(),  "EGOPDF-93",      BarcodeFormat.CODE_93),    10, BrandDark),
            ("Interleaved 2 of 5", Encode1D(new ITFWriter(),     "12345678901234", BarcodeFormat.ITF),        10, BrandAccent),
            ("Codabar",            Encode1D(new CodaBarWriter(), "A12345678B",     BarcodeFormat.CODABAR),    10, BrandDark),
            ("EAN-13",             Encode1D(new EAN13Writer(),   "0123456789128",  BarcodeFormat.EAN_13),     12, BrandAccent),
            ("EAN-8",              Encode1D(new EAN8Writer(),    "12345670",       BarcodeFormat.EAN_8),      12, BrandDark),
            ("UPC-A",              Encode1D(new UPCAWriter(),    "012345678905",   BarcodeFormat.UPC_A),      12, BrandAccent),
        };

        foreach (var row in rows)
        {
            SetFont("Roboto", "", 10);
            SetTextColor(TextMuted);
            SetXY(x, cursor);
            Cell(width, 4, row.label);

            // AddBarcode draws at pdf.Y (when y=null), so move the cursor
            // to the desired bar top before drawing.
            Y = cursor + 5;
            AddBarcode(0.5, row.modules, x + CodeIndent, y: null, row.height, row.color);

            cursor += 5 + row.height + 4;
        }
    }

    private void DrawMatrixColumn(double x, double y, double width)
    {
        double cursor = DrawSectionLabel(x, y, width, "MATRIX 2D");

        const double matrixSize = 22.5; // mm, square cells (height); width = size * aspect.

        // Strip the default quiet-zone padding so all four matrices have
        // the same visible margin (none) and share the same left edge.
        // PDF417 in particular ships with a wide quiet zone that visually
        // shifts the code to the right of the column.
        var noMargin = new Dictionary<EncodeHintType, object>
        {
            { EncodeHintType.MARGIN, 0 },
        };

        var matrices = new (string label, BitMatrix matrix, double aspect, Color color)[]
        {
            ("QR Code",     new QRCodeWriter().encode("https://github.com/marcoregueira/egopdf",
                                                       BarcodeFormat.QR_CODE, 0, 0, noMargin),     1.0, BrandDark),
            ("Data Matrix", new DataMatrixWriter().encode("egoPdf · Data Matrix · 2026",
                                                       BarcodeFormat.DATA_MATRIX, 0, 0, noMargin), 1.0, BrandAccent),
            ("PDF417",      new PDF417Writer().encode("egoPdf · PDF417 · 1234567890",
                                                       BarcodeFormat.PDF_417, 0, 0, noMargin),     2.0, BrandDark),
            ("Aztec",       new AztecWriter().encode("egoPdf · Aztec · 1234567890",
                                                       BarcodeFormat.AZTEC, 0, 0, noMargin),       1.0, BrandAccent),
        };

        foreach (var m in matrices)
        {
            DrawMatrix(m.label, m.matrix, x, cursor, matrixSize, m.aspect, m.color, matrixIndent: CodeIndent);
            cursor += 5 + matrixSize + 8; // caption + matrix + gap
        }
    }

    /// <summary>
    /// Wrap any ZXing writer (including the few that don't expose
    /// encode(string) -> bool[]) into a single row of modules by going
    /// through the universal BitMatrix overload and reading row 0.
    /// Trailing/leading "off" modules (the quiet zone the writer adds)
    /// are stripped so the first dark bar sits flush against the column
    /// origin and aligns with the 2D matrices on the right.
    /// </summary>
    private static bool[] Encode1D(Writer writer, string contents, BarcodeFormat format)
    {
        var matrix = writer.encode(contents, format, 0, 0);
        var bits = new bool[ matrix.Width ];
        for (int i = 0; i < matrix.Width; i++)
            bits[ i ] = matrix[ i, 0 ];

        int first = 0;
        while (first < bits.Length && !bits[ first ]) first++;
        int last = bits.Length - 1;
        while (last > first && !bits[ last ]) last--;
        if (first == 0 && last == bits.Length - 1) return bits;

        var trimmed = new bool[ last - first + 1 ];
        Array.Copy(bits, first, trimmed, 0, trimmed.Length);
        return trimmed;
    }

    private void DrawMatrix(string label, BitMatrix matrix, double x, double y, double targetMm,
        double aspect = 1.0, Color? color = null, double matrixIndent = 0)
    {
        var drawColor = color ?? BrandDark;

        // Caption above the matrix sits flush against `x`.
        SetFont("Roboto", "", 10);
        SetTextColor(TextMuted);
        SetXY(x, y);
        Cell(targetMm * aspect, 5, label);

        // Each ZXing module → moduleMm user units. PDF417 isn't square so
        // we let the caller pass an aspect ratio that scales the matrix
        // width by `aspect` while keeping the height equal to targetMm.
        var moduleMmX = (targetMm * aspect) / matrix.Width;
        var moduleMmY = targetMm / matrix.Height;
        AddMatrix(matrix, x + matrixIndent, y + 6, moduleMmX, moduleMmY, drawColor);
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
