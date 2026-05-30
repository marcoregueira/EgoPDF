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
        MarkdownRenderer.Render(pdf, Document);
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

        ---

        That's the whole happy path. Tables, footnotes and inline images
        are scheduled for later previews.
        """;
}
