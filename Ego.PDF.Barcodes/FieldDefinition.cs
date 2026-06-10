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

    /// <summary>
    /// Per-field copy of <see cref="PdfZpl.CondensedFontScale"/>: a 1.0+
    /// multiplier applied to fontPoints whenever this field renders with
    /// the condensed slot, so a host can dial Roboto Condensed +10%
    /// without touching individual ^A?h,w numbers.
    /// </summary>
    public double CondensedFontScale { get; set; } = 1.0;

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
            // Zebra's Font 0 is Triumvirate Bold Condensed — a CONDENSED
            // proportional sans, not the wider Helvetica-style VariableFont
            // would otherwise pick. Prefer the host's CondensedFont when
            // one is registered (matches the P-V proportional slots), and
            // only fall back to VariableFont when no condensed asset is
            // available.
            if (!string.IsNullOrEmpty(this.CondensedFont))
            {
                pdf.SetFont(this.CondensedFont, this.CondensedStyle ?? "", 0, null);
                if (this.CondensedFontScale != 1.0)
                    pdf.SetFontSize(fontPoints * this.CondensedFontScale);
            }
            else if (!string.IsNullOrEmpty(this.VariableFont))
                pdf.SetFont(this.VariableFont, this.VariableStyle ?? "", 0, null);

            // ^A0h,w with an explicit width parameter: COMPRESS glyphs
            // horizontally when the requested w is narrower than the
            // font's natural "M" advance at em=DotsH. Without this, large
            // fields (CL1 at ^A0R,140,90) render the CondensedFont at its
            // natural ~95-dot advance — visibly wider than Labelary, which
            // honours the ZPL w parameter. We only clamp DOWN (Math.Min):
            // small body text like ^A0R,25,28 would otherwise be stretched
            // far past Labelary's render because DotsW for body text is
            // typically much larger than the condensed M advance, and
            // expansion to match it doesn't reflect what Zebra does in
            // practice. Only kicks in when both h and w are explicit;
            // single-arg ^A0R,25 falls back to the font's natural metrics.
            if (DotsH > 0 && DotsW > 0)
            {
                var naturalAdvanceUU = pdf.GetStringWidth("M");
                var emUU = pdf.FontSize;
                var naturalAspectWH = emUU > 0 ? naturalAdvanceUU / emUU : 0.6;
                var requestedAspectWH = (double)DotsW / DotsH;
                this.ScaleX = naturalAspectWH > 0
                    ? Math.Min(1.0, requestedAspectWH / naturalAspectWH)
                    : 1;
                this.ScaleY = 1;
                pdf.FontScale.ScaleX = this.ScaleX;
                pdf.FontScale.ScaleY = this.ScaleY;
            }
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
                    // ZPL ^A?h,w sizing for proportional slots (Font 0 +
                    // P-V) using Labelary's pixel-perfect quantization
                    // rule, reverse-engineered from its rendered PDFs.
                    // Derived from inspecting Labelary's PDF (Font2 Tf/Tz
                    // ops) on the SEUR 1485:
                    //
                    //   WIDE (w > h × native_aspect):
                    //     em = h² × native_aspect / w   (= h × native / ratio)
                    //     Tz = ratio² / native
                    //     ⇒ em × Tz = h × native × ratio = w * native
                    //   NARROW (w ≤ h × native_aspect):
                    //     em = h × ~0.92    (Font2 cap-height-ish constant)
                    //     Tz = sqrt(w/h) / 0.92
                    //
                    // Wide validated by: VALENCIA ^ASR,50,70 → em=31.25
                    // (Labelary 33.8), Tz=2.24 (Labelary 1.98); 46520
                    // ^AQR,130,132 → em=109.7 (115), Tz=1.20 (1.188).
                    // Narrow Q validated by: C.B ^AQR,50,40 → em=46
                    // (matches Labelary), Tz=0.972 (0.99); PUERTO
                    // ^AQR,50,9 → em=46, Tz=0.461 (0.495).
                    // Narrow P (0 Eur ^APR,57,42) still off: predicted
                    // em=52.4 / Tz=0.93, observed 48.6 / 0.667. Likely
                    // a Font P-specific extra compression that we haven't
                    // reverse-engineered yet.
                    var nativeAspectWH = fontsize.DotsH > 0
                        ? (double)fontsize.DotsW / fontsize.DotsH
                        : 0.875;
                    // Labelary's pixel-perfect quantization rule, same as
                    // the monospace bitmap path: quantize h and w to
                    // integer multiples of the slot's native cell, then
                    // build em / Tz from those multiples.
                    //   hMul  = round(h / native_h)
                    //   wMul  = round(w / native_w)
                    //   em    = max(native_h × 0.82,  0.81 × hMul × native_h)
                    //   Tz    = wMul / hMul
                    // The 0.81 / 0.82 factors are empirical from a Font P
                    // probe (h=30/60/120 × w=15/30/60/120/240) plus all
                    // the SEUR 1485 fields; predicted vs observed em is
                    // exact for Font P and within ~2% for Font Q / S /
                    // narrow Q-small-h. Predicted vs observed Tz is exact
                    // for ratio ≤ 1 and ~5% off for moderate wide cases;
                    // only very wide w (rendered_M > 10 × hMul) misbehaves
                    // (Labelary appears to cap N there).
                    // AwayFromZero matters at midpoints: round(60/28 =
                    // 2.143) is 2 with either rule, but round(70/28 = 2.5)
                    // should be 3 (Zebra) not 2 (banker's).
                    var nativeH = Math.Max(1, fontsize.DotsH);
                    var nativeW = Math.Max(1, fontsize.DotsW);
                    var hMul = Math.Max(1, (int)Math.Round((double)charH / nativeH, MidpointRounding.AwayFromZero));
                    var wMul = Math.Max(1, (int)Math.Round((double)charW / nativeW, MidpointRounding.AwayFromZero));
                    var emQuant = 0.81 * hMul * nativeH;
                    var emFloor = nativeH * 0.82;
                    var emDots = Math.Max(emFloor, emQuant);
                    double emFactor = emDots / charH;
                    double wScale = (double)wMul / hMul;
                    wScale *= this.CondensedFontScale;

                    // Measure both fonts at the squat em so the resulting
                    // ScaleX produces a render that matches the metric
                    // font's per-text proportional width × wScale.
                    fontPoints = charH * pdf.k * this.CondensedFontScale * emFactor;
                    pdf.SetFontSize(fontPoints);
                    // wScale (= wMul/hMul, Labelary's pixel-perfect Tz)
                    // applied directly to the render font's own width.
                    // We used to anchor the target width to Liberation Sans
                    // Narrow Bold (a free Triumvirate proxy) so the cell
                    // matched Labelary regardless of which family the
                    // host registered as CondensedFont, but the residual
                    // Roboto-vs-Liberation drift (~4% on most fields) was
                    // smaller than the inherent slot-to-slot mismatch and
                    // not worth the extra TTF dependency.
                    this.ScaleX = wScale;
                    this.ScaleY = 1.0;
                    baselineOffset = Origin == OriginEnum.LeftBottom
                        ? 0
                        : charH * ascentRatio * emFactor;
                    pdf.FontScale.ScaleX = this.ScaleX;
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
                    if (this.Orientation == "N" || string.IsNullOrEmpty(this.Orientation))
                    {
                        var naturalTextWidth = pdf.GetStringWidth(text);
                        var renderedWidth = naturalTextWidth * this.ScaleX;
                        var available = (pdf.W - pdf.X) * 0.85;
                        if (renderedWidth > available && available > 0)
                            this.ScaleX *= available / renderedWidth;
                    }
                }
                else
                {
                    // Monospace bitmap fonts (A-H, O): Labelary's pixel-
                    // perfect rule, extracted from its rendered PDF on the
                    // SEUR 1485:
                    //     em      = hMul × native_h   where hMul = round(h/native_h)
                    //     Tz      = wMul / hMul       where wMul = round(w/native_w)
                    // Labelary's Font1 (monospace) advances 0.667 of em per
                    // char, so each rendered glyph occupies
                    //   em × 0.667 × Tz = wMul × native_h × 0.667 dots.
                    // For our Roboto Mono Bold the advance ratio is slightly
                    // lower, so derive ScaleX by measuring and matching the
                    // Labelary target width per char. ^ADR,45,25 → hMul=3,
                    // wMul=3, em=54, target advance = 3 × 18 × 0.667 = 36
                    // dots; matches Labelary's F1 19.15pt Tz=100% render.
                    const double labelaryFont1AdvanceRatio = 0.667;
                    // C#'s default Math.Round is banker's rounding
                    // (ties-to-even): 45/18 = 2.5 rounds to 2 instead of 3.
                    // Real Zebra printers round half-up — explicit
                    // MidpointRounding.AwayFromZero matches that and lands
                    // ^ADR,45,25 at hMul=3 (em=54) as Labelary does.
                    var hMul = Math.Max(1, (int)Math.Round((double)charH / Math.Max(1, fontsize.DotsH), MidpointRounding.AwayFromZero));
                    var wMul = Math.Max(1, (int)Math.Round((double)charW / Math.Max(1, fontsize.DotsW), MidpointRounding.AwayFromZero));
                    fontPoints = hMul * fontsize.DotsH * pdf.k;
                    pdf.SetFontSize(fontPoints);
                    var naturalAdvance = pdf.GetStringWidth("M");
                    var targetAdvance = wMul * fontsize.DotsH * labelaryFont1AdvanceRatio;
                    this.ScaleX = naturalAdvance > 0 ? targetAdvance / naturalAdvance : 1.0;
                }
                this.ScaleY = 1;
            }
            else
            {
                // Mirror the explicit-size branch above: when the field is
                // landing on the condensed slot, scale the size by the
                // host's CondensedFontScale (1.0 by default).
                var hasCondensedHere = (this.Font is "P" or "Q" or "R" or "S" or "T" or "U" or "V")
                                       && !string.IsNullOrEmpty(this.CondensedFont);
                pdf.SetFontSize(hasCondensedHere ? fontPoints * this.CondensedFontScale : fontPoints);
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
        // Temporarily disabled — the "fake XOR via white text on prior
        // black ^GB" approximation makes more fields look wrong than
        // right when the host label uses ^FR pervasively (the SEUR 1485
        // had 12 ^FR fields, most of which had no underlying ^GB). Kept
        // here so we can re-enable once we have a real strategy (e.g.
        // rasterising the field group and XORing the mask).
        // using var reverseScope = this.Reverse ? pdf.PushState() : null;
        // if (this.Reverse) pdf.SetTextColor(Color.White);

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

    private double ZebraTracking(FPdf pdf) => ZebraTracking(pdf, this.Thickness);

    /// <summary>
    /// Extra inter-character advance (in PDF user units) so a variable-width
    /// host font sits on Zebra's font A pitch grid. Font A at height 9 dots
    /// has a 6-dot pitch (5-dot glyph + 1-dot gap); scale that by the actual
    /// rendered glyph height to get the target pitch. The naturalAdvance of
    /// "M" stands in for the average rendered glyph width.
    ///
    /// <paramref name="heightDots"/> is the actual rendered glyph height in
    /// dots; for the rotated caption path that's the per-call captionAscent
    /// (not the field-level Thickness, which over-estimates because the
    /// caption font is intentionally shrunk for short ^B?R bars).
    /// </summary>
    private double ZebraTracking(FPdf pdf, double heightDots)
    {
        if (this.Font == "0") return 0;
        var targetPitch = heightDots * 6.0 / 9.0;
        var naturalAdvance = pdf.GetStringWidth("M");
        return Math.Max(0, targetPitch - naturalAdvance);
    }


    private void DrawBarcodeCode128(FPdf pdf, string arg2)
    {
        // Choice of codeset:
        //  - ZPL lets the field data pick a subset via a ">x" prefix:
        //      >; = Subset C (digits in pairs)
        //      >: = Subset A
        //      >> = Subset B (the default)
        //  - ^BC's trailing mode param overrides when no prefix wins:
        //      A (Automatic) / "" -> let ZXing pick the most compact
        //                            subset (codeset C for digit pairs)
        //      N / U / D -> keep the safe-default subset B (legacy
        //                   behaviour; UCC/EAN special handling not
        //                   implemented).
        // The 1485 SEUR label sets mode=A on a 23-digit field; forcing
        // subset B there nearly DOUBLES the bar count vs Labelary, which
        // honours the automatic codeset-C compression.
        EncodeHintType? hint = EncodeHintType.CODE128_FORCE_CODESET_B;
        if (arg2.StartsWith(">;"))
        {
            hint = EncodeHintType.CODE128_COMPACT;
            arg2 = arg2.Substring(2);
        }
        else if (arg2.StartsWith(">:") || arg2.StartsWith(">>"))
        {
            arg2 = arg2.Substring(2);
        }
        else
        {
            var mode = this.Barcode128Options.Mode;
            if (string.IsNullOrEmpty(mode) || mode == "A")
                hint = null; // let ZXing's writer auto-pick the codeset
        }

        var barcode = new Code128Writer();
        var hints = hint.HasValue
            ? new Dictionary<EncodeHintType, object>() { { hint.Value, true } }
            : new Dictionary<EncodeHintType, object>();
        var code128 = barcode.encode(arg2, hints);
        var x = pdf.X;
        // ^BC height (Barcode128Options.Height) wins over the ^BY default
        // (BarcodeOptions.Height) when the field specifies one. Without
        // this swap "324f" rendered at the 20-dot ^BY height instead of
        // the 150-dot ^BCN,150 height -- bars looked like tall ticks.
        var savedHeight = this.BarcodeOptions.Height;
        if (this.Barcode128Options.Height > 0)
            this.BarcodeOptions.Height = this.Barcode128Options.Height;
        var w = this.BarcodeOptions.Width;
        var height = this.Barcode128Options.Height > 0 ? this.Barcode128Options.Height : this.BarcodeOptions.Height;
        var orientation = string.IsNullOrEmpty(this.Barcode128Options.Orientation) ? "N" : this.Barcode128Options.Orientation;
        double width;
        try
        {
            if (orientation == "B" || orientation == "R")
            {
                DrawRotatedBars(pdf, code128, pdf.X, pdf.Y, w, height,
                    useTopLeftBboxAnchor: Origin == OriginEnum.LeftTop,
                    orientation: orientation);
                width = w * code128.Length;
            }
            else
            {
                width = AddBarcode(pdf, code128, pdf.X, y: null) - pdf.X;
            }
        }
        finally { this.BarcodeOptions.Height = savedHeight; }

        if (this.Barcode128Options.Line && !this.Barcode128Options.LineAbove)
        {
            // Two styles of caption depending on whether ^BC carries its
            // own height parameter:
            //  - ^BC with height (^BCN,150,...): the author is opting in
            //    to a specific layout (often paired with a leading
            //    ^A?h,w that picks the caption font, as on the GLS
            //    courier ^ADN,20,5^BCN,150,...). Honour both the ^A?
            //    sized Thickness and a left-aligned caption that starts
            //    at the barcode's left edge.
            //  - ^BC without height: legacy behaviour. The author hasn't
            //    chosen a font for the caption so a leftover ^CFA from
            //    higher up the label would render it microscopic. Use
            //    Thickness=50 and centre the text under the bars so the
            //    horizontal-shipping sample's ^BC^FD98765432 reads as
            //    before.
            this.TextMode = FieldMode.Text;
            if (orientation == "B" || orientation == "R")
            {
                this.Thickness = 50;
                var stackLength = w * code128.Length;
                var (barLeft, barTop) = ComputeRotatedBboxTopLeft(pdf, height, stackLength, orientation);
                DrawRotatedHumanReadable(pdf, arg2,
                    barLeft: barLeft, barTop: barTop,
                    barLength: height,
                    barStackLength: stackLength,
                    captionAscent: ComputeRotatedCaptionAscent(height),
                    orientation: orientation);
            }
            else if (this.Barcode128Options.Height > 0)
            {
                pdf.Ln(this.Barcode128Options.Height + 4);
                DrawHumanReadable(pdf, arg2, barcodeLeft: x, barcodeWidth: width, centerText: false);
            }
            else
            {
                pdf.Ln(this.BarcodeOptions.Height + 4);
                this.Thickness = 50;
                DrawHumanReadable(pdf, arg2, barcodeLeft: x, barcodeWidth: width);
            }
        }
    }

    private void DrawHumanReadable(FPdf pdf, string text, double barcodeLeft, double barcodeWidth, bool centerText = true)
    {
        var fontPoints = (Convert.ToDouble(this.Thickness) / Dpi) * 25.4 * 2.54;
        pdf.SetFont(this.MonospaceFont, this.MonospaceStyle ?? "", 0, null);
        pdf.SetFontSize(fontPoints);
        pdf.FontScale.ScaleX = 1;
        pdf.FontScale.ScaleY = 1;
        var tracking = ZebraTracking(pdf);
        double indent = 0;
        if (centerText)
        {
            // GetStringWidth already returns the width in user units (FontSize is
            // stored divided by k). Account for the per-char tracking so the
            // centring stays correct.
            var textWidthUser = pdf.GetStringWidth(text) + tracking * Math.Max(0, text.Length - 1);
            indent = Math.Max(0, (barcodeWidth - textWidthUser) / 2);
        }
        pdf.SetX(barcodeLeft + indent);
        pdf.WriteRotatedText(pdf.X, pdf.Y, this.Thickness * 0.7, "N", text, tracking);
    }

    /// <summary>
    /// Print interpretation line for a rotated barcode (^B?B bottom-up
    /// or ^B?R top-down). The caption sits one gap-dot away from the bar
    /// bbox and reads in the same direction as the bars. ZPL printAbove=N
    /// places the caption "below" the bars in the rotated reading frame,
    /// which maps to:
    ///   - B (90° CCW): "below" in rotated frame = RIGHT in absolute.
    ///   - R (90° CW):  "below" in rotated frame = LEFT in absolute.
    /// (barLeft, barTop) is the bar bbox's top-left in absolute coords;
    /// the caller computes it via <see cref="ComputeRotatedBboxTopLeft"/>.
    /// <paramref name="captionAscent"/> is the desired font ascent in dots;
    /// the caller scales it from the bar length so the digits don't
    /// dwarf small bars (default ^Bxh,YN with h=88 dots produced a ~17pt
    /// caption next to 11mm bars, dominating the strip).
    /// </summary>
    private void DrawRotatedHumanReadable(FPdf pdf, string text,
        double barLeft, double barTop, double barLength, double barStackLength,
        double captionAscent,
        string orientation = "B")
    {
        // Condensed font when the host configured one (Roboto Condensed)
        // keeps the rotated digits packed; otherwise fall back to the
        // monospace slot. The host's caption font choice survives across
        // call sites because we restore nothing -- the caller has already
        // done what it needs the host font for.
        var fontPoints = (captionAscent / Dpi) * 25.4 * 2.54;
        if (!string.IsNullOrEmpty(this.CondensedFont))
            pdf.SetFont(this.CondensedFont, this.CondensedStyle ?? "", 0, null);
        else
            pdf.SetFont(this.MonospaceFont, this.MonospaceStyle ?? "", 0, null);
        pdf.SetFontSize(fontPoints);
        pdf.FontScale.ScaleX = 1;
        pdf.FontScale.ScaleY = 1;
        // Tracking has to follow the actual caption font height, not the
        // field-level Thickness (which the callers leave at 50 for the
        // legacy N-orientation path). Otherwise a small ^B?R caption gets
        // pitch padding sized for a much larger font and the digits
        // visibly drift apart.
        var tracking = ZebraTracking(pdf, captionAscent);
        var textWidthUser = pdf.GetStringWidth(text) + tracking * Math.Max(0, text.Length - 1);
        const double gap = 4.0;
        // captionStripWidth = horizontal absolute extent of the rotated
        // glyph strip. The strip is "captionAscent" tall in the unrotated
        // frame, which becomes a horizontal extent after the 90° rotation.
        var captionStripWidth = captionAscent * 0.7;
        var textFoY = barTop + Math.Max(0, (barStackLength - textWidthUser) / 2.0);
        double textFoX;
        if (orientation == "R")
        {
            // Caption to the LEFT of bars. WriteRotatedTextZpl "R" with
            // useTopLeftBboxAnchor=false treats foX as the baseline column
            // (= print LEFT edge of bbox), so to put the visible glyphs at
            // [barLeft-gap-strip, barLeft-gap] we anchor at
            // barLeft - gap - strip. Use the unpadded path because the
            // caption's textFoX is already the column we want.
            textFoX = barLeft - gap - captionStripWidth;
        }
        else // "B"
        {
            textFoX = barLeft + barLength + gap;
        }
        pdf.WriteRotatedTextZpl(textFoX, textFoY, captionStripWidth, orientation, text, tracking,
            useTopLeftBboxAnchor: orientation == "B");
    }

    /// <summary>
    /// For a rotated barcode (B or R), compute the top-left corner of
    /// the bar bounding box in absolute coords from the raw anchor at
    /// (pdf.X, pdf.Y). The mapping is:
    ///                   ^FT (LeftBottom)              ^FO (LeftTop)
    ///   B (90° CCW):    bottom-right of rotated       top-left
    ///   R (90° CW):     top-left of rotated           top-left
    /// For R, ^FO and ^FT both land at the print-frame top-left of the
    /// bbox: ^FO because the ZPL spec defines the field origin as the
    /// upper-left of the field in print coordinates; ^FT because the
    /// lower-left of the rotated character box collapses to the top of
    /// the stack for a barcode (the stack grows downward in print).
    /// </summary>
    private (double left, double top) ComputeRotatedBboxTopLeft(FPdf pdf, double barLength, double stackLength, string orientation)
    {
        var useTopLeftAnchor = Origin == OriginEnum.LeftTop;
        if (orientation == "R")
        {
            return (pdf.X, pdf.Y);
        }
        else // "B"
        {
            var left = useTopLeftAnchor ? pdf.X : pdf.X - barLength;
            var top  = useTopLeftAnchor ? pdf.Y : pdf.Y - stackLength;
            return (left, top);
        }
    }

    /// <summary>
    /// Caption ascent (in dots) for a rotated barcode of length
    /// <paramref name="barLength"/>. Scales the strip to ~33% of the bar
    /// length so a short ^B3R,N,88,Y caption sits at ~29 dots (≈9pt, close
    /// to Labelary's reference render) without dwarfing the bars, and
    /// tall bars on the GLS courier ^B2B,200 still get clamped to the
    /// 45-dot cap rather than ballooning past the bars.
    /// </summary>
    private static double ComputeRotatedCaptionAscent(double barLength)
        => Math.Min(45.0, Math.Max(12.0, barLength * 0.33));
    /// <summary>
    /// Map every simple-1D BarcodeMode to the ZXing Writer + format
    /// pair the generic <see cref="DrawBarcode1D"/> pipeline drives.
    /// Adding a new symbology that fits the "single Writer, single
    /// format, no codeset hint" pattern is a one-line entry here.
    /// Code 128, Code 39 and Interleaved 2 of 5 have their own renderers
    /// (codeset prefix / wide:narrow ratio / start-stop captioning) so
    /// they stay out of the table.
    /// </summary>
    private static readonly Dictionary<BarcodeMode, (Func<ZXing.Writer> writer, ZXing.BarcodeFormat format)> Simple1DWriters = new()
    {
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
            case BarcodeMode.Code39:          DrawBarcodeCode39(pdf, text); break;
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

        if (orientation == "B" || orientation == "R")
        {
            DrawRotatedBars(pdf, bitmap, pdf.X, pdf.Y, w, height,
                useTopLeftBboxAnchor: Origin == OriginEnum.LeftTop,
                orientation: orientation);
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
        else if (opts.Line && (orientation == "B" || orientation == "R"))
        {
            this.TextMode = FieldMode.Text;
            this.Thickness = 50;
            var stackLength = w * bitmap.Length;
            var (barLeft, barTop) = ComputeRotatedBboxTopLeft(pdf, height, stackLength, orientation);
            DrawRotatedHumanReadable(pdf, data,
                barLeft: barLeft, barTop: barTop,
                barLength: height,
                barStackLength: stackLength,
                captionAscent: ComputeRotatedCaptionAscent(height),
                orientation: orientation);
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
        // ^B2 with the checkDigit=Y trailing flag appends a Mod-10
        // weighted-sum check digit *before* the even-length pad. The
        // previous code skipped the check-digit step so 17-char
        // payloads like the GLS courier 61771295653200001 ended up
        // encoded as "0" + payload (18 chars) instead of payload +
        // check digit (18 chars). Same length, different bars,
        // different scanned value.
        if (opts.CheckDigit)
            data += ComputeI2of5CheckDigit(data);
        // ITF requires an even number of digits; pad with a leading
        // zero when the (post-check-digit) length is still odd.
        if (data.Length % 2 == 1)
            data = "0" + data;

        // Custom encoder rather than ZXing.Net's ITFWriter so we can
        // honour ^BY's wide:narrow ratio. ZXing's writer hard-codes
        // wide = 3 modules (its END_PATTERN = {3,1,1}); ^BY4,2 asks
        // for wide = 2, which gives noticeably thinner bars and the
        // bar-by-bar pattern Labelary produces.
        var wideUnits = (int)Math.Round(this.BarcodeOptions.WidthRatio, MidpointRounding.AwayFromZero);
        if (wideUnits < 2) wideUnits = 2;
        if (wideUnits > 3) wideUnits = 3;
        var bitmap = EncodeI2of5(data, wideUnits);

        var w = this.BarcodeOptions.Width;
        var orientation = string.IsNullOrEmpty(opts.Orientation) ? "N" : opts.Orientation;
        var height = opts.Height > 0 ? opts.Height : this.BarcodeOptions.Height;

        if (orientation == "B" || orientation == "R")
        {
            DrawRotatedBars(pdf, bitmap, pdf.X, pdf.Y, w, height,
                useTopLeftBboxAnchor: Origin == OriginEnum.LeftTop,
                orientation: orientation);
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
        else if (opts.Line && (orientation == "B" || orientation == "R"))
        {
            // Vertical caption to the side of the rotated bars (R left,
            // B right, both centred along the stack).
            this.TextMode = FieldMode.Text;
            this.Thickness = 50;
            var stackLength = w * bitmap.Length;
            var (barLeft, barTop) = ComputeRotatedBboxTopLeft(pdf, height, stackLength, orientation);
            DrawRotatedHumanReadable(pdf, data,
                barLeft: barLeft, barTop: barTop,
                barLength: height,
                barStackLength: stackLength,
                captionAscent: ComputeRotatedCaptionAscent(height),
                orientation: orientation);
        }
    }

    /// <summary>
    /// Interleaved 2-of-5 Mod-10 weighted-sum check digit. Walks the
    /// payload right-to-left, multiplying the rightmost digit by 3 and
    /// alternating 1 / 3 from there; the check digit is whatever pushes
    /// the running sum to the next multiple of 10. Matches the algorithm
    /// Zebra applies when ^B2's trailing checkDigit=Y flag is set, and
    /// the value Labelary stamps under the GLS courier I2of5.
    /// </summary>
    private static char ComputeI2of5CheckDigit(string digits)
    {
        int sum = 0;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            if (digits[ i ] < '0' || digits[ i ] > '9') continue;
            int d = digits[ i ] - '0';
            int factor = ((digits.Length - 1 - i) % 2 == 0) ? 3 : 1;
            sum += d * factor;
        }
        int check = (10 - (sum % 10)) % 10;
        return (char)('0' + check);
    }

    /// <summary>
    /// Render a Code 39 barcode through our own encoder rather than
    /// ZXing.Net's Code39Writer. The custom path lets us
    ///  - honour ^BY's wide:narrow ratio (ZXing locks it at 2:1),
    ///  - skip the 10-module quiet zone ZXing always emits (so the bar
    ///    stack matches Labelary's length when ratio matches),
    ///  - render the start/stop '*' chars in the human-readable line,
    ///    which Zebra prints and ZXing's encoded data does not surface.
    /// Mirrors the dispatch shape of <see cref="DrawBarcodeI2of5"/>.
    /// </summary>
    private void DrawBarcodeCode39(FPdf pdf, string data)
    {
        var opts = this.Barcode1DOptions;
        // Code 39 is case-insensitive in the alphabet but the encoding
        // table is upper-case only.
        var payload = (data ?? "").ToUpperInvariant();

        // ^BY's r param: 2.0..3.0 → wide bars in narrow-module units.
        var wideUnits = (int)Math.Round(this.BarcodeOptions.WidthRatio, MidpointRounding.AwayFromZero);
        if (wideUnits < 2) wideUnits = 2;
        if (wideUnits > 3) wideUnits = 3;
        var bitmap = EncodeCode39(payload, wideUnits);
        if (bitmap == null) return; // unsupported char — drop the field

        var w = this.BarcodeOptions.Width;
        var height = opts.Height > 0 ? opts.Height : this.BarcodeOptions.Height;
        var orientation = string.IsNullOrEmpty(opts.Orientation) ? "N" : opts.Orientation;

        if (orientation == "B" || orientation == "R")
        {
            DrawRotatedBars(pdf, bitmap, pdf.X, pdf.Y, w, height,
                useTopLeftBboxAnchor: Origin == OriginEnum.LeftTop,
                orientation: orientation);
        }
        else
        {
            var saved = this.BarcodeOptions.Height;
            this.BarcodeOptions.Height = height;
            try { AddBarcode(pdf, bitmap, pdf.X, y: null); }
            finally { this.BarcodeOptions.Height = saved; }
        }

        // Caption — Code 39 prints the '*' start/stop chars alongside
        // the data (^FD07504004485275 → "*07504004485275*").
        if (opts.Line)
        {
            var captionText = "*" + payload + "*";
            if (orientation == "N")
            {
                pdf.Ln(height + 4);
                this.TextMode = FieldMode.Text;
                this.Thickness = 50;
                DrawHumanReadable(pdf, captionText, barcodeLeft: pdf.X, barcodeWidth: w * bitmap.Length);
            }
            else if (orientation == "B" || orientation == "R")
            {
                this.TextMode = FieldMode.Text;
                this.Thickness = 50;
                var stackLength = w * bitmap.Length;
                var (barLeft, barTop) = ComputeRotatedBboxTopLeft(pdf, height, stackLength, orientation);
                DrawRotatedHumanReadable(pdf, captionText,
                    barLeft: barLeft, barTop: barTop,
                    barLength: height,
                    barStackLength: stackLength,
                    captionAscent: ComputeRotatedCaptionAscent(height),
                    orientation: orientation);
            }
        }
    }

    // 44-char Code 39 alphabet, ending with the '*' start/stop sentinel.
    private const string Code39Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%*";

    // 9-element bar/space patterns (5 bars + 4 spaces, alternating
    // B S B S B S B S B). Each entry is a 9-char string where 'n' marks
    // a narrow element and 'w' marks a wide one. Exactly three elements
    // per char are wide (Code 39 spec). Indexed parallel to
    // <see cref="Code39Alphabet"/>.
    private static readonly string[] Code39Patterns =
    {
        "nnnwwnwnn", // 0
        "wnnwnnnnw", // 1
        "nnwwnnnnw", // 2
        "wnwwnnnnn", // 3
        "nnnwwnnnw", // 4
        "wnnwwnnnn", // 5
        "nnwwwnnnn", // 6
        "nnnwnnwnw", // 7
        "wnnwnnwnn", // 8
        "nnwwnnwnn", // 9
        "wnnnnwnnw", // A
        "nnwnnwnnw", // B
        "wnwnnwnnn", // C
        "nnnnwwnnw", // D
        "wnnnwwnnn", // E
        "nnwnwwnnn", // F
        "nnnnnwwnw", // G
        "wnnnnwwnn", // H
        "nnwnnwwnn", // I
        "nnnnwwwnn", // J
        "wnnnnnnww", // K
        "nnwnnnnww", // L
        "wnwnnnnwn", // M
        "nnnnwnnww", // N
        "wnnnwnnwn", // O
        "nnwnwnnwn", // P
        "nnnnnnwww", // Q
        "wnnnnnwwn", // R
        "nnwnnnwwn", // S
        "nnnnwnwwn", // T
        "wwnnnnnnw", // U
        "nwwnnnnnw", // V
        "wwwnnnnnn", // W
        "nwnnwnnnw", // X
        "wwnnwnnnn", // Y
        "nwwnwnnnn", // Z
        "nwnnnnwnw", // -
        "wwnnnnwnn", // .
        "nwwnnnwnn", // (space)
        "nwnwnwnnn", // $
        "nwnwnnnwn", // /
        "nwnnnwnwn", // +
        "nnnwnwnwn", // %
        "nwnnwnwnn", // *
    };

    /// <summary>
    /// Encode a Code 39 payload to a bare bitmap (no quiet zones), with
    /// '*' start/stop chars prepended/appended automatically. Each
    /// character contributes 9 bar/space elements + 1 narrow inter-char
    /// gap; wide elements are rendered as <paramref name="wideUnits"/>
    /// narrow modules so ^BY's r param drives the visual ratio.
    /// Returns null if any payload char isn't in the Code 39 alphabet.
    /// </summary>
    private static bool[] EncodeCode39(string data, int wideUnits)
    {
        var bits = new System.Collections.Generic.List<bool>();
        var toEncode = "*" + data + "*";
        for (int ci = 0; ci < toEncode.Length; ci++)
        {
            var c = toEncode[ ci ];
            var idx = Code39Alphabet.IndexOf(c);
            if (idx < 0) return null;
            var pattern = Code39Patterns[ idx ];
            for (int el = 0; el < 9; el++)
            {
                bool isBar = (el % 2) == 0; // elements 0,2,4,6,8 are bars
                int count = pattern[ el ] == 'w' ? wideUnits : 1;
                for (int k = 0; k < count; k++) bits.Add(isBar);
            }
            // 1 narrow inter-char space after every char EXCEPT the last
            if (ci < toEncode.Length - 1) bits.Add(false);
        }
        return bits.ToArray();
    }

    /// <summary>
    /// Interleaved 2-of-5 encoder that respects ^BY's wide:narrow
    /// ratio (ZXing.Net's ITFWriter hard-codes wide = 3). Each digit
    /// has two wide elements and three narrow ones; in I2of5 the bars
    /// of the first digit of a pair are interleaved with the spaces
    /// of the second digit. Patterns are the standard ITF set: 0 =
    /// NNWWN, 1 = WNNNW, ..., 9 = NWNWN.
    /// </summary>
    private static bool[] EncodeI2of5(string data, int wideUnits)
    {
        // Wide-position indices (0..4) for each digit's 5-element pattern.
        int[][] wides =
        {
            new[]{2, 3}, // 0  NNWWN
            new[]{0, 4}, // 1  WNNNW
            new[]{1, 4}, // 2  NWNNW
            new[]{0, 1}, // 3  WWNNN
            new[]{2, 4}, // 4  NNWNW
            new[]{0, 2}, // 5  WNWNN
            new[]{1, 2}, // 6  NWWNN
            new[]{3, 4}, // 7  NNNWW
            new[]{0, 3}, // 8  WNNWN
            new[]{1, 3}, // 9  NWNWN
        };

        var bits = new System.Collections.Generic.List<bool>();
        bool barNext = true;
        void Emit(int count)
        {
            for (int i = 0; i < count; i++) bits.Add(barNext);
            barNext = !barNext;
        }

        // Start guard: 4 narrow elements (bar, space, bar, space).
        Emit(1); Emit(1); Emit(1); Emit(1);

        // Each digit pair: bars from d1's pattern interleaved with spaces
        // from d2's pattern.
        for (int i = 0; i + 1 < data.Length; i += 2)
        {
            var w1 = wides[ data[ i ] - '0' ];
            var w2 = wides[ data[ i + 1 ] - '0' ];
            for (int j = 0; j < 5; j++)
            {
                Emit((w1[ 0 ] == j || w1[ 1 ] == j) ? wideUnits : 1); // bar
                Emit((w2[ 0 ] == j || w2[ 1 ] == j) ? wideUnits : 1); // space
            }
        }

        // Stop guard: wide bar, narrow space, narrow bar.
        Emit(wideUnits); Emit(1); Emit(1);

        return bits.ToArray();
    }

    /// <summary>
    /// Draw a 1-D barcode bitmap as a stack of horizontal bars, with the
    /// bottom-right corner of the resulting barcode at (anchorX, anchorY) —
    /// the ZPL convention for ^B?B (orientation B, 270° CW). Each "true"
    /// run in the bitmap becomes one bar.
    /// </summary>
    private void DrawRotatedBars(FPdf pdf, bool[] bitmap, double anchorX, double anchorY, int moduleWidth, int barLength,
        bool useTopLeftBboxAnchor = false,
        string orientation = "B")
    {
        // Both B (read bottom-up) and R (read top-down) produce vertical
        // barcodes whose bars are horizontal stripes. The difference is:
        //  - B: bitmap module 0 sits at the visual BOTTOM.
        //  - R: bitmap module 0 sits at the visual TOP.
        // Anchor corner (per ZPL):
        //                   ^FT (LeftBottom)              ^FO (LeftTop)
        //   B (90° CCW):    bottom-right of rotated       top-left
        //   R (90° CW):     top-left of rotated           top-left
        // For R, both ^FT and ^FO collapse to the print-frame top-left of
        // the bbox (the field is anchored at the top of the stack), so the
        // anchor-mode flag doesn't change the math.
        var stackLength = bitmap.Length * moduleWidth;
        double leftX, topY;
        if (orientation == "R")
        {
            leftX = anchorX;
            topY  = anchorY;
        }
        else // "B"
        {
            leftX = useTopLeftBboxAnchor ? anchorX : anchorX - barLength;
            topY  = useTopLeftBboxAnchor ? anchorY : anchorY - stackLength;
        }

        var index = 0;
        while (index < bitmap.Length)
        {
            if (bitmap[ index ])
            {
                var trueCount = bitmap.Skip(index + 1).TakeWhile(b => b).Count();
                var thickness = moduleWidth * (trueCount + 1);

                // Place the bar covering modules [index, index+trueCount]
                // along the bbox's vertical axis. For B the module 0 is
                // at the bottom, so the bar's top is offset from the bbox
                // bottom; for R module 0 is at the top so the offset
                // walks down from the bbox top.
                double yTop = orientation == "R"
                    ? topY + index * moduleWidth
                    : topY + (bitmap.Length - 1 - index - trueCount) * moduleWidth;

                DrawAbsoluteRect(pdf, leftX, yTop, barLength, thickness, Color.Black);
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