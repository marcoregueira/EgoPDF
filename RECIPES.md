# egoPdf Recipes

> Coded PDFs, perfect on paper. This file is the agent-facing cheat sheet:
> every pattern below appears in at least one shipping sample, so an agent
> can grep the recipe name and copy the matching block of code.

## How to read this file

Each recipe has three parts:

1. **When** — a one-line problem statement.
2. **Code** — the smallest copy-pasteable snippet that solves it.
3. **See also** — the sample file where the same idea is used end-to-end.

The samples themselves (`Ego.PDF.Samples/Sample*.cs`,
`Ego.PDF.Samples/SamplePhotovoltaic.cs`,
`Ego.PDF.Samples/SampleMarkdown.cs`,
`Ego.PDF.Samples/SampleZebra.cs`) are the source of truth — if a recipe
ever disagrees with them, the samples win.

---

## Coordinate system in 10 seconds

- Origin is the **top-left corner of the page**. X grows right, Y grows down.
- All distances are in **millimetres** by default (set via `new FPdf("P", UnitEnum.mm, ...)`).
- The cursor (`pdf.X`, `pdf.Y`) is moved by `Cell`, `MultiCell`, `Write`, `Ln`,
  `SetXY`, `SetX`, `SetY` and the page-break logic. Drawing primitives
  (`Rect`, `Line`, `Image`) take explicit coordinates and **don't** move it.
- Page margins (`SetMargins`, `SetTopMargin`) define the printable area
  returned by `pdf.Bounds()`.

---

## Recipe index

