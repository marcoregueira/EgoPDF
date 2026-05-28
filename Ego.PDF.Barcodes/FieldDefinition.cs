using System.Text;
using Ego.PDF;
using ZXing.OneD;
using ZXing;
using Ego.PDF.Data;
using Microsoft.Xna.Framework;
using static Ego.PDF.Barcodes.Zpl.PdfZpl;

namespace Ego.PDF.Barcodes.Zpl;

public class FieldDefinition
{
    public enum OriginEnum { LeftTop, LeftBottom };

    public OriginEnum Origin { get; set; } = OriginEnum.LeftTop;
    public string Orientation { get; set; } = "N";
    public double ScaleX { get; set; } = 1;
    public double ScaleY { get; set; } = 1;
    public string? Value { get; set; }
    public string? Font { get; set; }
    public int Thickness { get; set; } = 50;

    public FieldMode TextMode { get; set; }
    public BarcodeMode BarcodeMode { get; set; }

    public BarcodeOptions BarcodeOptions { get; } = new BarcodeOptions();
    public Barcode128Options Barcode128Options { get; } = new Barcode128Options();
    public Barcode2of5Options Barcode2of5Options { get; } = new Barcode2of5Options();
    public Barcode1DOptions Barcode1DOptions { get; } = new Barcode1DOptions();
    public Barcode2DOptions Barcode2DOptions { get; } = new Barcode2DOptions();
    public string EscapeCharacter { get; set; }
    public int Dpi { get; internal set; }
    public string MonospaceFont { get; internal set; }
    public string MonospaceStyle { get; internal set; } = "";
    public string VariableFont { get; internal set; }
    public string VariableStyle { get; internal set; } = "";
    internal FrameBox? FrameBox { get; set; }
    public double K { get; internal set; }
    public int DotsW { get; internal set; }
    public int DotsH { get; internal set; }

    /// <summary>ZPL ^FR: invert the next graphic field's fill (black ↔ white).</summary>
    public bool Reverse { get; set; }

