using System;
using System.IO;
using Ego.PDF.Data;
using Ego.PDF.Markdown;
using Ego.PDF.Markdown.Shortcodes;
using SkiaSharp;

namespace Ego.PDF.Samples;

/// <summary>
/// <c>[[imagepair]]</c> markdown shortcode: lays two images side by
/// side in equal half-column cells with optional centred captions
/// underneath. Built on the <see cref="FPdf.Row"/> primitive so the
/// gutter and cell widths are derived without manual arithmetic.
///
/// Use it for before/after comparisons, juxtaposed artwork or when a
/// full-column natural-size image would dominate the page.
///
/// Options:
///   src1, src2       — file paths (required). Remote URLs are skipped.
///   caption1, caption2 — muted-italic text shown below each image.
///   gap              — millimetres of whitespace between the cells (default 6).
///   maxheight        — vertical cap per image in mm so portrait
///                      orientations don't tower over a portrait page
///                      (default 80).
///   borders          — when "true", a hairline frames each cell so
///                      the layout reads more "tabular".
/// </summary>
public sealed class ImagePairShortcode : IShortcodeHandler
{
    public void Render(FPdf pdf, ShortcodeBlock block, MarkdownTheme theme)
    {
        var src1 = ShortcodeOptions.GetString(block.Options, "src1");
        var src2 = ShortcodeOptions.GetString(block.Options, "src2");
        if (string.IsNullOrEmpty(src1) || string.IsNullOrEmpty(src2)) return;
        if (IsRemote(src1) || IsRemote(src2)) return;

        var path1 = Path.IsPathRooted(src1) ? src1 : Path.GetFullPath(src1);
        var path2 = Path.IsPathRooted(src2) ? src2 : Path.GetFullPath(src2);
        if (!File.Exists(path1) || !File.Exists(path2)) return;

        var caption1  = ShortcodeOptions.GetString(block.Options, "caption1");
        var caption2  = ShortcodeOptions.GetString(block.Options, "caption2");
        var gap       = ShortcodeOptions.GetDouble(block.Options, "gap", 6);
        var maxHeight = ShortcodeOptions.GetDouble(block.Options, "maxheight", 80);
        var borders   = ShortcodeOptions.GetString(block.Options, "borders", "false")
                            .Equals("true", StringComparison.OrdinalIgnoreCase);

        var contentWidth = pdf.W - pdf.LeftMargin - pdf.RightMargin;

        // Provisional row -- height filled in after we measure both images.
        var rowOrigin = pdf.Y + theme.ParagraphSpacing;
        var rowBounds = new Rect(pdf.LeftMargin, rowOrigin, contentWidth, maxHeight);
        var slots = pdf.Row(rowBounds, 2, gap);

        var captionH = string.IsNullOrEmpty(caption1) && string.IsNullOrEmpty(caption2)
            ? 0
            : theme.LineHeight + 1; // caption row + small padding

        var h1 = DrawCell(pdf, theme, path1, caption1, slots[0], maxHeight, borders);
        var h2 = DrawCell(pdf, theme, path2, caption2, slots[1], maxHeight, borders);

        var consumed = Math.Max(h1, h2);
        pdf.Y = rowOrigin + consumed + theme.ParagraphSpacing;
        pdf.SetX(pdf.LeftMargin);
    }

    /// <summary>
    /// Render one image + caption inside <paramref name="slot"/>; return
    /// the vertical extent actually used so the caller can match the
    /// taller of the two when picking the row height.
    /// </summary>
    private static double DrawCell(FPdf pdf, MarkdownTheme theme, string path,
        string caption, Rect slot, double maxHeight, bool borders)
    {
        var (pxW, pxH) = ReadDimensions(path);
        if (pxW <= 0 || pxH <= 0) return 0;

        // Fit-and-centre: scale to slot width first, then cap by maxHeight.
        var aspect = (double)pxH / pxW;
        var w = slot.W;
        var h = w * aspect;
        if (h > maxHeight)
        {
            h = maxHeight;
            w = h / aspect;
        }

        var x = slot.X + (slot.W - w) / 2.0;
        var y = slot.Y;
        pdf.Image(path, x, y, w, h);

        double captionH = 0;
        if (!string.IsNullOrEmpty(caption))
        {
            captionH = theme.LineHeight + 1;
            pdf.SetFont(theme.BodyFont, "I", theme.BodyFontSize - 1);
            pdf.SetTextColor(theme.MutedColor);
            pdf.SetXY(slot.X, y + h + 1);
            pdf.Cell(slot.W, theme.LineHeight, caption, "0", 0, AlignEnum.Center);
            pdf.SetTextColor(theme.BodyColor);
        }

        if (borders)
        {
            pdf.SetDrawColor(EgoPdfBrand.HairLine);
            pdf.SetLineWidth(0.2);
            pdf.Rect(slot.X, slot.Y, slot.W, h + captionH, "D");
        }

        return h + captionH;
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
}
