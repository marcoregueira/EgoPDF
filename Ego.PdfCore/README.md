# EgoPDF.Generator

C# port of [FPDF](http://www.fpdf.org) with bug fixes and modern additions
(SkiaSharp-driven font metrics, embedded TrueType fonts with valid PDF
descriptors, page-size and image helpers).

If you also need to render Zebra ZPL labels to PDF, add the companion
preview package [`EgoPDF.Zpl`](https://www.nuget.org/packages/EgoPDF.Zpl).

## Quick start

```csharp
using Ego.PDF;
using Ego.PDF.Data;

using var pdf = new FPdf("hello.pdf");
pdf.AddPage(PageSizeEnum.A4);
pdf.SetFont("Helvetica", "B", 24);
pdf.Cell(0, 10, "Hello from EgoPDF!");
pdf.Close();
```

That writes a one-page A4 PDF with the text in Helvetica-Bold. The PDF
core fonts (Helvetica, Courier, Times) are never embedded; load a TTF
with `pdf.LoadFont("RobotoSlab", "path/to/RobotoSlab-Regular.ttf");`
and `pdf.AddFont("RobotoSlab", "");` to embed a custom face.

## What's in the box

- The full FPDF API (text, lines, rectangles, cells, multi-cell, images,
  links, font scale, page sizes / orientations).
- SkiaSharp-based metrics for embedded TrueType fonts — no .php tool
  generation step needed, just hand `LoadFont` a `.ttf` path.
- Embedded fonts emit valid PDF descriptors (PostScript-safe `BaseFont`
  names, real `FontBBox`, non-zero `StemV`, italic / bold / monospace
  flags) so Acrobat in strict mode accepts them.
- Image embedding for PNG / JPEG / GIF, with on-the-fly format detection
  via Skia codecs.

## Targets

- net8.0
- net9.0

## License

MIT. See the `NOTICE` file shipped with the package for the FPDF
acknowledgment — the substantial portions inherited from the PHP
project's automated port are reused under FPDF's permissive notice,
the rest is original work under MIT.
