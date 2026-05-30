using Ego.PDF.Markdown.Shortcodes;
using Microsoft.Xna.Framework;

namespace Ego.PDF.Markdown;

/// <summary>
/// Visual theme used by <see cref="MarkdownRenderer"/>: fonts, sizes,
/// colours and spacing. The default theme uses the FPDF core fonts
/// (Helvetica / Courier) so it works out of the box without loading
/// any TTF; <see cref="EgoPdf"/> uses a brand-coloured variant.
///
/// Themes are mutable on purpose — tweak only the properties you care
/// about and pass the instance to <see cref="MarkdownRenderer.Render"/>.
/// </summary>
public sealed class MarkdownTheme
{
    // ---- Fonts -------------------------------------------------------------

    /// <summary>Family used for body text, paragraphs and list items.</summary>
    public string BodyFont { get; set; } = "Helvetica";

    /// <summary>Family used for H1-H6 headings.</summary>
    public string HeadingFont { get; set; } = "Helvetica";

    /// <summary>Family used for inline code (`like this`) and fenced code blocks.</summary>
    public string CodeFont { get; set; } = "Courier";

    // ---- Sizes (in points) -------------------------------------------------

    public double BodyFontSize { get; set; } = 11;

    /// <summary>Font sizes for H1..H6 (index 0 = H1). Six entries.</summary>
    public double[] HeadingSizes { get; set; } = new double[] { 26, 20, 16, 14, 12, 11 };

    public double CodeFontSize { get; set; } = 9.5;

    // ---- Spacing (in user units, default mm) -------------------------------

    /// <summary>Line height used by <c>Write()</c> for paragraphs.</summary>
    public double LineHeight { get; set; } = 5.5;

    /// <summary>Extra vertical space inserted after a paragraph.</summary>
    public double ParagraphSpacing { get; set; } = 3;

    /// <summary>Extra vertical space above a heading.</summary>
    public double HeadingSpacingAbove { get; set; } = 8;

    /// <summary>Extra vertical space below a heading.</summary>
    public double HeadingSpacingBelow { get; set; } = 3;

    /// <summary>Indent applied per list nesting level.</summary>
    public double ListIndent { get; set; } = 6;

    /// <summary>Width reserved for the list marker (bullet or "N.").</summary>
    public double ListMarkerWidth { get; set; } = 5;

    /// <summary>Internal padding around fenced/indented code blocks.</summary>
    public double CodeBlockPadding { get; set; } = 2.5;

    // ---- Colours -----------------------------------------------------------

    public Color BodyColor { get; set; } = new Color(26, 29, 38);
    public Color HeadingColor { get; set; } = new Color(26, 29, 38);

    /// <summary>Used for links, autolinks and emphasis markers.</summary>
    public Color AccentColor { get; set; } = new Color(204, 105, 95);

    public Color CodeColor { get; set; } = new Color(26, 29, 38);
    public Color CodeBackground { get; set; } = new Color(247, 238, 236);

    /// <summary>Colour of horizontal rules and code-block outline.</summary>
    public Color RuleColor { get; set; } = new Color(220, 220, 224);

    /// <summary>Colour for list markers and fallback placeholders (e.g. remote images).</summary>
    public Color MutedColor { get; set; } = new Color(110, 115, 130);

    // ---- Glyphs ------------------------------------------------------------

    /// <summary>
    /// Handlers for <c>[[name k=v ...]]</c> shortcodes. Register a handler
    /// per name (e.g. <c>theme.Shortcodes.Register("barcode", ...)</c>);
    /// unknown shortcodes fall back to muted-italic raw text.
    /// </summary>
    public ShortcodeRegistry Shortcodes { get; } = new ShortcodeRegistry();

    /// <summary>
    /// Character used as bullet for unordered lists. Defaults to "-" so
    /// the marker renders with the FPDF core fonts (which only ship
    /// widths for the Latin-1 range; the typographic bullet U+2022 is
    /// outside that range).
    /// </summary>
    public string BulletGlyph { get; set; } = "-";

    // ---- Built-in themes ---------------------------------------------------

    /// <summary>
    /// Safe out-of-the-box theme: FPDF core fonts only, brand-leaning palette.
    /// No <c>LoadFont</c> required.
    /// </summary>
    public static MarkdownTheme Default => new();

    /// <summary>
    /// Editorial theme using Poppins (headings) + Roboto (body) +
    /// Roboto Mono (code). The caller must have loaded those families
    /// via <c>FPdf.LoadFont</c> / <c>FPdf.AddFont</c> beforehand.
    /// </summary>
    public static MarkdownTheme EgoPdf => new()
    {
        BodyFont = "Roboto",
        HeadingFont = "Poppins",
        CodeFont = "RobotoMono",
    };
}
