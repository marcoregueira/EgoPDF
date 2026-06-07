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
    // Three independent instances of Barcode1DOptions so a label that
    // alternates ^BC / ^B2 / ^B3 etc. doesn't leak parameters between
    // them (each ^B? handler writes to its own instance).
    public Barcode1DOptions Barcode128Options { get; } = new Barcode1DOptions();
    public Barcode1DOptions Barcode2of5Options { get; } = new Barcode1DOptions();
    public Barcode1DOptions Barcode1DOptions { get; } = new Barcode1DOptions();
    public Barcode2DOptions Barcode2DOptions { get; } = new Barcode2DOptions();
    public string EscapeCharacter { get; set; }
    public int Dpi { get; internal set; }
    public string MonospaceFont { get; internal set; }
    public string MonospaceStyle { get; internal set; } = "";
    public string VariableFont { get; internal set; }
    public string VariableStyle { get; internal set; } = "";
    public string CondensedFont { get; internal set; }
    public string CondensedStyle { get; internal set; } = "";
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
            DispatchBarcode(pdf, text);
            return;
        }

        var fontsize = GetFontSize(this.Font, Dpi);
        pdf.SavePos();
        var fontPoints = (Convert.ToDouble(this.Thickness) / Dpi) * 25.4 * 2.54;
        pdf.SetFontSize(fontPoints);

        // ^FO places the FO point at the top-left of the bounding box, so the
        // baseline lives Thickness * ascentRatio below it. ^FT (Origin =
        // LeftBottom) places the FO point ON the baseline, so no offset.
        // The default 0.7 is calibrated for helvetica (ascent ≈ 0.72 em);
        // Roboto Condensed has a much taller ascent (≈ 0.93 em), so when
        // we render P-V through the condensed slot we'd push the glyph
        // top above the FO point and leave a wide gap below it. Pick the
        // multiplier per font slot.
        double ascentRatio = 0.7;
        if (this.Font is "P" or "Q" or "R" or "S" or "T" or "U" or "V")
        {
            // 0.85 is a compromise between helvetica's 0.72-ish ascent and
            // Roboto Condensed's ~0.93. Lower numbers (like 0.7) leave the
            // tracking glyphs glued to the page edge with a big gap to the
            // separator line below; higher numbers (0.93) drop fields like
            // 27004 visibly below where Labelary renders them. 0.85 keeps
            // both within a few dots of expected.
            ascentRatio = !string.IsNullOrEmpty(this.CondensedFont) ? 0.85 : 0.72;
        }
        var baselineOffset = Origin == OriginEnum.LeftBottom ? 0 : this.Thickness * ascentRatio;

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

            // Fonts P-V are the scalable proportional U-L-D set in Zebra's
            // catalogue. For these slots we prefer a condensed PDF font if
            // the host configured one (SetCondensedFont) — that's the only
            // way to land near Labelary's tight glyph widths without
            // shipping a font asset. Otherwise fall back to the variable
            // (proportional) font and fake narrow with PDF horizontal text
            // scale (Tz) further down.
            var isProportional = this.Font is "P" or "Q" or "R" or "S" or "T" or "U" or "V";
            var hasCondensed = isProportional && !string.IsNullOrEmpty(this.CondensedFont);
            if (hasCondensed)
                pdf.SetFont(this.CondensedFont, this.CondensedStyle ?? "", 0, null);
            else if (isProportional && !string.IsNullOrEmpty(this.VariableFont))
                pdf.SetFont(this.VariableFont, this.VariableStyle ?? "", 0, null);
            else
                pdf.SetFont(this.MonospaceFont, this.MonospaceStyle ?? "", 0, null);

            if (explicitSize)
            {
                if (isProportional && hasCondensed)
                {
                    // Zebra V honours the bitmap's native aspect (h=80,
                    // w=71 -> h/w = 80/71 ≈ 1.127): when the caller asks
                    // for a w narrower than w_natural_for_h the whole
                    // glyph scales down proportionally (so the h drops
                    // too). When w is wider than natural the chars
                    // stretch horizontally while h stays. This explains
                    // why the GLS tracking ^AVN,120,100 renders ~2x
                    // taller than 27004 ^AVN,105,50: the second hits the
                    // narrow branch and h collapses to 56 dots.
                    const double nativeVAspectHW = 80.0 / 71.0;
                    const double nativeVAspectWH = 71.0 / 80.0;
                    var requestedAspect = (double)charW / charH;
                    double effectiveEm;
                    if (requestedAspect < nativeVAspectWH)
                    {
                        // Narrow request -> aspect-locked: h scales with w.
                        effectiveEm = charW * nativeVAspectHW;
                        this.ScaleX = 1.0;
                    }
                    else
                    {
                        // Wide request -> keep h, stretch chars.
                        effectiveEm = charH;
                        this.ScaleX = requestedAspect;
                    }
                    fontPoints = effectiveEm * pdf.k;
                    pdf.SetFontSize(fontPoints);
                    // For the narrow branch (effective em < charH) the
                    // glyph fits inside a smaller cell than the ZPL ^h
                    // would imply; bottom-align inside the em cell so
                    // the digits sit on FO_y + em rather than floating
                    // high. The wide branch keeps the original ratio
                    // because there the cell IS the em and bumping the
                    // baseline further down (1.0) would push XXX into
                    // the next field below (^AEN,90 PRUEBAS at +120).
                    var narrowBranch = requestedAspect < nativeVAspectWH;
                    var emRatio = narrowBranch ? 1.0 : ascentRatio;
                    baselineOffset = Origin == OriginEnum.LeftBottom
                        ? 0
                        : effectiveEm * emRatio;

                    // Auto-compress if the per-spec render would overflow
                    // the remaining strip (tracking spills 14 * 100 dots
                    // into the 549-dot space and has to fit somehow).
                    // Target 85% of the strip so the GLS tracking row
                    // lands near Labelary's ~700-dot endpoint instead of
                    // the page edge.
                    pdf.FontScale.ScaleX = this.ScaleX; // for GetStringWidth callers reading current scale
                    var naturalTextWidth = pdf.GetStringWidth(text);
                    var renderedWidth = naturalTextWidth * this.ScaleX;
                    var available = (pdf.W - pdf.X) * 0.85;
                    if (renderedWidth > available && available > 0)
                        this.ScaleX *= available / renderedWidth;
                }
                else if (isProportional)
                {
                    // Helvetica fallback (no condensed font registered).
                    // Keep the older aspect-direct rule -- without a font
                    // designed for V's proportions we have nothing better
                    // to mimic.
                    fontPoints = charH * pdf.k;
                    pdf.SetFontSize(fontPoints);
                    var requestedAspect = (double)charW / charH;
                    this.ScaleX = requestedAspect;
                    pdf.FontScale.ScaleX = this.ScaleX;
                    var naturalTextWidth = pdf.GetStringWidth(text);
                    var renderedWidth = naturalTextWidth * this.ScaleX;
                    var available = (pdf.W - pdf.X) * 0.85;
                    if (renderedWidth > available && available > 0)
                        this.ScaleX *= available / renderedWidth;
                }
                else
                {
                    // Monospace bitmap fonts (A-H, O): width parameter is
                    // the per-char advance, so stretch every glyph to charW.
                    fontPoints = charH * pdf.k;
                    pdf.SetFontSize(fontPoints);
                    var naturalAdvancePt = 0.6 * fontPoints;
                    var targetAdvancePt = charW * pdf.k;
                    this.ScaleX = naturalAdvancePt > 0 ? targetAdvancePt / naturalAdvancePt : 1;
                }
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

        // ^FR (Field Reverse): the field's pixels should invert whatever
        // is already on the label at the same position. ZPL achieves this
        // with a 1-bit XOR; PDF has no native XOR for vector content, so
        // we approximate the common case -- text on top of a previously
        // painted dark ^GB rect -- by painting the glyphs in white. Any
        // dark fill below shows through where the glyph isn't; the glyph
        // itself reads as "white text on the dark background". The trick
        // breaks down (white-on-white = invisible) when ^FR text isn't
        // sitting over something opaque, which mirrors a real ZPL author
        // error rather than an engine bug. PushState snapshots and
        // restores the text colour so the next field is unaffected.
        using var reverseScope = this.Reverse ? pdf.PushState() : null;
        if (this.Reverse) pdf.SetTextColor(Color.White);

        if (FrameBox != null && FrameBox.MaxWidth > 0)
        {
            DrawFramed(pdf, text, baselineOffset, tracking);
        }
        else
        {
            // Non-^FB path: ^FO + B should anchor the rotated bbox top-left
            // (GLS courier label DESTINATARIO et al.). ^FT + B keeps the
            // first-char baseline anchor by passing useTopLeftBboxAnchor=
            // false. DrawFramed's per-line calls below pass false too --
            // the field-level Origin is already absorbed into lineY.
            pdf.WriteRotatedTextZpl(pdf.X, pdf.Y, baselineOffset, this.Orientation, text, tracking,
                useTopLeftBboxAnchor: Origin == OriginEnum.LeftTop);
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

            pdf.WriteRotatedTextZpl(lineX, lineY, baselineOffset, this.Orientation, line, tracking);
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
    /// Map every simple-1D BarcodeMode to the ZXing Writer + format
    /// pair the generic <see cref="DrawBarcode1D"/> pipeline drives.
    /// Adding a new symbology that fits the "single Writer, single
    /// format, no codeset hint" pattern is a one-line entry here.
    /// Code 128 and Interleaved 2 of 5 have their own renderers (codeset
    /// prefix and odd-length padding) so they stay out of the table.
    /// </summary>
    private static readonly Dictionary<BarcodeMode, (Func<ZXing.Writer> writer, ZXing.BarcodeFormat format)> Simple1DWriters = new()
    {
        [BarcodeMode.Code39]  = (() => new ZXing.OneD.Code39Writer(),  ZXing.BarcodeFormat.CODE_39),
        [BarcodeMode.Codabar] = (() => new ZXing.OneD.CodaBarWriter(), ZXing.BarcodeFormat.CODABAR),
        [BarcodeMode.EAN13]   = (() => new ZXing.OneD.EAN13Writer(),   ZXing.BarcodeFormat.EAN_13),
        [BarcodeMode.EAN8]    = (() => new ZXing.OneD.EAN8Writer(),    ZXing.BarcodeFormat.EAN_8),
        [BarcodeMode.UPC_A]   = (() => new ZXing.OneD.UPCAWriter(),    ZXing.BarcodeFormat.UPC_A),
        [BarcodeMode.UPC_E]   = (() => new ZXing.OneD.UPCEWriter(),    ZXing.BarcodeFormat.UPC_E),
        [BarcodeMode.MSI]     = (() => new ZXing.OneD.MSIWriter(),     ZXing.BarcodeFormat.MSI),
    };

    /// <summary>
    /// Same shape as <see cref="Simple1DWriters"/> for the 2D family --
    /// QR, Data Matrix, PDF417 and Aztec all route through
    /// <see cref="DrawBarcode2D"/>.
    /// </summary>
    private static readonly Dictionary<BarcodeMode, (Func<ZXing.Writer> writer, ZXing.BarcodeFormat format)> Simple2DWriters = new()
    {
        [BarcodeMode.QrCode]     = (() => new ZXing.QrCode.QRCodeWriter(),         ZXing.BarcodeFormat.QR_CODE),
        [BarcodeMode.DataMatrix] = (() => new ZXing.Datamatrix.DataMatrixWriter(), ZXing.BarcodeFormat.DATA_MATRIX),
        [BarcodeMode.PDF417]     = (() => new ZXing.PDF417.PDF417Writer(),         ZXing.BarcodeFormat.PDF_417),
        [BarcodeMode.Aztec]      = (() => new ZXing.Aztec.AztecWriter(),           ZXing.BarcodeFormat.AZTEC),
    };

    private void DispatchBarcode(FPdf pdf, string text)
    {
        if (Simple1DWriters.TryGetValue(this.BarcodeMode, out var w1))
        {
            DrawBarcode1D(pdf, text, w1.writer(), w1.format);
            return;
        }
        if (Simple2DWriters.TryGetValue(this.BarcodeMode, out var w2))
        {
            DrawBarcode2D(pdf, text, w2.writer(), w2.format);
            return;
        }
        // Specialised renderers: Code 128 honours the >; / >: / >> codeset
        // prefix on the field data; Interleaved 2 of 5 pads odd-length
        // digit strings with a leading zero. Both also use options
        // classes the simple pipeline doesn't know about.
        switch (this.BarcodeMode)
        {
            case BarcodeMode.Code128:         DrawBarcodeCode128(pdf, text); break;
            case BarcodeMode.Interleaved2of5: DrawBarcodeI2of5(pdf, text); break;
        }
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
        => PdfZpl.DrawFilledRect(pdf, absX, absY, w, h, color);

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
                { "E", new CharSize( "E", 28, 15, 0.138, 0.085, 23.4, 1.75, 1.08, 0.92 ) },
                { "F", new CharSize( "F", 26, 13, 0.085, 0.053, 19.06, 2.16, 1.34, 0.74 ) },
                { "G", new CharSize( "G", 60, 40, 0.197, 0.158, 6.36, 5.00, 4.00, 0.25 ) },
                { "H", new CharSize( "H", 17, 11, 0.111, 0.098, 10.20, 2.81, 2.48, 0.40 ) },
                { "GS",new CharSize( "GS", 24, 24, 0.079, 0.079, 12.70, 1.99, 1.99, 0.52 ) },
                { "P", new CharSize( "P", 20, 18, 0.067, 0.060, 0, 1.69, 1.52, 0 ) },
                // Q-V are proportional U-L-D scalable; CharsPerInch is N/A
                // per Zebra's datasheet, so InChars / MmChars stay at 0.
                { "Q", new CharSize( "Q", 28, 24, 0.093, 0.080, 0, 2.37, 2.03, 0 ) },
                { "R", new CharSize( "R", 35, 31, 0.117, 0.103, 0, 2.96, 2.62, 0 ) },
                { "S", new CharSize( "S", 40, 35, 0.133, 0.177, 0, 3.39, 2.96, 0 ) },
                { "T", new CharSize( "T", 48, 42, 0.160, 0.140, 0, 4.06, 3.56, 0 ) },
                { "U", new CharSize( "U", 59, 53, 0.197, 0.177, 0, 5.00, 4.49, 0 ) },
                { "V", new CharSize( "V", 80, 71, 0.267, 0.237, 0, 6.77, 6.01, 0 ) },
                { "0", new CharSize( "0", 15, 12, 0.075, 0.060, 0, 1.88, 1.88, 0 ) },
            };

            // Fall back to "A" rather than KeyNotFoundException for any
            // font slot we don't know about. KNFE used to be swallowed
            // by PdfZpl.PrintWithData's catch-all and the entire field
            // silently disappeared; with the fallback the worst case is
            // a field drawn at the wrong size, which is loud enough to
            // notice and easy to add to the table here.
            return sizes.TryGetValue(font, out var size) ? size : sizes[ "A" ];
        }
    }

}