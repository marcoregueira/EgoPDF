using Ego.PDF;
namespace Ego.PDF.Barcodes.Zpl;

public class PdfZpl
{
    public FPdf Pdf { get; }
    public int ThicknessDefault { get; private set; } = 50;
    private Dictionary<string, Action<FPdf, string>> Tokens { get; set; } = new();
    private FieldDefinition CurrentField = new();

    public double TopX { get; private set; } = 0;
    public double TopY { get; private set; } = 0;
    public string Alignment { get; private set; }
    public int Dpi { get; private set; }
    public string DefaultOrientation { get; private set; } = "N";
    public string DefaultFont { get; private set; } = "A";
    public int DefaultFontH { get; private set; } = 9;
    public string MonospaceFont { get; private set; }
    public string MonospaceStyle { get; private set; } = "";
    public string VariableFont { get; private set; }
    public string VariableStyle { get; private set; } = "";
    public string CondensedFont { get; private set; }
    public string CondensedStyle { get; private set; } = "";
    /// <summary>
    /// Global em-size multiplier applied whenever a field renders with
    /// the CondensedFont slot (Font 0 and the P-V proportional slots).
    /// 1.10 nudges Roboto Condensed up ~10% so it lands closer to
    /// Labelary's reference width on labels where the host font sits
    /// visibly thinner than the reference. Default 1.0 keeps existing
    /// calibrations (GLS courier, web samples) unchanged.
    /// </summary>
    public double CondensedFontScale { get; private set; } = 1.0;
    public Dictionary<string, string> Values { get; private set; }
    private readonly Dictionary<string, string> _graphics = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (List<byte[]> rows, int bytesPerRow)> _storedGraphics = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Map a ZPL graphic name (the one used in ^XG) to a local image file.
    /// In real Zebra printers the graphic is stored ahead of time via
    /// ~DG / ^IS; here the sample provides it directly.
    /// </summary>
    public void RegisterGraphic(string name, string filePath)
    {
        _graphics[ name ] = filePath;
    }

