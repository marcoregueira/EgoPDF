# EgoPDF

PDF generator for .NET, originally an automated port of
[FPDF](http://www.fpdf.org) from PHP with hand-tuned fixes on top.
Two NuGet packages live in this repo:

| Package | Purpose | Status |
| --- | --- | --- |
| [`EgoPDF.Generator`](https://www.nuget.org/packages/EgoPDF.Generator) | Core FPDF API plus SkiaSharp-driven font embedding | Stable |
| [`EgoPDF.Barcodes`](https://www.nuget.org/packages/EgoPDF.Barcodes) | Render Zebra ZPL II labels to PDF on top of the generator | Preview |

Both packages target `net8.0` and `net9.0` and ship under the MIT
license. `EgoPDF.Generator` additionally carries a `NOTICE` file
acknowledging FPDF, the PHP project it was originally automated from.

## What's here

```
Ego.PdfCore/          → EgoPDF.Generator package source
Ego.PDF.Barcodes.Zpl/          → EgoPDF.Barcodes package source
Ego.PDF.Samples/      → reusable sample code consumed by the test harness and WebDemo
Ego.Pdf.Test/         → xUnit tests; the DoSample*/DoZebra* facts emit PDFs next to the test binary for human review
WebDemo/              → ASP.NET Core demo site that exposes the same samples over HTTP
```

## Quick start

### Plain PDF

```csharp
using Ego.PDF;
using Ego.PDF.Data;

using var pdf = new FPdf("hello.pdf");
pdf.AddPage(PageSizeEnum.A4);
pdf.SetFont("Helvetica", "B", 24);
pdf.Cell(0, 10, "Hello from EgoPDF!");
pdf.Close();
```

### ZPL → PDF

```csharp
using Ego.PDF;
using Ego.PDF.Data;
using Ego.PDF.Barcodes.Zpl;

using var pdf = new FPdf("label.pdf");
pdf.SetUnitConverionFactor(UnitEnum.Point, 203);
pdf.LoadFont("robotomonob", "RobotoMono-Bold.ttf");
pdf.AddFont("robotomonob", "");
pdf.SetFont("helvetica", "B", 16);

var zpl = new PdfZpl(pdf, dpi: 203);
zpl.SetLabelSize(812, 1218);            // 4" x 6" at 203 dpi
zpl.SetVariableFont("helvetica", "B");
zpl.SetMonospaceFont("robotomonob");

zpl.Print(@"
^XA
^FO50,50^FDHello from EgoPDF.Barcodes!^FS
^FO50,200^BCN,80,N,N,N^FD12345678^FS
^XZ
");

pdf.Close();
```

Per-package READMEs (with the full list of supported ZPL commands and a
longer feature list for the generator) live next to each project.

## Building locally

```bash
dotnet build PDF.sln
dotnet test Ego.Pdf.Test/Ego.Pdf.Test.csproj
dotnet pack Ego.PdfCore/Ego.PdfCore.csproj  -c Release -o artifacts
dotnet pack Ego.PDF.Barcodes/Ego.PDF.Barcodes.csproj  -c Release -o artifacts
```

The Zebra `[Fact]`s in the test project don't assert anything — they
drop their PDF output beside the binary so a human can eyeball the
result against a Labelary reference render.

## Publishing to NuGet

`publish.ps1` wraps `dotnet pack` + `dotnet nuget push` for both
packages. The API key reads from `$env:NUGET_API_KEY` by default, or
from `-ApiKey`:

```powershell
# Verify both packages build (no push)
./publish.ps1 -PackOnly

# Publish both packages to nuget.org
$env:NUGET_API_KEY = 'oy2...'
./publish.ps1

# Publish only one package
./publish.ps1 -Generator
./publish.ps1 -Zpl

# Idempotent re-runs: skip versions that already exist on the feed
./publish.ps1 -SkipDuplicate
```

Bump the `<Version>` in each csproj before running. NuGet rejects
re-publishing the same version unless `-SkipDuplicate` is set.

## License

[MIT](LICENSE). See [NOTICE](NOTICE) for the FPDF acknowledgment that
ships with `EgoPDF.Generator`. "Zebra" and "ZPL" are trademarks of
Zebra Technologies; this project is not affiliated with or endorsed by
them.