- [Layout primitives](#layout-primitives) — `Bounds`, `Row`, `Stack`, `Panel`, `Rect.Inset`
- [State scopes](#state-scopes) — `PushState`, `PushPos`
- [Text measurement and auto-growing rows](#text-measurement)
- [Coloured header band with brand wordmark](#header-band)
- [Identification grid with auto-grow](#identification-grid)
- [Aligned cells, badges and OK / NOK indicators](#badges)
- [Compliance checklist with custom checkbox glyph](#checklist)
- [Image with placeholder fallback](#image-fallback)
- [Barcodes and 2D codes](#barcodes)
- [Markdown to PDF](#markdown)

---

## <a id="layout-primitives"></a> Layout primitives

### When you need columns

Avoid `x + w + gap` arithmetic — `Row()` gives you the slot rects directly.

```csharp
var slots = pdf.Row(pdf.Bounds(), 3, gap: 3);
// slots[0], slots[1], slots[2] are equal-width Rects with 3 mm between.

// Non-equal weights:
var slots2 = pdf.Row(pdf.Bounds(), new[] { 2.0, 1.0 }, gap: 0);
// 2/3 of the width on the left, 1/3 on the right, no gap.
```

### When you need stacked rows

`Stack()` is the vertical sibling of `Row()`.

```csharp
var bodyAndFooter = pdf.Stack(pdf.Bounds(), new[] { 4.0, 1.0 }, gap: 4);
// bodyAndFooter[0] gets 80% of the height, [1] gets 20%.

var equalRows = pdf.Stack(content, count: 6);
// content split into 6 equal-height rows, no gap.
```

### When you need a titled box

`Panel(bounds, title, body)` paints the frame and title using the
currently-set draw / fill / font / text colours, hands you the inner
content rect, and restores all state on return.

```csharp
pdf.SetFillColor(panelFill);
pdf.SetDrawColor(lineGray);
pdf.SetLineWidth(0.2);
pdf.SetFont("Helvetica", "B", 8);
pdf.SetTextColor(brandAccent);

pdf.Panel(slot, "MEDIDAS DC", content =>
{
    var rows = pdf.Stack(content, 6);
    foreach (var r in rows) { /* render in r */ }
});
// fonts and colours are exactly as they were before the call.
```

Untitled framed box: `pdf.Panel(bounds, content => ...)`.

### When you have a Rect and want to shrink it

```csharp
var inner = bounds.Inset(3);            // 3 mm on every side
var insets = bounds.Inset(3, 2);        // 3 mm horizontal, 2 mm vertical
var custom = bounds.Inset(3, 8, 3, 3);  // L, T, R, B (clamped to >= 0)
```

`Inset` is non-mutating: every call returns a new `Rect`.

**See also**: `Ego.PDF.Samples/SamplePhotovoltaic.cs::DrawThreePanels`.

---

## <a id="state-scopes"></a> State scopes

### When you want to change font/colours inside a block without leaking

```csharp
pdf.SetFont("Helvetica", "", 11);
pdf.SetTextColor(Color.Black);

using (pdf.PushState())
{
    pdf.SetFont("Helvetica", "B", 7);
    pdf.SetTextColor(accent);
    pdf.Cell(40, 4, "LABEL");
}
// back to Helvetica regular 11, black text -- no manual SetFont/SetTextColor.
```

`PushState()` snapshots **font family / style / size**, **text / fill /
draw colours**, and **line width**. It does **not** touch the cursor
(`X`, `Y`) -- use `PushPos()` for that.

### When you want to render somewhere and snap back

```csharp
using (pdf.PushPos())
{
    pdf.SetXY(120, 50);
    pdf.Image(file, pdf.X, pdf.Y, 30, 0);
}   // cursor restored even if the body throws.
```

`PushPos(GoBackMode)` lets you restore only the X or only the Y if you
need to advance vertically but stay in the same column (`GoBackMode.X`
restores X only; `.Y` restores Y only).

`PushPos` and `PushState` nest freely and combine well:

```csharp
using (pdf.PushState())
using (pdf.PushPos())
{
    // wild changes here are all reverted on exit.
}
```

**See also**: `Ego.PDF.Samples/SamplePhotovoltaic.cs::DrawIdentificationBlock`,
`Ego.PDF.Samples/SamplePhotovoltaic.cs::DrawPanel`.

---

## <a id="text-measurement"></a> Text measurement and auto-growing rows

`CellMeasure(width, lineHeight, text)` returns the height a `MultiCell`
would consume if rendered with the **currently configured font**.
Divide by `lineHeight` to get a line count, then size your row so the
longest cell fits.

```csharp
pdf.SetFont("Helvetica", "", 9);
const double valueLineH = 3.8;

int[] lineCounts = pairs
    .Select(p => Math.Max(1, (int)Math.Round(pdf.CellMeasure(contentW, valueLineH, p.Value) / valueLineH)))
    .ToArray();

int rowMaxLines = lineCounts.Max();
double rowH = topPad + labelH + rowMaxLines * valueLineH + bottomPad;
```

Then draw `MultiCell(contentW, valueLineH, value)` inside the grown row.

**Gotcha**: the font (family + style + size) must be the **same** at
measurement time and at render time, otherwise the line count is wrong.

`GetStringWidth` is the single-line equivalent and is what
`Cell(..., AlignEnum.Right)` uses internally. It now falls back to
the `"A"` width for any glyph the font doesn't know (em-dash, Greek,
etc.) rather than throwing `KeyNotFoundException`.

**See also**: `Ego.PDF.Samples/SamplePhotovoltaic.cs::DrawIdentificationBlock`.

---

## <a id="header-band"></a> Coloured header band with brand wordmark

A full-width band at `Y = 0` with the egoPdf wordmark on the left and a
right-aligned title is the egoPdf calling card. Both invoice samples
and the PV report use it.

```csharp
SetFillColor(brandDark);
Rect(0, 0, W, 32, "F");   // band fills the full page width

SetFont("Helvetica", "B", 22);
SetXY(LeftMargin, 11);
SetTextColor(Color.White);
Cell(GetStringWidth("ego"), 11, "ego");
SetTextColor(brandAccent);
Cell(GetStringWidth("Pdf"), 11, "Pdf");

SetFont("Helvetica", "B", 16);
SetTextColor(Color.White);
SetXY(LeftMargin, 9);
Cell(W - LeftMargin - RightMargin, 9, title, "0", 0, AlignEnum.Right);

SetXY(LeftMargin, 32 + 6);   // move cursor below the band
SetTextColor(brandDark);
```

Two-colour wordmark trick: each `Cell` uses `GetStringWidth` so the
two halves butt up against each other with no extra margin.

**See also**:
- `Ego.PDF.Samples/Sample8.cs` (invoice header)
- `Ego.PDF.Samples/SamplePhotovoltaic.cs::DrawHeaderBand`
- `Ego.PDF.Samples/Sample9.cs` (barcode catalogue header)

---

## <a id="identification-grid"></a> Identification grid with auto-grow

A 2x4 (or NxM) grid of label / value cells, each cell sized to its
content. Used for "the metadata block at the top of the form".

Steps:

1. Measure every value with `CellMeasure` at the value font.
2. For each row, take the max line count and compute the row height.
3. `Stack()` the rows, `Row()` each into N cells.
4. For each cell: small coral label on top, value below as `MultiCell`.

```csharp
SetFont("Helvetica", "", 9);
var rowHs = ComputeRowHeights(pairs);     // see CellMeasure recipe

using (PushState())
{
    SetDrawColor(lineGray);
    SetLineWidth(0.2);
    Rect(LeftMargin, Y, w, rowHs.Sum(), "D");

    // ... cell rendering with PushState() per cell if you want isolation.
}
```

The shipping `DrawIdentificationBlock` does this longhand; the same
idea written with the layout primitives would be:

```csharp
var bounds  = new Rect(LeftMargin, Y, w, rowHs.Sum());
var rowRects = Stack(bounds, rowHs);
for (int r = 0; r < rowRects.Length; r++)
    foreach (var cell in Row(rowRects[r], 4))
        RenderLabelValue(cell, pairs[r * 4 + ...]);
```

**See also**: `Ego.PDF.Samples/SamplePhotovoltaic.cs::DrawIdentificationBlock`.

---

## <a id="badges"></a> Aligned cells, badges and OK / NOK indicators

A coloured pill inside a table cell signals pass / fail at a glance.

```csharp
// In the row loop:
var badgeX = pdf.X;
var badgeY = pdf.Y;
pdf.SetFillColor(item.Ok ? okGreen : brandAccent);
pdf.Rect(badgeX + cellW * 0.2, badgeY + 1.2, cellW * 0.6, rowH - 2.4, "F");

pdf.SetXY(badgeX, badgeY);
pdf.SetTextColor(Color.White);
pdf.SetFont("Helvetica", "B", 7);
pdf.Cell(cellW, rowH, item.Ok ? "OK" : "NOK", "B", 1, AlignEnum.Center);
```

**Right-aligning** any cell: pass `AlignEnum.Right` as the last
positional argument of `Cell`. Text width is computed from the current
font via `GetStringWidth`, so make sure you set the font **before**
calling `Cell` with right / center alignment.

**See also**: `Ego.PDF.Samples/SamplePhotovoltaic.cs::DrawStringsTable`.

---

## <a id="checklist"></a> Compliance checklist with custom checkbox glyph

The default core fonts have no checkbox glyph, so we draw it ourselves:
a small filled square with a coral V-shape inside drawn with two
`Line()` calls.

```csharp
var boxSize = 3.2;
SetDrawColor(brandDark);
SetLineWidth(0.25);
SetFillColor(item.Pass ? brandDark : Color.White);
Rect(x, y + (h - boxSize) / 2, boxSize, boxSize, item.Pass ? "DF" : "D");

if (item.Pass)
{
    SetDrawColor(brandAccent);
    SetLineWidth(0.5);
    Line(x + 0.6, y + h / 2,       x + 1.4, y + h / 2 + 0.9);
    Line(x + 1.4, y + h / 2 + 0.9, x + 2.7, y + h / 2 - 1.0);
    SetLineWidth(0.25);
}
```

**See also**: `Ego.PDF.Samples/SamplePhotovoltaic.cs::DrawCheckedItem`.

---

## <a id="image-fallback"></a> Image with placeholder fallback

When the asset might be missing (during a review pass, or in a
template), drop a labelled placeholder instead of crashing:

```csharp
if (File.Exists(path))
{
    try { Image(path, x, y, w, h); return; }
    catch { /* drop through */ }
}

SetFillColor(lightFill);
SetDrawColor(lineGray);
Rect(x, y, w, h, "DF");

SetFont("Helvetica", "I", 9);
SetTextColor(textMuted);
SetXY(x, y + h / 2 - 3);
Cell(w, 4, $"[ {fileName} pending ]", "0", 0, AlignEnum.Center);
```

**See also**: `Ego.PDF.Samples/SamplePhotovoltaic.cs::DrawImageOrPlaceholder`.

---

## <a id="barcodes"></a> Barcodes and 2D codes

`Ego.PDF.Barcodes.BarcodeRenderer` exposes one method per symbology.
All of them take user-unit (mm) coordinates and a tint colour.

```csharp
using Ego.PDF.Barcodes;

BarcodeRenderer.DrawQrCode(pdf, "https://example.com", x: 20, y: 60, size: 30, color);
BarcodeRenderer.DrawDataMatrix(pdf, "PO-12345", x: 60, y: 60, size: 25, color);
BarcodeRenderer.DrawPdf417(pdf, "long payload...", x: 100, y: 60, w: 60, h: 20, color);
BarcodeRenderer.DrawCode128(pdf, "ABC123", x: 20, y: 100, moduleWidth: 0.5, height: 14, color);
```

For Markdown:

```
[[barcode type=qr size=30 color=#1a1d26]] payload [[/barcode]]
```

is wired up by `Ego.PDF.Samples/BarcodeShortcode.cs`.

**See also**:
- `Ego.PDF.Samples/Sample9.cs` (catalogue of every symbology)
- `Ego.PDF.Samples/SampleZebra.cs` (ZPL barcode commands routed through ZXing)

---

## <a id="markdown"></a> Markdown to PDF

`EgoPDF.Markdown` parses CommonMark + GFM and emits PDF using the core
`FPdf` API. Set up the theme, register custom shortcodes, render.

```csharp
using Ego.PDF.Markdown;

var pdf = new FPdf();
pdf.AddPage(PageSizeEnum.A4);

var theme = MarkdownTheme.Default;
theme.Shortcodes.Register("barcode", new BarcodeShortcode());
theme.Shortcodes.Register("cta",     new CallToActionShortcode());

MarkdownRenderer.Render(pdf, markdownText, theme);
pdf.Close();
```

Built-in shortcodes (auto-registered): `[[pagebreak]]`, `[[image]]`.

The image-shortcode supports `src`, `width`, `align`, `caption`, `link`.
Plain Markdown images (`![alt](url)`) auto-size from the file's pixel
dimensions through `theme.ImageDpi` (default 150).

**See also**: `Ego.PDF.Samples/SampleMarkdown.cs`.

---

## When you need a primitive that isn't here

The library is intentionally small. Before adding a new helper, check:

1. **Can `Row` / `Stack` / `Panel` already express it?** Most "this code
   feels repetitive" moments are positional arithmetic in disguise.
2. **Can `PushState` / `PushPos` clean up the state machine?** If your
   problem is "I keep forgetting to restore the font," wrap the block.
3. **Is the missing piece a measurement?** `CellMeasure` and
   `GetStringWidth` cover most cases; the rest you can derive from
   `FontSizePt`.

If none of the above fit, copy the closest sample and extend it. The
samples are the spec.