    /// <summary>
    /// Regex extracting one <c>~DG&lt;name&gt;,&lt;total_bytes&gt;,&lt;bytes_per_row&gt;,&lt;data&gt;</c>
    /// segment. Data runs until the next ZPL command (introduced by <c>^</c>
    /// or another <c>~</c>) or the end of the string.
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex DgPattern =
        new(@"~DG\s*([^,]+?)\s*,\s*(\d+)\s*,\s*(\d+)\s*,([^~^]*)",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    // Page geometry (in dots). Defaults match Labelary's default label.
    public int LabelWidthDots { get; private set; } = 812;
    public int LabelLengthDots { get; private set; } = 1218;
    // If true, ^PW and ^LL clamp to the values above instead of replacing
    // them — this mirrors Labelary's behaviour when you pre-select a label
    // size in its UI (excess ^PW/^LL get clipped to the physical roll).
    private bool _labelSizeLocked = false;
    private bool _pageStarted = false;

    /// <summary>
    /// Lock the label dimensions (in dots). Subsequent ^PW / ^LL commands
    /// from the ZPL will be clamped to these maxima instead of replacing
    /// them.
    /// </summary>
    public void SetLabelSize(int widthDots, int lengthDots)
    {
        if (widthDots > 0) this.LabelWidthDots = widthDots;
        if (lengthDots > 0) this.LabelLengthDots = lengthDots;
        this._labelSizeLocked = true;
    }

    public PdfZpl(FPdf document, int dpi = 72)
    {
        this.Pdf = document;
        this.Dpi = dpi;

        // Label / printer-state commands.
        Tokens[ "^XA" ] = (pdf, _) => { pdf.X = 0; pdf.Y = 0; };
        Tokens[ "^XZ" ] = Voidf;        // end-of-label — page is opened lazily; nothing else to flush
        Tokens[ "^LH" ] = SetLabelHome;
        Tokens[ "^LL" ] = SetLabelLength;
        Tokens[ "^PW" ] = SetPrintWidth;
        Tokens[ "^LR" ] = Voidf;        // label reverse — not modelled
        Tokens[ "^LS" ] = Voidf;        // label shift — not modelled
        Tokens[ "^JM" ] = Voidf;        // mm-per-dot — printer hardware setting
        Tokens[ "^PM" ] = Voidf;        // mirror image — not modelled
        Tokens[ "^PO" ] = Voidf;        // print orientation — not modelled
        Tokens[ "^PF" ] = Voidf;        // slew dot rows — printer hardware
        Tokens[ "^PR" ] = Voidf;        // print rate — printer hardware
        Tokens[ "^MD" ] = Voidf;        // media darkness — printer hardware

        // Field positioning + content.
        Tokens[ "^FO" ] = SetLocation;
        Tokens[ "^FT" ] = SetLocationBottom;
        Tokens[ "^FD" ] = WriteText;          // text content of the current field
        Tokens[ "^FN" ] = AddFieldValue;      // ^FN<n>: substitute from a ^DF/^XF template
        Tokens[ "^FH" ] = EscapeCharacter;
        Tokens[ "^FW" ] = SetOrientation;
        Tokens[ "^FB" ] = FrameBox;
        Tokens[ "^FR" ] = FieldReverse;
        Tokens[ "^FS" ] = FinishField;
        Tokens[ "^FX" ] = Voidf;              // comment — discarded
        Tokens[ "^CF" ] = SetDefaultFont;
        Tokens[ "^CI" ] = CharacterSet;

        // ^A?  font selectors (A-V, plus the proportional ^A0).
        Tokens[ "^AA" ] = SetFont;
        Tokens[ "^AB" ] = SetFont;
        Tokens[ "^AC" ] = SetFont;
        Tokens[ "^AD" ] = SetFont;
        Tokens[ "^AE" ] = SetFont;
        Tokens[ "^AF" ] = SetFont;
        Tokens[ "^AG" ] = SetFont;
        Tokens[ "^AH" ] = SetFont;
        Tokens[ "^AO" ] = SetFont;
        Tokens[ "^AP" ] = SetFont;
        Tokens[ "^AQ" ] = SetFont;
        Tokens[ "^AR" ] = SetFont;
        Tokens[ "^AS" ] = SetFont;
        Tokens[ "^AT" ] = SetFont;
        Tokens[ "^AU" ] = SetFont;
        Tokens[ "^AV" ] = SetFont;
        Tokens[ "^A0" ] = SetFont;

        // Graphics primitives.
        Tokens[ "^GB" ] = RectangleBox;
        Tokens[ "^GC" ] = Circle;
        Tokens[ "^GE" ] = Ellipse;
        Tokens[ "^GD" ] = Diagonal;

        // Images.
        Tokens[ "^GF" ] = GraphicField;       // inline ASCII bitmap at the current ^FO
        Tokens[ "^XG" ] = RecallGraphic;      // recall a previously-registered graphic
        Tokens[ "^DG" ] = Voidf;              // download graphics — printer-side storage
        Tokens[ "^DY" ] = Voidf;              // download objects — printer-side storage
        Tokens[ "^DU" ] = Voidf;              // upload font — printer-side storage
        Tokens[ "^IS" ] = Voidf;              // save format — printer-side storage
        Tokens[ "^IL" ] = Voidf;              // recall format — printer-side storage

        // Template / referenced format. The actual splitting happens up in
        // ZplParser; here we only need to ignore the commands inside a
        // label that references a template.
        Tokens[ "^DF" ] = Voidf;
        Tokens[ "^XF" ] = Voidf;

        // Barcodes -- 1D family driven by the OneDBarcodes table so each
        // ZPL command is one row of data instead of a one-off method.
        Tokens[ "^BY" ] = SetDefaultBarcodeOptions;
        foreach (var spec in OneDBarcodes)
        {
            var captured = spec;
            Tokens[ captured.Token ] = (pdf, body) => Handle1DBarcode(body, captured);
        }
        Tokens[ "^B7" ] = Pdf417;
        Tokens[ "^BQ" ] = QrCode;
        Tokens[ "^BX" ] = DataMatrix;
        Tokens[ "^BO" ] = Aztec;

        ResetField(Pdf);
    }

    /// <summary>
    /// Where each ZPL 1D command parks its parsed parameters and how its
    /// parameter tail is indexed. <c>OptionsTarget</c> returns the
    /// <see cref="FieldDefinition"/> property the ^B? handler writes to;
    /// keeping three separate instances avoids cross-talk inside a label
    /// that mixes symbologies. The <c>...Idx</c> fields are the position
    /// each parameter occupies in the command's comma-separated tail.
    /// </summary>
    private readonly record struct OneDSpec(
        string Token,
        BarcodeMode Mode,
        Func<FieldDefinition, Barcode1DOptions> OptionsTarget,
        int HeightIdx, int LineIdx, int LineAboveIdx, int CheckDigitIdx);

    private static readonly OneDSpec[] OneDBarcodes =
    {
        // ^BC  orientation, height, line,        lineAbove, checkDigit, mode
        new("^BC", BarcodeMode.Code128,         f => f.Barcode128Options,  1, 2, 3, -1),
        // ^B2  orientation, height, line,        lineAbove, checkDigit
        new("^B2", BarcodeMode.Interleaved2of5, f => f.Barcode2of5Options, 1, 2, 3,  4),
        // ^B3  orientation, checkDigit, height,  line,      lineAbove
        new("^B3", BarcodeMode.Code39,          f => f.Barcode1DOptions,   2, 3, 4,  1),
        // ^BK  orientation, checkDigit, height,  line,      lineAbove, startChar, stopChar
        new("^BK", BarcodeMode.Codabar,         f => f.Barcode1DOptions,   2, 3, 4, -1),
        // ^BE  orientation, height, line,        lineAbove
        new("^BE", BarcodeMode.EAN13,           f => f.Barcode1DOptions,   1, 2, 3, -1),
        // ^B8  orientation, height, line,        lineAbove
        new("^B8", BarcodeMode.EAN8,            f => f.Barcode1DOptions,   1, 2, 3, -1),
        // ^BU  orientation, height, line,        lineAbove, checkDigit
        new("^BU", BarcodeMode.UPC_A,           f => f.Barcode1DOptions,   1, 2, 3,  4),
        // ^B9  orientation, height, line,        lineAbove, checkDigit
        new("^B9", BarcodeMode.UPC_E,           f => f.Barcode1DOptions,   1, 2, 3,  4),
        // ^BM  orientation, type (A-D), height,  line,      lineAbove, checkDigit2
        new("^BM", BarcodeMode.MSI,             f => f.Barcode1DOptions,   2, 3, 4, -1),
    };

    private void Handle1DBarcode(string commandLine, OneDSpec spec)
    {
        Apply1DOptions(spec.OptionsTarget(this.CurrentField), commandLine.Substring(3),
            spec.HeightIdx, spec.LineIdx, spec.LineAboveIdx, spec.CheckDigitIdx);
        // ^BC carries a trailing "mode" param (N/U/A/D) that picks the
        // Code 128 encoding strategy. Only ^BC reads it — the other 1D
        // commands don't have a mode param.
        if (spec.Mode == BarcodeMode.Code128)
        {
            var parts = commandLine.Substring(3).Split(',');
            if (parts.Length > 5 && !string.IsNullOrEmpty(parts[ 5 ]))
                this.CurrentField.Barcode128Options.Mode = parts[ 5 ].ToUpperInvariant();
        }
        this.CurrentField.TextMode = FieldMode.Barcode;
        this.CurrentField.BarcodeMode = spec.Mode;
    }

    private void FrameBox(FPdf pdf, string arg2)
    {
        // ^FB maxWidth, maxLines, lineSpacing, alignment, hangingIndent
        var parts = arg2.Substring(3).Split(',');
        var maxWidth = parts.ToInt(0, 0);
        var maxLines = parts.ToInt(1, 1);
        var lineSpacing = parts.ToInt(2, 1);
        var alignment = parts.ToString(3, "L");
        var hangingIntent = parts.ToInt(4, 0);
        this.CurrentField.FrameBox = new FrameBox(maxWidth, maxLines, lineSpacing, alignment, hangingIntent);
    }

    private void AddFieldValue(FPdf pdf, string arg2)
    {
        var fieldName = arg2.Substring(3);
        var value = this.Values.TryGetValue(fieldName, out var fieldValue) ? fieldValue : string.Empty;
        this.CurrentField.Value = value;
    }

    private void SetDefaultBarcodeOptions(FPdf pdf, string arg2)
    {
        arg2 = arg2.Substring(3);
        var parts = arg2.Split(',');

        // ^BY accepts decimal-typed values ("3.0", "2.0") even on the int
        // params. Parse as decimal first and round to int so "^BY3.0,2.0"
        // sets Width=3 (not the default 2 due to a strict int.TryParse
        // rejecting "3.0"). Use InvariantCulture so a comma-decimal
        // OS locale (es-ES) doesn't reject "2.0" as a ratio.
        if (parts.Length > 0 && !string.IsNullOrEmpty(parts[ 0 ]))
        {
            if (decimal.TryParse(parts[ 0 ], System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out var w))
            {
                this.CurrentField.BarcodeOptions.Width = (int)Math.Round(w, MidpointRounding.AwayFromZero);
            }
        }

        if (parts.Length > 1 && !string.IsNullOrEmpty(parts[ 1 ]))
        {
            if (decimal.TryParse(parts[ 1 ], System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out var w))
            {
                w = Math.Round(w, 1);
                if (w >= 2 && w <= 3)
                    this.CurrentField.BarcodeOptions.WidthRatio = w;
            }
        }

        if (parts.Length > 2 && !string.IsNullOrEmpty(parts[ 2 ]))
        {
            if (decimal.TryParse(parts[ 2 ], System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out var h))
            {
                this.CurrentField.BarcodeOptions.Height = (int)Math.Round(h, MidpointRounding.AwayFromZero);
            }
        }
    }

    private void SetLabelLength(FPdf pdf, string arg2)
    {
        // ^LL<dots> — sets the label length in dots. When the label size
        // has been locked from C# we cap instead of replacing, so a
        // ZPL ^LL larger than the physical roll does not extend the page.
        if (!int.TryParse(arg2.Substring(3), out var ll) || ll <= 0) return;
        this.LabelLengthDots = _labelSizeLocked ? Math.Min(this.LabelLengthDots, ll) : ll;
    }

    private void SetPrintWidth(FPdf pdf, string arg2)
    {
        // ^PW<dots> — sets the print width in dots. Same clamp rule as ^LL.
        if (!int.TryParse(arg2.Substring(3), out var pw) || pw <= 0) return;
        this.LabelWidthDots = _labelSizeLocked ? Math.Min(this.LabelWidthDots, pw) : pw;
    }

    /// <summary>
    /// Add the first PDF page if none exists yet. Called lazily before the
    /// first drawing operation so ^PW / ^LL (which appear after ^XA) can
    /// influence the page size.
    /// </summary>
    private void EnsurePage()
    {
        if (_pageStarted || Pdf.Page > 0)
        {
            _pageStarted = true;
            return;
        }
        Pdf.AddPage(
            Ego.PDF.Data.PageOrientation.Portrait,
            new Ego.PDF.Data.Dimensions { Width = LabelWidthDots, Heigth = LabelLengthDots });
        _pageStarted = true;
    }

    private void SetLabelHome(FPdf pdf, string arg2)
    {
        // ^LH x,y — sets the offset added to every ^FO / ^FT in dots.
        arg2 = arg2.Substring(3);
        var parts = arg2.Split(',');
        if (parts.Length != 2)
            return;
        this.TopX = parts.ToMilimeters(0, 0, Dpi);
        this.TopY = parts.ToMilimeters(1, 0, Dpi);
    }

    private void SetOrientation(FPdf pdf, string arg2)
    {
        var orientation = arg2.Substring(3);
        this.CurrentField.Orientation = orientation;
    }

    private void FinishField(FPdf pdf, string arg2)
    {
        this.CurrentField.Draw(pdf);
        ResetField(pdf);
    }

    private void ResetField(FPdf pdf)
    {
        this.CurrentField.EscapeCharacter = ""; // ??
        this.CurrentField.TextMode = FieldMode.Text;
        this.CurrentField.Font = DefaultFont;
        this.CurrentField.Thickness = ThicknessDefault;
        this.CurrentField.Orientation = DefaultOrientation;
        this.CurrentField.ScaleX = 1;
        this.CurrentField.ScaleY = 1;
        this.CurrentField.Value = "";
        this.CurrentField.Dpi = Dpi;
        this.CurrentField.K = pdf.k;
        this.CurrentField.MonospaceFont = MonospaceFont;
        this.CurrentField.MonospaceStyle = MonospaceStyle;
        this.CurrentField.VariableFont = VariableFont;
        this.CurrentField.VariableStyle = VariableStyle;
        this.CurrentField.CondensedFont = CondensedFont;
        this.CurrentField.CondensedStyle = CondensedStyle;
        this.CurrentField.CondensedFontScale = CondensedFontScale;
        this.CurrentField.FrameBox = null;
        this.CurrentField.Reverse = false;
    }

    public void Print(string zpl)
    {
        // ~DG download-graphic commands live OUTSIDE the ^XA…^XZ blocks
        // ZplParser keeps, so harvest them up here before the block
        // splitter discards their payload.
        zpl = PreprocessDownloadGraphics(zpl);
        var parser = new ZplParser();
        parser.Parse(zpl);
        foreach (var label in parser.ReferencingLabels)
        {
            if (label.ReferencedTemplate != null)
            {
                if (parser.Templates.TryGetValue(label.ReferencedTemplate, out var template))
                {
                    var values = parser.ExtractFieldValues(label.Content);
                    PrintWithData(template, values);
                }
            }
            else
            {
                PrintWithData(label.Content, new Dictionary<string, string>());
            }
        }
    }

    public void PrintWithData(string zpl, Dictionary<string, string> fieldValues)
    {
        this.Values = fieldValues;
        var reader = new StringReader(zpl);
        var line = reader.ReadLine();
        while (line != null)
        {
            line = line.Trim();
            var parts = line.Split(new[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                line = reader.ReadLine();
                continue;
            }

            foreach (var part in parts)
            {
                var command = "^" + part;
                // ZPL command names are case-insensitive in the spec, and
                // real printers (and Labelary) accept lowercase variants
                // like ^Ae or ^FdHello. Uppercase the 3-char dispatch key
                // so we don't miss them; the field handlers normalise
                // further down (e.g. SetFont uppercases the font slot).
                var token = command.Substring(0, Math.Min(3, command.Length)).ToUpperInvariant();
                if (Tokens.ContainsKey(token))
                {
                    try
                    {
                        Tokens[ token ](Pdf, command);
                    }
                    catch (Exception ex)
                    {
                        //throw new Exception(command, ex);
                    }
                }
            }

            line = reader.ReadLine();
        }
    }


    // ---------------------------------------------------------------------
    // Generic ZPL barcode handlers. Each one parses the orientation /
    // height / line / lineAbove tail of its command and stores the
    // configuration on the current field so FieldDefinition.Draw can
    // dispatch the right ZXing writer when ^FS arrives.
    // ---------------------------------------------------------------------

    /// <summary>
    /// Parse the orientation + height + line + lineAbove + checkDigit
    /// tail shared by every ZPL 1D barcode command into
    /// <paramref name="opts"/>. Each command places those values at
    /// slightly different positions in the comma-separated tail (e.g.
    /// ^BC puts height at index 1 while ^B3 and ^BK put it at 2 because
    /// the check-digit flag comes first); the per-command indices live
    /// in <see cref="OneDBarcodes"/>. Pass <c>-1</c> for any index to
    /// skip that field.
    /// </summary>
    private static void Apply1DOptions(Barcode1DOptions opts, string body,
        int heightIdx, int lineIdx, int lineAboveIdx, int checkDigitIdx)
    {
        var parts = body.Split(',');
        opts.Orientation = parts.ToString(0, opts.Orientation);
        if (heightIdx >= 0 && parts.Length > heightIdx && int.TryParse(parts[ heightIdx ], out var h) && h > 0)
            opts.Height = h;
        if (lineIdx >= 0 && parts.Length > lineIdx && !string.IsNullOrEmpty(parts[ lineIdx ]))
            opts.Line = !parts[ lineIdx ].Equals("N", StringComparison.OrdinalIgnoreCase);
        if (lineAboveIdx >= 0 && parts.Length > lineAboveIdx && !string.IsNullOrEmpty(parts[ lineAboveIdx ]))
            opts.LineAbove = parts[ lineAboveIdx ].Equals("Y", StringComparison.OrdinalIgnoreCase);
        if (checkDigitIdx >= 0 && parts.Length > checkDigitIdx && !string.IsNullOrEmpty(parts[ checkDigitIdx ]))
            opts.CheckDigit = parts[ checkDigitIdx ].Equals("Y", StringComparison.OrdinalIgnoreCase);
    }

    private void Set2DOptions(string body, int defaultMagnification = 4)
    {
        // Common shape: <orientation>,<magnification>[,...]
        var parts = body.Split(',');
        var opts = this.CurrentField.Barcode2DOptions;
        opts.Orientation = parts.ToString(0, opts.Orientation);
        opts.Magnification = parts.ToInt(1, defaultMagnification);
    }

    private void Pdf417(FPdf pdf, string arg2)
    {
        // ^B7 orientation, securityLevel, columns, rows, truncate. There's
        // no per-module size parameter so don't read parts[1] as the
        // magnification (that's the security level). PDF417 matrices are
        // wide; default to 2 dots per module so the symbol stays compact.
        var parts = arg2.Substring(3).Split(',');
        var opts = this.CurrentField.Barcode2DOptions;
        opts.Orientation = parts.ToString(0, opts.Orientation);
        opts.Magnification = 2;
        this.CurrentField.TextMode = FieldMode.Barcode;
        this.CurrentField.BarcodeMode = BarcodeMode.PDF417;
    }

    private void QrCode(FPdf pdf, string arg2)
    {
        // ^BQa,b,c,d  — a = orientation, b = model, c = magnification,
        // d = error correction. We only care about orientation +
        // magnification right now.
        var body = arg2.Substring(3);
        var parts = body.Split(',');
        var opts = this.CurrentField.Barcode2DOptions;
        opts.Orientation = parts.ToString(0, opts.Orientation);
        // ZPL puts magnification at index 2 (index 1 is "model").
        opts.Magnification = parts.ToInt(2, 5);
        this.CurrentField.TextMode = FieldMode.Barcode;
        this.CurrentField.BarcodeMode = BarcodeMode.QrCode;
    }

    private void DataMatrix(FPdf pdf, string arg2)
    {
        // ^BXa,b,c,d  — a = orientation, b = height (≈ module size),
        // c = quality. Treat "height" as our module magnification.
        var body = arg2.Substring(3);
        var parts = body.Split(',');
        var opts = this.CurrentField.Barcode2DOptions;
        opts.Orientation = parts.ToString(0, opts.Orientation);
        opts.Magnification = parts.ToInt(1, 4);
        this.CurrentField.TextMode = FieldMode.Barcode;
        this.CurrentField.BarcodeMode = BarcodeMode.DataMatrix;
    }

    private void Aztec(FPdf pdf, string arg2)
    {
        // ^BOa,b,c,d  — a = orientation, b = magnification (1-10).
        Set2DOptions(arg2.Substring(3));
        this.CurrentField.TextMode = FieldMode.Barcode;
        this.CurrentField.BarcodeMode = BarcodeMode.Aztec;
    }

    private void GraphicField(FPdf pdf, string arg2)
    {
        // ^GFa,b,c,d,data
        //   a = compression type (A=ASCII hex, B=binary, C=compressed binary)
        //   b = binary byte count
        //   c = graphic field count (= b for non-compressed)
        //   d = bytes per row
        //   data follows after the 4th comma — may include ZPL-RLE compression.
        EnsurePage();
        if (arg2.Length < 4) return;
        var body = arg2.Substring(3); // strip "^GF"
        // arg2 starts with "^GF" + compression letter, then comma, then numbers.
        var compression = body.Length > 0 ? body[ 0 ] : 'A';
        body = body.Length > 0 ? body.Substring(1) : body; // drop the compression letter
        if (body.StartsWith(",")) body = body.Substring(1);

        // Split off the first three comma-separated numeric parameters.
        int commaCount = 0;
        int payloadStart = 0;
        for (int i = 0; i < body.Length; i++)
        {
            if (body[ i ] == ',')
            {
                commaCount++;
                if (commaCount == 3) { payloadStart = i + 1; break; }
            }
        }
        if (payloadStart == 0) return; // malformed

        var header = body.Substring(0, payloadStart - 1).Split(',');
        if (header.Length < 3) return;
        if (!int.TryParse(header[ 0 ], out _)) return;       // total bytes (ignored — derived from data)
        if (!int.TryParse(header[ 1 ], out _)) return;       // graphic field count (ignored)
        if (!int.TryParse(header[ 2 ], out var bytesPerRow) || bytesPerRow <= 0) return;
        var payload = body.Substring(payloadStart);

        // Only the ASCII-hex variant is implemented for now. Compressed binary
        // (C) and binary (B) are uncommon in the labels we deal with.
        if (compression != 'A' && compression != 'a') return;

        var rows = DecodeAsciiGraphicField(payload, bytesPerRow);
        if (rows.Count == 0) return;

        DrawBitmapRows(pdf, rows, bytesPerRow, pdf.X, pdf.Y);
    }

    /// <summary>
    /// Decode ZPL ^GFA-style ASCII data into raw row bytes. Honours the
    /// G..Y / g..z run-length prefixes, the row-fill `,`, the
    /// fill-with-last `*` and the repeat-previous-row `:`.
    /// </summary>
    private static List<byte[]> DecodeAsciiGraphicField(string data, int bytesPerRow)
    {
        var rows = new List<byte[]>();
        var nibbles = new int[ bytesPerRow * 2 ];
        var pos = 0;
        var lastNibble = 0;
        var pendingRun = 0;

        void FlushRow()
        {
            var row = new byte[ bytesPerRow ];
            for (int b = 0; b < bytesPerRow; b++)
                row[ b ] = (byte) ((nibbles[ b * 2 ] << 4) | nibbles[ b * 2 + 1 ]);
            rows.Add(row);
            Array.Clear(nibbles, 0, nibbles.Length);
            pos = 0;
            lastNibble = 0;
            pendingRun = 0;
        }

        foreach (var ch in data)
        {
            if (ch == ' ' || ch == '\r' || ch == '\n' || ch == '\t')
                continue;
            if (ch == ',')
            {
                // pad with zero to end of row
                FlushRow();
                continue;
            }
            if (ch == '*')
            {
                // pad with the last nibble to end of row
                while (pos < nibbles.Length) nibbles[ pos++ ] = lastNibble;
                FlushRow();
                continue;
            }
            if (ch == ';')
            {
                // line break with no fill — just discard pending count.
                pendingRun = 0;
                continue;
            }
            if (ch == ':')
            {
                // repeat the previous row
                if (rows.Count > 0)
                {
                    var prev = rows[ rows.Count - 1 ];
                    rows.Add((byte[]) prev.Clone());
                }
                pendingRun = 0;
                continue;
            }
            if (ch >= 'g' && ch <= 'z')
            {
                pendingRun += (ch - 'f') * 20;
                continue;
            }
            if (ch >= 'G' && ch <= 'Y')
            {
                pendingRun += (ch - 'F');
                continue;
            }

            int nibble;
            if (ch >= '0' && ch <= '9') nibble = ch - '0';
            else if (ch >= 'A' && ch <= 'F') nibble = 10 + (ch - 'A');
            else if (ch >= 'a' && ch <= 'f') nibble = 10 + (ch - 'a');
            else { pendingRun = 0; continue; } // unknown char, ignore

            var count = pendingRun > 0 ? pendingRun : 1;
            pendingRun = 0;
            while (count > 0 && pos < nibbles.Length)
            {
                nibbles[ pos++ ] = nibble;
                count--;
                if (pos == nibbles.Length) FlushRow();
            }
            lastNibble = nibble;
        }

        // Trailing row if any bits remain.
        if (pos > 0) FlushRow();
        return rows;
    }

    private static void DrawBitmapRows(FPdf pdf, List<byte[]> rows, int bytesPerRow, double anchorX, double anchorY)
    {
        var color = Microsoft.Xna.Framework.Color.Black;
        var totalRows = rows.Count;
        for (int row = 0; row < totalRows; row++)
        {
            var rowBytes = rows[ row ];
            for (int b = 0; b < bytesPerRow; b++)
            {
                var dataByte = rowBytes[ b ];
                if (dataByte == 0) continue;
                int colBase = b * 8;
                int startX = -1;
                for (int bit = 7; bit >= 0; bit--)
                {
                    bool isSet = (dataByte & (1 << bit)) != 0;
                    int x = colBase + (7 - bit);
                    if (isSet)
                    {
                        if (startX == -1) startX = x;
                    }
                    else if (startX != -1)
                    {
                        DrawBitmapRun(pdf, anchorX, anchorY, startX, row, x - startX);
                        startX = -1;
                    }
                }
                if (startX != -1)
                    DrawBitmapRun(pdf, anchorX, anchorY, startX, row, (colBase + 8) - startX);
            }
        }
    }

    private static void DrawBitmapRun(FPdf pdf, double anchorX, double anchorY, int x, int row, int length)
    {
        // 1-dot-tall horizontal run = a length x 1 filled rect.
        DrawFilledRect(pdf, anchorX + x, anchorY + row, length, 1, Microsoft.Xna.Framework.Color.Black);
    }

    private void RecallGraphic(FPdf pdf, string arg2)
    {
        // ^XG<name>,<mx>,<my> — draw a previously-registered image at the
        // current ^FO. mx / my are integer magnification factors (1..10).
        EnsurePage();
        var body = arg2.Substring(3).Trim();
        var parts = body.Split(',');
        if (parts.Length == 0 || string.IsNullOrEmpty(parts[ 0 ])) return;
        var name = parts[ 0 ];
        var mx = parts.ToInt(1, 1);
        var my = parts.ToInt(2, 1);

        // ZPL ~DG-loaded bitmap stored from the same label stream takes
        // precedence over an externally-registered image file. ^FT
        // anchors the bottom-left of the bitmap instead of the top-left;
        // shift the anchor up by the rendered image height in that case.
        if (_storedGraphics.TryGetValue(name, out var stored))
        {
            var anchorY = pdf.Y;
            if (this.CurrentField.Origin == FieldDefinition.OriginEnum.LeftBottom)
                anchorY -= stored.rows.Count * my;
            DrawBitmapRowsScaled(pdf, stored.rows, stored.bytesPerRow, pdf.X, anchorY, mx, my);
            return;
        }
        if (!_graphics.TryGetValue(name, out var path) || !System.IO.File.Exists(path)) return;

        try
        {
            using var codec = SkiaSharp.SKCodec.Create(path);
            var info = codec.Info;
            // Source pixels map 1:1 to printer dots → user units (dots).
            pdf.Image(path, pdf.X, pdf.Y, info.Width * mx, info.Height * my);
        }
        catch
        {
            // ignore unreadable images — the rest of the label should still
            // print.
        }
    }

    /// <summary>
    /// Extract every <c>~DG</c> download-graphic from the ZPL stream, decode
    /// the compressed hex payload via <see cref="DecodeAsciiGraphicField"/>
    /// (same encoding as ^GFA) and stash the resulting rows under the
    /// graphic name so ^XG can recall it. Returns the ZPL with the ~DG
    /// segments stripped — the main parser splits on '^' and would
    /// otherwise drop the data on the floor and start interpreting the
    /// hex pairs as commands.
    /// </summary>
    private string PreprocessDownloadGraphics(string zpl)
    {
        return DgPattern.Replace(zpl, m =>
        {
            try
            {
                var name = m.Groups[ 1 ].Value.Trim();
                var bpr = int.Parse(m.Groups[ 3 ].Value);
                var data = m.Groups[ 4 ].Value;
                if (bpr <= 0) return string.Empty;
                var rows = DecodeAsciiGraphicField(data, bpr);
                _storedGraphics[ name ] = (rows, bpr);
            }
            catch
            {
                // skip malformed ~DG, the rest of the label should still print
            }
            return string.Empty;
        });
    }

    /// <summary>
    /// Variant of <see cref="DrawBitmapRows"/> that magnifies each source
    /// pixel by <paramref name="mx"/> × <paramref name="my"/>. ^XG's
    /// integer scale factors map a 1×1 source pixel onto an mx×my dot
    /// rect.
    /// </summary>
    private static void DrawBitmapRowsScaled(FPdf pdf, List<byte[]> rows, int bytesPerRow, double anchorX, double anchorY, int mx, int my)
    {
        if (mx < 1) mx = 1;
        if (my < 1) my = 1;
        var color = Microsoft.Xna.Framework.Color.Black;
        for (int row = 0; row < rows.Count; row++)
        {
            var rowBytes = rows[ row ];
            for (int b = 0; b < bytesPerRow; b++)
            {
                var dataByte = rowBytes[ b ];
                if (dataByte == 0) continue;
                int colBase = b * 8;
                int startX = -1;
                for (int bit = 7; bit >= 0; bit--)
                {
                    bool isSet = (dataByte & (1 << bit)) != 0;
                    int x = colBase + (7 - bit);
                    if (isSet)
                    {
                        if (startX == -1) startX = x;
                    }
                    else if (startX != -1)
                    {
                        DrawFilledRect(pdf, anchorX + startX * mx, anchorY + row * my, (x - startX) * mx, my, color);
                        startX = -1;
                    }
                }
                if (startX != -1)
                    DrawFilledRect(pdf, anchorX + startX * mx, anchorY + row * my, ((colBase + 8) - startX) * mx, my, color);
            }
        }
    }

    private void Diagonal(FPdf pdf, string arg2)
    {
        throw new NotImplementedException();
    }

    private void Ellipse(FPdf pdf, string arg2)
    {
        throw new NotImplementedException();
    }

    private void Circle(FPdf pdf, string arg2)
    {
        // ^GC diameter, thickness, color [example]
        // ^GE width, height, thickness, color [example]
        // ^GD width, height, thickness, color, orientation [example]
        // ^GB width, height, thickness, color, rounding [example]
    }

    private void FieldReverse(FPdf pdf, string arg2)
    {
        // ^FR: invert the next graphic field (^GB / ^GC / ^GD / ^GE / ^FD) so
        // a black fill is drawn white and vice versa. The flag clears when the
        // field finishes (FinishField -> ResetField).
        this.CurrentField.Reverse = true;
    }

    private void EscapeCharacter(FPdf pdf, string arg2)
    {
        this.CurrentField.EscapeCharacter = arg2.Substring(3);
        /*
            ^XA
            ^FX FH command parameters:
            ^FX - escape character
            ^CI28
            ^FO25,25^A0,40^FH_^FDone: _5E^FS
            ^FO25,75^A0,40^FH_^FDtwo: _7E^FS
            ^FO25,125^A0,40^FH_^FDthree: _C2_BF?^FS
            ^XZ             
         */

        // Sets the escape character to encode hexadecimal characters with the ^FH command
    }

    /// <summary>
    /// ^CF
    /// </summary>
    /// <param name="pdf"></param>
    /// <param name="arg2"></param>
    private void SetDefaultFont(FPdf pdf, string arg2)
    {
        /*
            ^CF fontName, height, width
            Sets the default font. The default font is used by all subsequent text fields which do not specify a font using the ^A or ^A@ commands.
            Parameters:
            fontName: The name of the new default font. Font names are either a capital letter (A-Z) or a number (0-9). The default value is the previously configured value, or A if no value has been set.
            height: The height of the new default font, in dots. The default value depends on the font selected.
            width: The width of the new default font, in dots. The default value depends on the font selected.
            Example (common usage): ^CF0,50
            Example (full usage): ^CF0,50,50          
        */

        // Same case normalisation as the ^A? path.
        var fontSlot = char.ToUpperInvariant(arg2[ 3 ]).ToString();
        this.CurrentField.Font = fontSlot;
        this.DefaultFont = fontSlot;
        var body = arg2.Remove(0, 5);
        var parts = body.Split(',');
        if (parts.Length > 3)
            return;

        this.CurrentField.Thickness = parts.ToInt(0, this.CurrentField.Thickness);
        this.ThicknessDefault = parts.ToInt(0, this.ThicknessDefault);
        this.DefaultFontH = parts.ToInt(2, DefaultFontH);
        pdf.SetFontSize(100 * this.CurrentField.Thickness / Dpi);
    }

    private void CharacterSet(FPdf pdf, string arg2)
    {
        // 28 = UTF-8
    }

    private void RectangleBox(FPdf pdf, string arg2)
    {
        EnsurePage();
        // ^GB w, h, thickness, color, rounding
        // - w, h, thickness are in dots
        // - color: "B" (black, default) or "W" (white)
        // - When thickness >= min(w, h) the rectangle is fully filled.
        // - Otherwise it is an outline of width = thickness.
        // - Degenerate w=0 or h=0 means a vertical or horizontal line of
        //   length max(w, h) and width = thickness.

        arg2 = arg2.Substring(3);
        var parts = arg2.Split(',');
        var width = parts.ToMilimeters(0, this.CurrentField.Thickness, Dpi);
        var height = parts.ToMilimeters(1, this.CurrentField.Thickness, Dpi);
        var thickness = parts.ToMilimeters(2, 1, Dpi);
        var colorCode = parts.Length > 3 && !string.IsNullOrEmpty(parts[ 3 ]) ? parts[ 3 ] : "B";
        // rounding (parts[4]) ignored for now.

        var isReverse = this.CurrentField.Reverse;
        var fill = !(colorCode == "W" ^ isReverse);

        var color = fill ? Microsoft.Xna.Framework.Color.Black : Microsoft.Xna.Framework.Color.White;
        var x = pdf.X;
        var y = pdf.Y;

        if (width <= 0 || height <= 0)
        {
            // Horizontal or vertical line. Draw it as a filled rectangle of
            // thickness `thickness` centred on the field origin axis.
            var w = width <= 0 ? thickness : width;
            var h = height <= 0 ? thickness : height;
            DrawFilledRect(pdf, x, y, w, h, color);
            return;
        }

        if (thickness >= Math.Min(width, height))
        {
            // Fully filled rectangle.
            DrawFilledRect(pdf, x, y, width, height, color);
        }
        else
        {
            // Outline only: draw four filled bars so the line width matches
            // `thickness` exactly (FPdf.Rect uses the current LineWidth, but
            // setting it for every ^GB and restoring it is more bookkeeping
            // than just stamping four bars).
            DrawFilledRect(pdf, x, y, width, thickness, color);                                      // top
            DrawFilledRect(pdf, x, y + height - thickness, width, thickness, color);                 // bottom
            DrawFilledRect(pdf, x, y, thickness, height, color);                                     // left
            DrawFilledRect(pdf, x + width - thickness, y, thickness, height, color);                 // right
        }
    }

    /// <summary>
    /// Stamp a solid-fill axis-aligned rectangle at absolute user-unit
    /// coordinates. Centralises the <c>DrawArea</c> pentagon trick (5
    /// points, last one repeating the first) used by ^GB outlines, the
    /// ^GFA bitmap renderer and the barcode module stamper, so the
    /// implicit <c>(H - pdf.Y) - point.Y</c> transform is applied in
    /// exactly one place.
    /// </summary>
    internal static void DrawFilledRect(FPdf pdf, double absX, double absY, double w, double h, Microsoft.Xna.Framework.Color color)
    {
        var relY = absY - pdf.Y;
        var points = new[]
        {
            new Ego.PDF.Data.DrawingPoint(absX,         relY),
            new Ego.PDF.Data.DrawingPoint(absX + w,     relY),
            new Ego.PDF.Data.DrawingPoint(absX + w,     relY + h),
            new Ego.PDF.Data.DrawingPoint(absX,         relY + h),
            new Ego.PDF.Data.DrawingPoint(absX,         relY),
        };
        pdf.DrawArea(color, 0.00, points);
    }

    private void Voidf(FPdf pdf, string arg2)
    {
        //throw new NotImplementedException(arg2);
    }

    private void WriteText(FPdf pdf, string arg2)
    {
        var text = arg2.Substring(3);
        this.CurrentField.Value = text;
    }

    /// <summary>
    /// ^A?
    /// </summary>
    /// <param name="pdf"></param>
    /// <param name="arg2"></param>
    private void SetFont(FPdf pdf, string arg2)
    {
        // ^A font, height, width

        /*
            ^ADN
            1.Alter the numbers after the ^ADN,x,x command.
            • 18,10 is the smallest size you can make the D font.
            • The first number is the height of the font in dots.The second number is the width in dots.
            • You can use direct multiples up to ten times that size as a maximum.
            180,100 is the largest you can make the D font.
            • 25,18 would not be a valid size. The printer rounds to the next recognizable s
        */

        // Font slot identifier is case-insensitive: ^AeN must end up
        // selecting the same "E" entry that ^AEN does.
        this.CurrentField.Font = char.ToUpperInvariant(arg2[ 2 ]).ToString();
        var body = arg2.Remove(0, 2);
        var parts = body.Split(',');
        if (parts.Length != 3)
            return;

        if (parts[ 0 ].Length > 1)
            this.CurrentField.Orientation = parts[ 0 ][ 1 ].ToString(); // (A-Z) and was trimmed to one character

        this.CurrentField.Thickness = parts.ToInt(1, this.CurrentField.Thickness);

        var size = this.CurrentField.GetFontSize(this.CurrentField.Font, Dpi);

        decimal height = parts.ToInt(1, size.DotsH);
        decimal width = parts.ToInt(2, size.DotsW);
        // Real Zebra printers quantize sizes to integer multiples of
        // the font's natural bitmap dimensions (rounding 1.5x up to 2x
        // visibly oversized our ^AVN,120,100 fields). Vector rendering
        // in PDF has no such limitation, so we honour the requested
        // height/width exactly. Cap at 10x natural per the published
        // Zebra ceiling.
        width  = Math.Min(width,  size.DotsW * 10);
        height = Math.Min(height, size.DotsH * 10);

        CurrentField.DotsW = Convert.ToInt32(width);
        CurrentField.DotsH = Convert.ToInt32(height);
        pdf.SetFontSize(100 * this.CurrentField.Thickness / Dpi);
    }

    private void SetLocation(FPdf pdf, string body)
        => SetFieldOrigin(pdf, body, FieldDefinition.OriginEnum.LeftTop);

    private void SetLocationBottom(FPdf pdf, string body)
        => SetFieldOrigin(pdf, body, FieldDefinition.OriginEnum.LeftBottom);

    /// <summary>
    /// Shared body of <c>^FO</c> and <c>^FT</c>. The two commands are
    /// identical bar the <see cref="FieldDefinition.OriginEnum"/> they
    /// install on the current field: ^FO anchors at the top-left of the
    /// glyph box, ^FT anchors on the baseline. The third parameter is
    /// ZPL's text alignment hint (0 left / 1 right / 2 auto by reading
    /// direction).
    /// </summary>
    private void SetFieldOrigin(FPdf pdf, string body, FieldDefinition.OriginEnum origin)
    {
        EnsurePage();
        body = body.Substring(3);
        var parts = body.Split(',');
        var X = parts.ToMilimeters(0, 0, Dpi);
        var Y = parts.ToMilimeters(1, 0, Dpi);
        pdf.SetXY(this.TopX + X, this.TopY + Y);
        pdf.SetXY(X, this.TopY + Y);

        this.CurrentField.Origin = origin;
        this.Alignment = parts.ToString(2, this.Alignment);
    }

    public void PrintToken()
    {
        Pdf.X = this.TopX;
        Pdf.Y = this.TopY;
    }


    public void SetVariableFont(string variableFont, string style = "")
    {
        this.VariableFont = variableFont;
        this.VariableStyle = style;
        this.CurrentField.VariableFont = variableFont;
        this.CurrentField.VariableStyle = style;
    }

    public void SetMonospaceFont(string monospaceFont, string style = "")
    {
        this.MonospaceFont = monospaceFont;
        this.MonospaceStyle = style;
        this.CurrentField.MonospaceFont = monospaceFont;
        this.CurrentField.MonospaceStyle = style;
    }

    /// <summary>
    /// Register a condensed font to back the scalable proportional ZPL slots
    /// (P, Q, R, S, T, U, V). When set, those slots render with this font
    /// at the requested aspect ratio. When unset, they fall back to the
    /// variable/monospace font and the renderer fakes the narrow shape with
    /// the PDF horizontal text scale (Tz) — readable but visibly squeezed
    /// at aggressive width settings.
    /// </summary>
    public void SetCondensedFont(string condensedFont, string style = "")
    {
        this.CondensedFont = condensedFont;
        this.CondensedStyle = style;
        this.CurrentField.CondensedFont = condensedFont;
        this.CurrentField.CondensedStyle = style;
    }

    /// <summary>
    /// Tune the global em multiplier the renderer applies whenever a
    /// field lands on the condensed font slot. See
    /// <see cref="CondensedFontScale"/> for the rationale.
    /// </summary>
    public void SetCondensedFontScale(double scale)
    {
        this.CondensedFontScale = scale;
        this.CurrentField.CondensedFontScale = scale;
    }

    public class CharSize
    {
        public CharSize(string FontName, int DotsH, int DotsW, double InH, double InW, double InChars, double MmH, double MmW, double MmChars)
        {
            this.FontName = FontName;
            this.DotsH = DotsH;
            this.DotsW = DotsW;
            this.InH = InH;
            this.InW = InW;
            this.InChars = InChars;
            this.MmH = MmH;
            this.MmW = MmW;
            this.MmChars = MmChars;
        }

        public string FontName { get; }
        public int DotsH { get; }
        public int DotsW { get; }
        public double InH { get; }
        public double InW { get; }
        public double InChars { get; }
        public double MmH { get; }
        public double MmW { get; }
        public double MmChars { get; }
    }
}