    internal void Draw(FPdf pdf)
    {
        pdf.FontScale.ScaleX = this.ScaleX;
        pdf.FontScale.ScaleY = this.ScaleY;

        var text = this.Value ?? string.Empty;
        if (Value == "")
        {
            return;
        }

        if (this.TextMode == FieldMode.Barcode)
        {
            switch (this.BarcodeMode)
            {
                case BarcodeMode.Code128:
                    DrawBarcodeCode128(pdf, text);
                    break;
                case BarcodeMode.Interleaved2of5:
                    DrawBarcodeI2of5(pdf, text);
                    break;
                case BarcodeMode.Code39:
                    DrawBarcode1D(pdf, text, new ZXing.OneD.Code39Writer(),  ZXing.BarcodeFormat.CODE_39);
                    break;
                case BarcodeMode.Codabar:
                    DrawBarcode1D(pdf, text, new ZXing.OneD.CodaBarWriter(), ZXing.BarcodeFormat.CODABAR);
                    break;
                case BarcodeMode.EAN13:
                    DrawBarcode1D(pdf, text, new ZXing.OneD.EAN13Writer(),   ZXing.BarcodeFormat.EAN_13);
                    break;
                case BarcodeMode.EAN8:
                    DrawBarcode1D(pdf, text, new ZXing.OneD.EAN8Writer(),    ZXing.BarcodeFormat.EAN_8);
                    break;
                case BarcodeMode.UPC_A:
                    DrawBarcode1D(pdf, text, new ZXing.OneD.UPCAWriter(),    ZXing.BarcodeFormat.UPC_A);
                    break;
                case BarcodeMode.UPC_E:
                    DrawBarcode1D(pdf, text, new ZXing.OneD.UPCEWriter(),    ZXing.BarcodeFormat.UPC_E);
                    break;
                case BarcodeMode.MSI:
                    DrawBarcode1D(pdf, text, new ZXing.OneD.MSIWriter(),     ZXing.BarcodeFormat.MSI);
                    break;
                case BarcodeMode.QrCode:
                    DrawBarcode2D(pdf, text, new ZXing.QrCode.QRCodeWriter(),       ZXing.BarcodeFormat.QR_CODE);
                    break;
                case BarcodeMode.DataMatrix:
                    DrawBarcode2D(pdf, text, new ZXing.Datamatrix.DataMatrixWriter(), ZXing.BarcodeFormat.DATA_MATRIX);
                    break;
                case BarcodeMode.PDF417:
                    DrawBarcode2D(pdf, text, new ZXing.PDF417.PDF417Writer(),       ZXing.BarcodeFormat.PDF_417);
                    break;
                case BarcodeMode.Aztec:
                    DrawBarcode2D(pdf, text, new ZXing.Aztec.AztecWriter(),         ZXing.BarcodeFormat.AZTEC);
                    break;
            }
            return;
        }

        var fontsize = GetFontSize(this.Font, Dpi);
        pdf.SavePos();
        var fontPoints = (Convert.ToDouble(this.Thickness) / Dpi) * 25.4 * 2.54;
        pdf.SetFontSize(fontPoints);

        // ^FO places the FO point at the top-left of the bounding box, so the
        // baseline lives Thickness * 0.7 (≈ ascent) below it. ^FT (Origin =
        // LeftBottom) places the FO point ON the baseline, so no offset.
        var baselineOffset = Origin == OriginEnum.LeftBottom ? 0 : this.Thickness * 0.7;

        double tracking;
        if (this.Font == "0")
        {
            if (!string.IsNullOrEmpty(this.VariableFont))
                pdf.SetFont(this.VariableFont, this.VariableStyle ?? "", 0, null);
            tracking = 0;
        }
        else
        {
            // Has ^A set explicit per-character dot dimensions?
            var explicitSize = DotsH > 0 && DotsW > 0;
            var charH = explicitSize ? DotsH : this.Thickness;
            var charW = explicitSize ? DotsW : (int)Math.Round(this.Thickness * (double)fontsize.DotsW / fontsize.DotsH);
            if (charW <= 0) charW = 1;
            pdf.SetFont(this.MonospaceFont, this.MonospaceStyle ?? "", 0, null);

            if (explicitSize)
            {
                // ZPL "height" empirically lines up with the em size of the
                // chosen PDF font (visible cap-height ≈ 0.7 × charH). The
                // cap-height interpretation (charH × k / 0.7) draws chars
                // tall enough to overlap adjacent fields; the em
                // interpretation matches Labelary's apparent sizing.
                fontPoints = charH * pdf.k;
                pdf.SetFontSize(fontPoints);
                // scaleX makes the horizontal advance match charW dots.
                var naturalAdvancePt = 0.6 * fontPoints;
                var targetAdvancePt = charW * pdf.k;
                this.ScaleX = naturalAdvancePt > 0 ? targetAdvancePt / naturalAdvancePt : 1;
                this.ScaleY = 1;
            }
            else
            {
                pdf.SetFontSize(fontPoints);
                this.ScaleX = 1;
                this.ScaleY = 1;
            }

            pdf.FontScale.ScaleX = this.ScaleX;
            pdf.FontScale.ScaleY = this.ScaleY;
            // The Zebra A pseudo-tracking only makes sense when ^CF guessed
            // the width from a height; if ^A spelled out both DotsH and
            // DotsW the pitch is already explicit and any extra spacing
            // doubles the apparent character gap (e.g. 201 BARCLNA at
            // ^AGB,120,40 overflows the field).
            tracking = explicitSize ? 0 : ZebraTracking(pdf);
        }

        if (FrameBox != null && FrameBox.MaxWidth > 0)
        {
            DrawFramed(pdf, text, baselineOffset, tracking);
        }
        else
        {
            pdf.WriteRotatedText(pdf.X, pdf.Y, baselineOffset, this.Orientation, text, tracking);
        }
        pdf.GetPos();
    }

