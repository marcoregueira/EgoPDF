using Ego.PDF.Data;
using Ego.PDF.Markdown;
using System.IO;

namespace Ego.PDF.Samples;

/// <summary>
/// Demonstrates the EgoPDF.Markdown package: parses an embedded
/// Markdown string and emits a single-page PDF. Uses the default
/// theme (FPDF core fonts) so the sample runs without loading any
/// TTF files.
/// </summary>
public class SampleMarkdown : FPdf
{
    private SampleMarkdown(string file) : base(file) { }

    public static Stream GetSample(string file)
    {
        using var pdf = new SampleMarkdown(file);
        pdf.AddPage(PageSizeEnum.A4);

        var theme = MarkdownTheme.Default;
        // [[pagebreak]] is registered automatically by ShortcodeRegistry;
        // only the package-specific handlers need wiring here.
        theme.Shortcodes.Register("barcode", new BarcodeShortcode());
        theme.Shortcodes.Register("cta", new CallToActionShortcode());

        MarkdownRenderer.Render(pdf, Document, theme);
        pdf.Close();
        return pdf.Buffer.BaseStream;
    }

    private const string Document = """
        # Markdown to PDF

        This document is rendered by **EgoPDF.Markdown** using the *default*
        theme (no fonts to load). It exercises the Phase-1 happy path:
        headings, paragraphs, lists, code blocks, links and rules.

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

        ## Quote

        > Markdown is a lightweight markup language with plain-text
        > formatting syntax. Its design allows it to be converted to
        > many output formats, including PDF.

        [[pagebreak]]

        ## Shortcodes

        EgoPDF.Markdown ships with a tiny shortcode extension: any line
        wrapped in `[[ ... ]]` becomes a custom block dispatched to a
        registered handler. The Samples package wires a `barcode`
        shortcode on top of EgoPDF.Barcodes.

        [[barcode type=qr data="https://github.com/marcoregueira/egopdf" size=30 align=center]]

        Or a 1D Code 128 across the column:

        [[barcode type=code128 data="EGOPDF-128" width=0.5 height=12]]

        Shortcodes are not limited to barcodes — anything you can draw
        on the page is fair game. The Samples package also ships a
        `cta` shortcode that drops a clickable button anywhere in the
        flow:

        [[cta text="Star egoPdf on GitHub" url="https://github.com/marcoregueira/egopdf"]]

        ---

        That's the whole happy path. Tables, footnotes and inline images
        are scheduled for later previews.
        """;
}
