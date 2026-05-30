using Ego.PDF.Data;
using Ego.PDF.Markdown;
using Microsoft.Xna.Framework;
using System;
using System.IO;

namespace Ego.PDF.Samples;

/// <summary>
/// Demonstrates the EgoPDF.Markdown package as a <b>mixed-mode</b>
/// document: an imperative brand header band on top, the rest rendered
/// from a Markdown string below. The same FPdf instance is shared, so
/// nothing stops a caller from interleaving hand-drawn sections with
/// Markdown blocks.
/// </summary>
public class SampleMarkdown : FPdf
{
    private SampleMarkdown(string file) : base(file) { }

    public static Stream GetSample(string file)
    {
        using var pdf = new SampleMarkdown(file);
        pdf.SetMargins(20, 20, 20);
        EgoPdfBrand.LoadPoppins(pdf);
        pdf.AddPage(PageSizeEnum.A4);

        // 1. Hand-drawn header band -- same primitive used by Sample8 /
        //    Sample9 / SamplePhotovoltaic, so the brand identity is the
        //    same across imperative and Markdown samples.
        DrawBrandBand(pdf);

        // 2. Markdown body. Tight, slightly more-indented list spacing
        //    so the bullet block reads as a cluster instead of a stack
        //    of breathing paragraphs.
        var theme = MarkdownTheme.Default;
        theme.ListItemSpacing = 0.5;
        theme.ListIndent      = 10;

        theme.Shortcodes.Register("barcode",   new BarcodeShortcode());
        theme.Shortcodes.Register("cta",       new CallToActionShortcode());
        theme.Shortcodes.Register("imagepair", new ImagePairShortcode());

        // The demo references a PNG sitting alongside the sample's
        // binary; build an absolute path so the markdown renders the
        // same regardless of the OS working directory at test time.
        var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "egopdf-logo.png")
            .Replace('\\', '/');
        var document = DocumentTemplate.Replace("{LOGO}", logoPath);

        MarkdownRenderer.Render(pdf, document, theme);
        pdf.Close();
        return pdf.Buffer.BaseStream;
    }

    private static void DrawBrandBand(SampleMarkdown pdf)
    {
        const double bandHeight = 44;
        pdf.SetFillColor(EgoPdfBrand.Dark);
        pdf.Rect(0, 0, pdf.W, bandHeight, "F");

        EgoPdfBrand.DrawWordmark(pdf, x: 20, y: 14, sizePt: 22,
            egoColor: Color.White, pdfColor: EgoPdfBrand.Accent, cellHeight: 11);

        pdf.SetFont("Poppins", "", 26);
        pdf.SetTextColor(Color.White);
        pdf.SetXY(20, 12);
        pdf.Cell(pdf.W - 40, 12, "MARKDOWN TO PDF", "0", 0, AlignEnum.Right);

        pdf.SetFont("Helvetica", "", 9);
        pdf.SetTextColor(EgoPdfBrand.SubText);
        pdf.SetXY(20, 28);
        pdf.Cell(pdf.W - 40, 6, "Hand-drawn header band + Markdown body", "0", 0, AlignEnum.Right);

        pdf.SetY(bandHeight + 6);
        pdf.SetX(pdf.LeftMargin);
        pdf.SetTextColor(EgoPdfBrand.Dark);
    }

    private const string DocumentTemplate = """
        This document is rendered by **EgoPDF.Markdown** using the *default*
        theme (no TTFs to load). The dark band above this paragraph is
        hand-drawn in egoPdf primitives — the rest is straight Markdown
        through the same FPdf instance. It exercises the Phase-1 happy
        path: headings, paragraphs, lists, code blocks, links and rules.

        ## Inline emphasis

        Markdown supports inline **bold**, *italic*, `inline code` and
        regular [links](https://github.com/marcoregueira/egopdf). Autolinks
        work too: <https://www.fpdf.org/>.

        ## Lists

        Unordered:

        - First item with a bit of trailing text so we can see the wrap behavior.
        - Second item.
        - Third item.

        Ordered:

        1. Step one.
        2. Step two.
        3. Step three.

        ## Code blocks

        ```csharp
        using var pdf = new FPdf("readme.pdf");
        pdf.AddPage(PageSizeEnum.A4);
        MarkdownRenderer.RenderFile(pdf, "README.md");
        pdf.Close();
        ```

        [[pagebreak]]

        > Markdown is a lightweight markup language with plain-text
        > formatting syntax. Its design allows it to be converted to
        > many output formats, including PDF.

        ## Shortcodes

        EgoPDF.Markdown ships with a tiny shortcode extension: any line
        wrapped in `[[ ... ]]` becomes a custom block dispatched to a
        registered handler. The Samples package wires a `barcode`
        shortcode on top of EgoPDF.Barcodes. Source first, then the
        rendered output:

        ```
        [[barcode type=qr data="https://github.com/marcoregueira/egopdf" size=30 align=center]]
        ```

        [[barcode type=qr data="https://github.com/marcoregueira/egopdf" size=30 align=center]]

        Or a 1D Code 128 across the column:

        ```
        [[barcode type=code128 data="EGOPDF-128" width=0.5 height=12]]
        ```

        [[barcode type=code128 data="EGOPDF-128" width=0.5 height=12]]

        Shortcodes are not limited to barcodes — anything you can draw
        on the page is fair game. The Samples package also ships a
        `cta` shortcode that drops a clickable button anywhere in the
        flow:

        ```
        [[cta text="Star egoPdf on GitHub" url="https://github.com/marcoregueira/egopdf"]]
        ```

        [[cta text="Star egoPdf on GitHub" url="https://github.com/marcoregueira/egopdf"]]

        [[pagebreak]]

        ## Images

        Three idioms cover the typical image needs:

        - `![alt](url)` — natural pixel size at the theme's print DPI
          (150 by default), capped at the column width.
        - `[[image src=... width=... align=... caption=... link=...]]`
          — explicit dimensions, alignment, caption, clickable link.
        - `[[imagepair src1=... src2=... caption1=... caption2=...]]`
          — two images side by side in equal half-column cells, with
          optional captions and dividers. Useful when a single
          full-column image would dominate the page.

        Example pair:

        [[imagepair
            src1="{LOGO}" caption1="Shortcode with caption + link"
            src2="{LOGO}" caption2="Same image, second cell"
            borders=true
        ]]

        ### Single image

        The plain `[[image]]` shortcode standalone — explicit width,
        centred, with a caption and a clickable link:

        [[image src="{LOGO}" width=30 align=center caption="The egoPdf wordmark, 30 mm wide and centered" link="https://github.com/marcoregueira/egopdf"]]
        """;
}
