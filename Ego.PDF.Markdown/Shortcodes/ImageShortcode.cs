using System;
using System.IO;
using Ego.PDF.Data;
using SkiaSharp;

namespace Ego.PDF.Markdown.Shortcodes;

/// <summary>
/// Built-in <c>[[image src="..." ...]]</c> shortcode. CommonMark's
/// <c>![alt](url)</c> syntax has no way to specify size, alignment,
/// caption or link target; this shortcode covers those cases while
/// the plain markdown form still works for the "no thinking required"
/// scenario.
///
/// Options:
///   src      — file path (required). Absolute, or relative to the
///              current working directory. Remote URLs are skipped.
///   width    — output width in mm. Optional. When omitted, derives
///              the size from the image's pixel dimensions and the
///              theme's <see cref="MarkdownTheme.ImageDpi"/>.
///   height   — output height in mm. Optional. Defaults to whatever
///              keeps the aspect ratio.
///   align    — left | center | right (default left).
///   caption  — text rendered below the image in muted italic.
///   link     — URL; if set, the image becomes a clickable area.
/// </summary>
public sealed class ImageShortcode : IShortcodeHandler
{
    public static readonly ImageShortcode Instance = new ImageShortcode();

    private ImageShortcode() { }

    public void Render(FPdf pdf, ShortcodeBlock block, MarkdownTheme theme)
    {
        var src = ShortcodeOptions.GetString(block.Options, "src");
        if (string.IsNullOrEmpty(src)) return;
        if (IsRemote(src)) return; // phase 1: no http fetching

        var path = Path.IsPathRooted(src) ? src : Path.GetFullPath(src);
        if (!File.Exists(path)) return;

        var contentWidth = pdf.W - pdf.LeftMargin - pdf.RightMargin;

        var (pixelWidth, pixelHeight) = ReadDimensions(path);
        if (pixelWidth <= 0 || pixelHeight <= 0) return;
        var aspect = (double)pixelHeight / pixelWidth;

        var requestedWidth  = ShortcodeOptions.GetDouble(block.Options, "width",  0);
        var requestedHeight = ShortcodeOptions.GetDouble(block.Options, "height", 0);

        double width, height;
        if (requestedWidth > 0 && requestedHeight > 0)
        {
            width = requestedWidth;
            height = requestedHeight;
        }
        else if (requestedWidth > 0)
        {
            width = requestedWidth;
            height = width * aspect;
        }
        else if (requestedHeight > 0)
        {
            height = requestedHeight;
            width = height / aspect;
        }
        else
        {
            // Auto: natural pixel dimensions at the theme's print DPI,
            // capped at the column width.
            width = PixelsToMm(pixelWidth, theme.ImageDpi);
            if (width > contentWidth) width = contentWidth;
            height = width * aspect;
        }

        var align = ShortcodeOptions.GetString(block.Options, "align", "left").ToLowerInvariant();
        var x = align switch
        {
            "center" => pdf.LeftMargin + (contentWidth - width) / 2.0,
            "right"  => pdf.W - pdf.RightMargin - width,
            _        => pdf.LeftMargin,
        };

        pdf.Y += theme.ParagraphSpacing;
        var y = pdf.Y;

        pdf.Image(path, x, y, width, height);

        var link = ShortcodeOptions.GetString(block.Options, "link");
        if (!string.IsNullOrEmpty(link))
        {
            pdf.Link(x, y, width, height, new LinkDataUri(link));
        }

        pdf.Y = y + height;

        var caption = ShortcodeOptions.GetString(block.Options, "caption");
        if (!string.IsNullOrEmpty(caption))
        {
            pdf.Y += 1; // small gap between image and caption
            pdf.SetFont(theme.BodyFont, "I", theme.BodyFontSize - 1);
            pdf.SetTextColor(theme.MutedColor);
            pdf.SetXY(x, pdf.Y);
            pdf.Cell(width, theme.LineHeight, caption, "0", 0, AlignEnum.Center);
            pdf.Y += theme.LineHeight;
            pdf.SetTextColor(theme.BodyColor);
        }

        pdf.Y += theme.ParagraphSpacing;
        pdf.SetX(pdf.LeftMargin);
    }

    private static bool IsRemote(string src) =>
        src.StartsWith("http:",  StringComparison.OrdinalIgnoreCase) ||
        src.StartsWith("https:", StringComparison.OrdinalIgnoreCase) ||
        src.StartsWith("data:",  StringComparison.OrdinalIgnoreCase);

    private static (int width, int height) ReadDimensions(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var codec = SKCodec.Create(stream);
            if (codec is null) return (0, 0);
            var info = codec.Info;
            return (info.Width, info.Height);
        }
        catch
        {
            return (0, 0);
        }
    }

    internal static double PixelsToMm(int pixels, int dpi) => pixels * 25.4 / dpi;
}
