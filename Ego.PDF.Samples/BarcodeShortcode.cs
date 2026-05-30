using Ego.PDF.Barcodes;
using Ego.PDF.Markdown;
using Ego.PDF.Markdown.Shortcodes;
using Microsoft.Xna.Framework;

namespace Ego.PDF.Samples;

/// <summary>
/// Wires the <see cref="BarcodeRenderer"/> from EgoPDF.Barcodes into
/// the EgoPDF.Markdown shortcode system. Register a single instance
/// against the name <c>"barcode"</c> on a <see cref="MarkdownTheme"/>:
///
/// <code>
/// var theme = MarkdownTheme.Default;
/// theme.Shortcodes.Register("barcode", new BarcodeShortcode());
/// MarkdownRenderer.Render(pdf, markdown, theme);
/// </code>
///
/// Then in Markdown:
///
/// <code>
/// [[barcode type=qr data="https://example.com" size=30]]
/// </code>
///
/// Supported <c>type</c> values: qr, datamatrix, aztec, pdf417,
/// code128, code39, code93, itf, codabar, ean13, ean8, upca.
/// Common options:
///   data       — the payload (required).
///   size       — square 2D side length, in mm (default 30).
///   width      — 1D module width OR PDF417 explicit width, mm.
///   height     — 1D bar height OR PDF417 height, mm.
///   color      — hex like "#cc695f" (defaults to body colour).
///   align      — left | center | right (default left).
/// </summary>
public sealed class BarcodeShortcode : IShortcodeHandler
{
    public void Render(FPdf pdf, ShortcodeBlock block, MarkdownTheme theme)
    {
        if (!block.Options.TryGetValue("data", out var data) || string.IsNullOrEmpty(data))
        {
            return;
        }

        var type = ShortcodeOptions.GetString(block.Options, "type", "qr").ToLowerInvariant();
        var color = ShortcodeOptions.GetColor(block.Options, "color", theme.BodyColor);
        var align = ShortcodeOptions.GetString(block.Options, "align", "left").ToLowerInvariant();

        var paddingTop = theme.ParagraphSpacing;
        var paddingBottom = theme.ParagraphSpacing;
        pdf.Y += paddingTop;

        switch (type)
        {
            case "qr":
            {
                var size = ShortcodeOptions.GetDouble(block.Options, "size", 30);
                var x = AlignX(pdf, size, align);
                BarcodeRenderer.DrawQrCode(pdf, data, x, pdf.Y, size, color);
                pdf.Y += size;
                break;
            }
            case "datamatrix":
            {
                var size = ShortcodeOptions.GetDouble(block.Options, "size", 25);
                var x = AlignX(pdf, size, align);
                BarcodeRenderer.DrawDataMatrix(pdf, data, x, pdf.Y, size, color);
                pdf.Y += size;
                break;
            }
            case "aztec":
            {
                var size = ShortcodeOptions.GetDouble(block.Options, "size", 25);
                var x = AlignX(pdf, size, align);
                BarcodeRenderer.DrawAztec(pdf, data, x, pdf.Y, size, color);
                pdf.Y += size;
                break;
            }
            case "pdf417":
            {
                var w = ShortcodeOptions.GetDouble(block.Options, "width", 60);
                var h = ShortcodeOptions.GetDouble(block.Options, "height", 20);
                var x = AlignX(pdf, w, align);
                BarcodeRenderer.DrawPdf417(pdf, data, x, pdf.Y, w, h, color);
                pdf.Y += h;
                break;
            }
            case "code128":
                Draw1D(pdf, data, block, BarcodeRenderer.DrawCode128, color, align);
                break;
            case "code39":
                Draw1D(pdf, data, block, BarcodeRenderer.DrawCode39, color, align);
                break;
            case "code93":
                Draw1D(pdf, data, block, BarcodeRenderer.DrawCode93, color, align);
                break;
            case "itf":
                Draw1D(pdf, data, block, BarcodeRenderer.DrawInterleaved2of5, color, align);
                break;
            case "codabar":
                Draw1D(pdf, data, block, BarcodeRenderer.DrawCodabar, color, align);
                break;
            case "ean13":
                Draw1D(pdf, data, block, BarcodeRenderer.DrawEan13, color, align);
                break;
            case "ean8":
                Draw1D(pdf, data, block, BarcodeRenderer.DrawEan8, color, align);
                break;
            case "upca":
                Draw1D(pdf, data, block, BarcodeRenderer.DrawUpcA, color, align);
                break;
            default:
                // Unknown type — silently drop, the surrounding text continues.
                return;
        }

        pdf.Y += paddingBottom;
        pdf.SetX(pdf.LeftMargin);
    }

    private delegate void OneDDrawer(FPdf pdf, string data, double x, double y, double moduleWidth, double height, Color? color);

    private static void Draw1D(FPdf pdf, string data, ShortcodeBlock block,
        OneDDrawer drawer, Color color, string align)
    {
        var moduleWidth = ShortcodeOptions.GetDouble(block.Options, "width", 0.5);
        var height = ShortcodeOptions.GetDouble(block.Options, "height", 14);
        // We don't know the natural width of a 1D barcode without
        // encoding it, so for "center" / "right" we approximate using
        // the available column width as a generous bounding box.
        var x = align == "left"
            ? pdf.LeftMargin
            : AlignX(pdf, pdf.W - pdf.LeftMargin - pdf.RightMargin, align);
        drawer(pdf, data, x, pdf.Y, moduleWidth, height, color);
        pdf.Y += height;
    }

    private static double AlignX(FPdf pdf, double width, string align)
    {
        var content = pdf.W - pdf.LeftMargin - pdf.RightMargin;
        switch (align)
        {
            case "center": return pdf.LeftMargin + (content - width) / 2.0;
            case "right":  return pdf.W - pdf.RightMargin - width;
            default:       return pdf.LeftMargin;
        }
    }
}
