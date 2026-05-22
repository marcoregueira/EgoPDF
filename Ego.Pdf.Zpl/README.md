# EgoPDF.Zpl

Render Zebra Programming Language (ZPL II) labels to PDF using
[EgoPDF.Generator](https://www.nuget.org/packages/EgoPDF.Generator) and
[ZXing.Net](https://www.nuget.org/packages/ZXing.Net).

> **Preview.** The API is in flux. Bug reports and PRs welcome; expect
> breaking changes between preview versions.

## Quick start

```csharp
using Ego.PDF;
using Ego.PDF.Data;
using Ego.Pdf.Zpl;

using var pdf = new FPdf("label.pdf");
pdf.SetUnitConverionFactor(UnitEnum.Point, 203);

// Optional: load a TTF for the monospace ZPL fonts (^A?, ^CFA).
// Without this, ^A? falls back to whatever font is currently set.
pdf.LoadFont("robotomonob", "path/to/RobotoMono-Bold.ttf");
pdf.AddFont("robotomonob", "");
pdf.SetFont("helvetica", "B", 16);

var zpl = new PdfZpl(pdf, dpi: 203);
zpl.SetLabelSize(812, 1218);           // 4" x 6" at 203 dpi
zpl.SetVariableFont("helvetica", "B"); // mapped to ZPL font "0"
zpl.SetMonospaceFont("robotomonob");   // mapped to ZPL fonts "A".."V"

zpl.Print(@"
^XA
^FO50,50^FDHello from EgoPDF.Zpl!^FS
^FO50,200^BCN,80,N,N,N^FD12345678^FS
^XZ
");

pdf.Close();
```

## Supported ZPL commands

### Layout

`^XA` / `^XZ`, `^FO`, `^FT`, `^FD`, `^FS`, `^FX`, `^FH`, `^FW`, `^FR`,
`^FB`, `^FN`, `^CF`, `^CI`, `^LH`, `^LL`, `^PW`,
`^DF` + `^XF` (template + field substitution),
`^A?` (A-V, plus the proportional `^A0`).

### Graphics

`^GB`, `^GC`, `^GE`, `^GD`,
`^GF` (inline ASCII bitmap with the ZPL RLE compression),
`^XG` (recall a host-registered graphic via
`PdfZpl.RegisterGraphic(name, filePath)`).

### Barcodes

| ZPL command | Symbology               |
| ----------- | ----------------------- |
| `^BC`       | Code 128                |
| `^B3`       | Code 39                 |
| `^B2`       | Interleaved 2 of 5      |
| `^BK`       | Codabar                 |
| `^BE`       | EAN-13                  |
| `^B8`       | EAN-8                   |
| `^BU`       | UPC-A                   |
| `^B9`       | UPC-E                   |
| `^BM`       | MSI                     |
| `^B7`       | PDF417                  |
| `^BQ`       | QR Code                 |
| `^BX`       | Data Matrix             |
| `^BO`       | Aztec                   |

All 1D symbologies honour orientation `N` (no rotation) and `B`
(rotated 90° CCW). 2D symbologies honour `N`, `B`, `R`, `I`.

## What it doesn't do

- Print to a Zebra device. Output is PDF, not ZPL → ZPL.
- Implement the full ZPL command surface. Printer-state commands
  (`^MD`, `^PR`, `^JM`, `^IL`, `^IS`, `^DG`, `^DY`, `^DU`, `^LR`,
  `^LS`, `^PM`, `^PO`, `^PF`) are silently accepted as no-ops.
- Reproduce Zebra's bitmap fonts byte-for-byte — the package uses
  whatever TTF the host registers, so visible metrics differ slightly
  from what a real printer would emit. Pre-pick a label width to match
  the physical roll if you care about absolute sizing.

## Targets

- net8.0
- net9.0

## License

MIT. "ZPL" and "Zebra" are trademarks of Zebra Technologies; this
package is not affiliated with or endorsed by them.
