# EgoPDF.Markdown

Render Markdown documents to PDF on top of
[EgoPDF.Generator](https://www.nuget.org/packages/EgoPDF.Generator) and
[Markdig](https://github.com/xoofx/markdig).

> **Preview.** Phase-1 happy path. Tables, blockquote styling polish,
> footnotes, task lists and inline images are planned for later
> releases.

## Quick start

```csharp
using Ego.PDF;
using Ego.PDF.Data;
using Ego.PDF.Markdown;

using var pdf = new FPdf("readme.pdf");
pdf.AddPage(PageSizeEnum.A4);

MarkdownRenderer.RenderFile(pdf, "README.md");
pdf.Close();
```

Or render an inline string:

```csharp
MarkdownRenderer.Render(pdf, "# Hello\n\nThis is **bold** Markdown.");
```

## What works in this preview

- Headings H1-H6 (sized hierarchy + accent colour from the theme).
- Paragraphs with inline emphasis: `**bold**`, `*italic*`,
  `` `code` ``, `[link](url)`, `<https://autolink>`, hard/soft
  line breaks.
- Unordered and ordered lists, including nested lists.
- Fenced (` ``` `) and indented code blocks, with a tinted background.
- Horizontal rules (`---`).
- Block-level local images (`![alt](path.png)` on its own line). Remote
  URLs and inline images fall back to a muted `[alt]` placeholder.
- Block quotes (`> ...`) with a left accent rule.

## Theming

Pass a `MarkdownTheme` to override fonts, sizes, colours and spacing:

```csharp
var theme = new MarkdownTheme
{
    BodyFont = "Helvetica",
    HeadingFont = "Helvetica",
    CodeFont = "Courier",
    AccentColor = new Microsoft.Xna.Framework.Color(40, 116, 166),
};
MarkdownRenderer.Render(pdf, markdown, theme);
```

Two built-in themes:

- `MarkdownTheme.Default` (FPDF core fonts only — no setup).
- `MarkdownTheme.EgoPdf` (Poppins + Roboto + Roboto Mono — load them
  with `pdf.LoadFont` / `pdf.AddFont` before rendering).

## Targets

- net8.0
- net9.0

## License

MIT.
