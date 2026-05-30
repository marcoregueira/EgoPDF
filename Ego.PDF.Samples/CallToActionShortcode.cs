using Ego.PDF.Data;
using Ego.PDF.Markdown;
using Ego.PDF.Markdown.Shortcodes;
using Microsoft.Xna.Framework;
using System;

namespace Ego.PDF.Samples;

/// <summary>
/// Renders a "call to action" button: a filled rectangle with centered
/// bold text and a clickable URL annotation. Mirrors the
/// <c>[button text="..." url="..."]</c> shortcode commonly used in
/// blog content systems, in a PDF-friendly form.
///
/// Register with:
/// <code>
/// theme.Shortcodes.Register("cta", new CallToActionShortcode());
/// </code>
///
/// Markdown usage:
/// <code>
/// [[cta text="Download the report" url="https://example.com" background="#1a1d26"]]
/// </code>
///
/// Options:
///   text       — button label (default "Learn more").
///   url        — link target. Omitted => static button, no annotation.
///   background — hex like "#cc695f" (default theme accent colour).
///   color      — text colour hex (default white).
///   width      — mm; default auto-sized to text + 10 mm padding each
///                side, capped at the column width.
///   height     — mm (default 12).
///   align      — left | center | right (default center).
/// </summary>
public sealed class CallToActionShortcode : IShortcodeHandler
{
    public void Render(FPdf pdf, ShortcodeBlock block, MarkdownTheme theme)
    {
        var text = ShortcodeOptions.GetString(block.Options, "text", "Learn more");
        var url = ShortcodeOptions.GetString(block.Options, "url");

        var background = ShortcodeOptions.GetColor(block.Options, "background", theme.AccentColor);
        var foreground = ShortcodeOptions.GetColor(block.Options, "color", Color.White);
        var height = ShortcodeOptions.GetDouble(block.Options, "height", 12);
        var requestedWidth = ShortcodeOptions.GetDouble(block.Options, "width", 0);
        var align = ShortcodeOptions.GetString(block.Options, "align", "center").ToLowerInvariant();

        // Measure the label so we can autosize when no explicit width
        // is supplied. The font we set here is also the one used for
        // drawing later — keep both calls in sync.
        pdf.SetFont(theme.BodyFont, "B", theme.BodyFontSize);
        var contentWidth = pdf.W - pdf.LeftMargin - pdf.RightMargin;
        var width = requestedWidth > 0
            ? requestedWidth
            : Math.Min(contentWidth, pdf.GetStringWidth(text) + 20);

        double x = align switch
        {
            "left"  => pdf.LeftMargin,
            "right" => pdf.W - pdf.RightMargin - width,
            _       => pdf.LeftMargin + (contentWidth - width) / 2.0,
        };

        pdf.Y += theme.ParagraphSpacing;
        var y = pdf.Y;

        pdf.SetFillColor(background);
        pdf.Rect(x, y, width, height, "F");

        pdf.SetTextColor(foreground);
        pdf.SetXY(x, y);
        pdf.Cell(width, height, text, "0", 0, AlignEnum.Center);

        if (!string.IsNullOrEmpty(url))
        {
            pdf.Link(x, y, width, height, new LinkDataUri(url));
        }

        pdf.Y = y + height + theme.ParagraphSpacing;
        pdf.SetX(pdf.LeftMargin);
        pdf.SetTextColor(theme.BodyColor);
    }
}