    private void DrawFramed(FPdf pdf, string text, double baselineOffset, double tracking)
    {
        var lines = WrapText(pdf, text, FrameBox!.MaxWidth, tracking);
        var maxLines = Math.Max(1, FrameBox.MaxLines);
        if (lines.Count > maxLines)
            lines = lines.GetRange(0, maxLines);

        // Line stride along the perpendicular-to-reading axis. Thickness is a
        // decent proxy for the line height when no explicit ^A size was given.
        var lineStride = this.Thickness + Math.Max(0, FrameBox.LineSpacing);
        var alignment = string.IsNullOrEmpty(FrameBox.Alignment) ? "L" : FrameBox.Alignment;

        // ^FO with B orientation: FO is the world top-left of the bounding
        // box. The first line's baseline sits at the BOTTOM of the box, which
        // in ZPL Y is FO_y + box_width. ^FT already specifies the baseline
        // directly so no anchor shift is needed.
        var anchorX = pdf.X;
        var anchorY = pdf.Y;
        if (Origin == OriginEnum.LeftTop)
        {
            switch (this.Orientation)
            {
                case "B":
                    anchorY += FrameBox.MaxWidth;
                    break;
                case "R":
                    anchorX += FrameBox.MaxWidth;
                    break;
                case "I":
                    anchorX += FrameBox.MaxWidth;
                    break;
            }
        }

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[ i ];
            var lineWidth = pdf.GetStringWidth(line) + tracking * Math.Max(0, line.Length - 1);
            var slack = Math.Max(0, FrameBox.MaxWidth - lineWidth);
            double alignOffset = alignment switch
            {
                "C" => slack / 2,
                "R" => slack,
                _   => 0, // L (default) and J (no real justify yet)
            };

            // ZPL hanging indent applies to the SECOND line onwards.
            var hangingIndent = i > 0 ? FrameBox.HangingIntent : 0;
            var lineOffset = i * lineStride;

            double lineX = anchorX;
            double lineY = anchorY;
            switch (this.Orientation)
            {
                case "N":
                    lineX += alignOffset + hangingIndent;
                    lineY += lineOffset;
                    break;
                case "B":
                    // Text reads bottom-up. Alignment offset moves the baseline
                    // toward the top of the box (= -Y in ZPL). Successive lines
                    // stack to the right of the previous one (+X in ZPL).
                    lineY -= alignOffset + hangingIndent;
                    lineX += lineOffset;
                    break;
                case "R":
                    lineY += alignOffset + hangingIndent;
                    lineX -= lineOffset;
                    break;
                case "I":
                    lineX -= alignOffset + hangingIndent;
                    lineY -= lineOffset;
                    break;
                default:
                    lineX += alignOffset + hangingIndent;
                    lineY += lineOffset;
                    break;
            }

            pdf.WriteRotatedText(lineX, lineY, baselineOffset, this.Orientation, line, tracking);
        }
    }

    private List<string> WrapText(FPdf pdf, string text, int maxWidth, double tracking)
    {
        var lines = new List<string>();
        if (maxWidth <= 0 || string.IsNullOrEmpty(text))
        {
            lines.Add(text ?? string.Empty);
            return lines;
        }

        var words = text.Split(' ');
        var current = new StringBuilder();
        foreach (var word in words)
        {
            var candidate = current.Length == 0 ? word : current.ToString() + " " + word;
            var width = pdf.GetStringWidth(candidate) + tracking * Math.Max(0, candidate.Length - 1);
            if (width <= maxWidth || current.Length == 0)
            {
                if (current.Length > 0) current.Append(' ');
                current.Append(word);
            }
            else
            {
                lines.Add(current.ToString());
                current.Clear();
                current.Append(word);
            }
        }
        if (current.Length > 0) lines.Add(current.ToString());
        return lines;
    }

    private double ZebraTracking(FPdf pdf)
    {
        if (this.Font == "0") return 0;
        // Zebra font A pitch at native height 9 is 6 dots (5 glyph + 1 gap).
        var targetPitch = this.Thickness * 6.0 / 9.0;
        var naturalAdvance = pdf.GetStringWidth("M");
        return Math.Max(0, targetPitch - naturalAdvance);
    }


    private void DrawBarcodeCode128(FPdf pdf, string arg2)
    {
        // ZPL lets the field data pick the Code 128 subset via a ">x" prefix:
        //   >; = Subset C (digits in pairs)
        //   >: = Subset A
        //   >> = Subset B (the default)
        var hint = EncodeHintType.CODE128_FORCE_CODESET_B;
        if (arg2.StartsWith(">;"))
        {
            hint = EncodeHintType.CODE128_COMPACT;
            arg2 = arg2.Substring(2);
        }
        else if (arg2.StartsWith(">:") || arg2.StartsWith(">>"))
        {
            arg2 = arg2.Substring(2);
        }

        var barcode = new Code128Writer();
        var code128 = barcode.encode(arg2, new Dictionary<EncodeHintType, object>() { { hint, (true) } });
        var x = pdf.X;
        var width = AddBarcode(pdf, code128, pdf.X, y: null) - pdf.X;

        if (this.Barcode128Options.Line && !this.Barcode128Options.LineAbove)
        {
            pdf.Ln(this.BarcodeOptions.Height + 4);
            this.TextMode = FieldMode.Text;
            this.Thickness = 50;
            DrawHumanReadable(pdf, arg2, barcodeLeft: x, barcodeWidth: width);
        }
    }

    private void DrawHumanReadable(FPdf pdf, string text, double barcodeLeft, double barcodeWidth)
    {
        var fontPoints = (Convert.ToDouble(this.Thickness) / Dpi) * 25.4 * 2.54;
        pdf.SetFont(this.MonospaceFont, this.MonospaceStyle ?? "", 0, null);
        pdf.SetFontSize(fontPoints);
        pdf.FontScale.ScaleX = 1;
        pdf.FontScale.ScaleY = 1;
        var tracking = ZebraTracking(pdf);
        // GetStringWidth already returns the width in user units (FontSize is
        // stored divided by k). Account for the per-char tracking so the
        // centring stays correct.
        var textWidthUser = pdf.GetStringWidth(text) + tracking * Math.Max(0, text.Length - 1);
        var indent = Math.Max(0, (barcodeWidth - textWidthUser) / 2);
        pdf.SetX(barcodeLeft + indent);
        pdf.WriteRotatedText(pdf.X, pdf.Y, this.Thickness * 0.7, "N", text, tracking);
    }
    /// <summary>
    /// Generic 1D barcode renderer. Encodes the text through any
    /// ZXing Writer using the universal BitMatrix encode overload
    /// (reading row 0 as the module pattern), then stamps the bars at
    /// the field origin. ^B?B (orientation B) draws horizontal bars
    /// stacked upward from the anchor.
    /// </summary>
    private void DrawBarcode1D(FPdf pdf, string data, ZXing.Writer writer, ZXing.BarcodeFormat format)
    {
        var opts = this.Barcode1DOptions;
        ZXing.Common.BitMatrix matrix;
        try { matrix = writer.encode(data, format, 0, 0); }
        catch { return; } // bad data for the format — ignore field

        // Row 0 of the matrix is the module pattern for any 1D writer.
        var bitmap = new bool[ matrix.Width ];
        for (int i = 0; i < matrix.Width; i++) bitmap[ i ] = matrix[ i, 0 ];

        var w = this.BarcodeOptions.Width;
        var height = opts.Height > 0 ? opts.Height : this.BarcodeOptions.Height;
        var orientation = string.IsNullOrEmpty(opts.Orientation) ? "N" : opts.Orientation;

        if (orientation == "B")
        {
            DrawRotatedBars(pdf, bitmap, pdf.X, pdf.Y, w, height);
        }
        else
        {
            var saved = this.BarcodeOptions.Height;
            this.BarcodeOptions.Height = height;
            try { AddBarcode(pdf, bitmap, pdf.X, y: null); }
            finally { this.BarcodeOptions.Height = saved; }
        }

        if (opts.Line && orientation == "N")
        {
            pdf.Ln(height + 4);
            this.TextMode = FieldMode.Text;
            this.Thickness = 50;
            DrawHumanReadable(pdf, data, barcodeLeft: pdf.X, barcodeWidth: w * bitmap.Length);
        }
    }

    /// <summary>
    /// Generic 2D barcode renderer (QR, Data Matrix, PDF417, Aztec).
    /// Stamps each module as a Magnification-by-Magnification square at
    /// the current field origin. Orientation is honoured by rotating
    /// the matrix walking order before stamping.
    /// </summary>
    private void DrawBarcode2D(FPdf pdf, string data, ZXing.Writer writer, ZXing.BarcodeFormat format)
    {
        var opts = this.Barcode2DOptions;
        ZXing.Common.BitMatrix matrix;
        try { matrix = writer.encode(data, format, 0, 0); }
        catch { return; }

        var module = Math.Max(1, opts.Magnification);
        var orientation = string.IsNullOrEmpty(opts.Orientation) ? "N" : opts.Orientation;

        var anchorX = pdf.X;
        var anchorY = pdf.Y;
        for (int row = 0; row < matrix.Height; row++)
        {
            for (int col = 0; col < matrix.Width; col++)
            {
                if (!matrix[ col, row ]) continue;
                // Apply the orientation to (col, row). N: no change.
                // B: 90° CCW — bottom-left becomes top-left in world.
                // R: 90° CW. I: 180°.
                int u, v;
                switch (orientation)
                {
                    case "B":
                        u = row;
                        v = matrix.Width - 1 - col;
                        break;
                    case "R":
                        u = matrix.Height - 1 - row;
                        v = col;
                        break;
                    case "I":
                        u = matrix.Width - 1 - col;
                        v = matrix.Height - 1 - row;
                        break;
                    default: // "N"
                        u = col;
                        v = row;
                        break;
                }
                var absX = anchorX + u * module;
                var absY = anchorY + v * module;
                DrawAbsoluteRect(pdf, absX, absY, module, module, Color.Black);
            }
        }
    }

    private double AddBarcode(FPdf pdf, bool[] bitmap, double? x, double? y, Color? color = null)
    {
        var w = this.BarcodeOptions.Width; // default 5
        var height = this.BarcodeOptions.Height; // default 10
        var widthRatio = this.BarcodeOptions.WidthRatio; // default 3 // Unused, not sure about it

        color = color ?? Color.Black;
        var index = 0;
        var count = bitmap.Length;
        var left = x ?? 0;
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

                pdf.DrawArea(color, 0.00, points);
                index += trueCount;
            }

            index++;
        }

        var right = left + w * index;
        return right;
    }

    private void DrawBarcodeI2of5(FPdf pdf, string data)
    {
        var opts = this.Barcode2of5Options;
        // ITF requires an even number of digits; pad with a leading zero
        // when the input is odd (matches the Zebra default behaviour).
        if (data.Length % 2 == 1)
            data = "0" + data;

        var writer = new ITFWriter();
        var bitmap = writer.encode(data);

        var w = this.BarcodeOptions.Width;
        var orientation = string.IsNullOrEmpty(opts.Orientation) ? "N" : opts.Orientation;
        var height = opts.Height > 0 ? opts.Height : this.BarcodeOptions.Height;

        if (orientation == "B")
        {
            DrawRotatedBars(pdf, bitmap, pdf.X, pdf.Y, w, height);
        }
        else
        {
            // Reuse the existing N-orientation path. AddBarcode reads height
            // from BarcodeOptions.Height, so make sure it's the ^B2 height.
            var saved = this.BarcodeOptions.Height;
            this.BarcodeOptions.Height = height;
            try { AddBarcode(pdf, bitmap, pdf.X, y: null); }
            finally { this.BarcodeOptions.Height = saved; }
        }

        // Human-readable line. ^B2 line=N is common in vertical labels (the
        // ECB number is rendered as a separate ^FD field), so we only emit
        // it when explicitly enabled.
        if (opts.Line && orientation == "N")
        {
            pdf.Ln(height + 4);
            this.TextMode = FieldMode.Text;
            this.Thickness = 50;
            var barcodeWidth = w * bitmap.Length;
            DrawHumanReadable(pdf, data, barcodeLeft: pdf.X, barcodeWidth: barcodeWidth);
        }
    }

    /// <summary>
    /// Draw a 1-D barcode bitmap as a stack of horizontal bars, with the
    /// bottom-right corner of the resulting barcode at (anchorX, anchorY) —
    /// the ZPL convention for ^B?B (orientation B, 270° CW). Each "true"
    /// run in the bitmap becomes one bar.
    /// </summary>
    private void DrawRotatedBars(FPdf pdf, bool[] bitmap, double anchorX, double anchorY, int moduleWidth, int barLength)
    {
        var index = 0;
        while (index < bitmap.Length)
        {
            if (bitmap[ index ])
            {
                var trueCount = bitmap.Skip(index + 1).TakeWhile(b => b).Count();
                var thickness = moduleWidth * (trueCount + 1);

                // Bar spans X = [anchorX - barLength, anchorX], Y = a
                // moduleWidth-thick slice of (anchorY - thickness, anchorY]
                // shifted up by the current module index.
                var yBottom = anchorY - index * moduleWidth;
                var yTop = yBottom - thickness;
                var rectX = anchorX - barLength;
                var rectW = barLength;
                DrawAbsoluteRect(pdf, rectX, yTop, rectW, yBottom - yTop, Color.Black);
                index += trueCount;
            }
            index++;
        }
    }

    private static void DrawAbsoluteRect(FPdf pdf, double absX, double absY, double w, double h, Color color)
    {
        // FPdf.DrawArea is relative to pdf.Y; convert to that convention.
        var relY = absY - pdf.Y;
        var points = new[]
        {
            new DrawingPoint(absX,         relY),
            new DrawingPoint(absX + w,     relY),
            new DrawingPoint(absX + w,     relY + h),
            new DrawingPoint(absX,         relY + h),
            new DrawingPoint(absX,         relY),
        };
        pdf.DrawArea(color, 0.00, points);
    }

    public CharSize GetFontSize(string font, int dpi)
    {
        //if (dpi == 300)
        {
            /*
                A 9 , 5 U-L-D 0.030 , 0.020 50.8 0.75 , 0.50 2.02
                B 11 , 7 U 0.036 , 0.030 33.8 0.91 , 0.75 1.32
                C, D 18 , 10 U-L-D 0.059 , 0.040 25.4 1.50 , 1.00 1.00
                E 42 , 20 OCR-B 0.138 , 0.085 23.4 1.75 , 1.08 0.92
                F 26 , 13 U-L-D 0.085 , 0.053 19.06 2.16 , 1.34 0.74
                G 60 , 40 U-L-D 0.197 , 0.158 6.36 5.00 , 4.00 0.25
                H 34 , 22 OCR-A 0.111 , 0.098 10.20 2.81 , 2.48 0.40
                GS 24 , 24 SYMBOL 0.079 , 0.079 12.70 1.99 , 1.99 0.52
                P 20 , 18 U-L-D 0.067 , 0.060 N/A 1.69 , 1.52 N/A
                Q 28 , 24 U-L-D 0.093 , 0.080 N/A 2.37 , 2.03 N/A
                R 35 , 31 U-L-D 0.117 , 0.103 N/A 2.96 , 2.62 N/A
                S 40 , 35 U-L-D 0.133 , 0.177 N/A 3.39 , 2.96 N/A
                T 48 , 42 U-L-D 0.160 , 0.140 N/A 4.06 , 3.56 N/A
                U 59 , 53 U-L-D 0.197 , 0.177 N/A 5.00 , 4.49 N/A
                V 80 , 71 U-L-D 0.267 , 0.237 N/A 6.77 , 6.01 N/             
             */

            var sizes = new Dictionary<string, CharSize>()
            {
                { "A", new CharSize( "A", 9, 5, 0.030, 0.020, 50.8, 0.75, 0.50, 2.02 ) },
                { "B", new CharSize( "B", 11, 7, 0.036, 0.030, 33.8, 0.91, 0.75, 1.32 ) },
                { "C", new CharSize( "C", 18, 10, 0.059, 0.040, 25.4, 1.50, 1.00, 1.00 ) },
                { "D", new CharSize( "D", 18, 10, 0.059, 0.040, 25.4, 1.50, 1.00, 1.00 ) },
                { "E", new CharSize( "E", 42, 20, 0.138, 0.085, 23.4, 1.75, 1.08, 0.92 ) },
                { "F", new CharSize( "F", 26, 13, 0.085, 0.053, 19.06, 2.16, 1.34, 0.74 ) },
                { "G", new CharSize( "G", 60, 40, 0.197, 0.158, 6.36, 5.00, 4.00, 0.25 ) },
                { "H", new CharSize( "H", 34, 22, 0.111, 0.098, 10.20, 2.81, 2.48, 0.40 ) },
                { "GS",new CharSize( "GS", 24, 24, 0.079, 0.079, 12.70, 1.99, 1.99, 0.52 ) },
                { "P", new CharSize( "P", 20, 18, 0.067, 0.060, 0, 1.69, 1.52, 0 ) },
                //{ "R", new CharSize( "Q", 28, 24, 0.093, 0.080, 0, 2.37, 2,0) },
                //{ "S", new CharSize( "Q", 0, 0, 0.093, 0.080, 0, 2.37, 2,0) }, // INCOMPLETA
                //{ "T", new CharSize( "Q", 0, 0, 0.093, 0.080, 0, 2.37, 2,0) }, // INCOMPLETA
                //{ "U", new CharSize( "Q", 0, 0, 0.093, 0.080, 0, 2.37, 2,0) }, // INCOMPLETA
                //{ "V", new CharSize( "Q", 0, 0, 0.093, 0.080, 0, 2.37, 2,0) }, // INCOMPLETA
                //{ "0", new CharSize( "Q", 0, 0, 0.093, 0.080, 0, 2.37, 2,0) }, // INCOMPLETA
                { "0", new CharSize( "0", 26, 13, 0.085, 0.053, 19.06, 2.16, 1.34, 0.74 ) },

            };

            //if (sizes.ContainsKey(font))
            return sizes[ font ];
            //return sizes[ this.DefaultFont ];
        }
    }

}