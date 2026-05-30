using Ego.PDF.Barcodes;
using Ego.PDF.Data;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;

namespace Ego.PDF.Samples;

/// <summary>
/// Acceptance / commissioning report for a residential solar
/// photovoltaic installation. Mirrors the structure of an industrial
/// inspection sheet (header, identification block, three side-by-side
/// technical panels, single-line schematic, strings measurement
/// table, compliance checklist, observations, signatures, plus two
/// annex pages for thermography and photo report).
///
/// Photos are referenced by filename and resolved against the
/// runtime base directory. When a file is missing the sample falls
/// back to a labelled placeholder rectangle, so the layout can be
/// reviewed before the final shots are delivered.
/// </summary>
public class SamplePhotovoltaic : FPdf
{
    // ---- Brand palette -----------------------------------------------------

    private static readonly Color BrandDark   = EgoPdfBrand.Dark;
    private static readonly Color BrandAccent = EgoPdfBrand.Accent;
    private static readonly Color TextMuted   = EgoPdfBrand.Muted;
    private static readonly Color LineGray    = EgoPdfBrand.HairLine;
    private static readonly Color BandSubText = EgoPdfBrand.SubText;
    // PV-specific tints stay local: only this sample uses them.
    private static readonly Color LightFill   = new Color(247, 238, 236);
    private static readonly Color PanelFill   = new Color(248, 248, 250);
    private static readonly Color OkGreen     = new Color(70, 145, 110);

    // Centralised look-and-feel for the technical panels. Declared once
    // so a new panel in the report doesn't have to re-derive five
    // SetFill / SetDraw / SetFont calls.
    private static readonly PanelStyle BrandPanel = new()
    {
        FillColor   = PanelFill,
        BorderColor = LineGray,
        TitleColor  = BrandAccent,
        LineWidth   = 0.2,
    };

    private string _assetsPath = string.Empty;

    public static Stream GetSample(string filePath, string path)
    {
        using var pdf = new SamplePhotovoltaic(filePath);
        pdf._assetsPath = path ?? AppDomain.CurrentDomain.BaseDirectory;
        pdf.SetMargins(12, 10, 12);
        pdf.SetAutoPageBreak(false, 0);
        // Brand wordmark uses Poppins Extra Light to match the project logo.
        EgoPdfBrand.LoadPoppins(pdf);
        pdf.RenderAll(BuildSampleInstallation());
        pdf.Close();
        return pdf.Buffer.BaseStream;
    }

    private SamplePhotovoltaic(string filePath) : base(filePath) { }

    // ---- Top-level flow ----------------------------------------------------

    private void RenderAll(Installation inst)
    {
        AddPage(PageSizeEnum.A4);
        DrawHeaderBand("ACTA DE RECEPCIÓN", "Instalación fotovoltaica de autoconsumo", inst);
        DrawIdentificationBlock(inst);
        DrawThreePanels(inst);
        DrawUnifilarSchematic(inst);
        DrawStringsTable(inst);
        DrawComplianceChecks(inst);
        DrawObservations(inst.Observations);
        DrawSignatures(inst);

        if (inst.Thermography.Any())
        {
            AddPage(PageSizeEnum.A4);
            DrawHeaderBand("ANEXO 1 - TERMOGRAFÍA", "Inspección IR de strings DC y cuadros", inst);
            DrawThermographyGrid(inst);
        }

        if (inst.Photos.Any())
        {
            AddPage(PageSizeEnum.A4);
            DrawHeaderBand("ANEXO 2 - REPORTAJE FOTOGRÁFICO", "Documentación de inspección", inst);
            DrawPhotoGallery(inst);
        }
    }

    // ---- Header band -------------------------------------------------------

    private const double BandHeight = 32;

    private void DrawHeaderBand(string title, string subtitle, Installation inst)
    {
        SetFillColor(BrandDark);
        Rect(0, 0, W, BandHeight, "F");

        // Left: two-tone wordmark in thin Poppins (matches the logo).
        EgoPdfBrand.DrawWordmark(this, x: LeftMargin, y: 11, sizePt: 22,
            egoColor: Color.White, pdfColor: BrandAccent, cellHeight: 11);

        // Right: title + subtitle stacked.
        SetFont("Helvetica", "B", 16);
        SetTextColor(Color.White);
        SetXY(LeftMargin, 9);
        Cell(W - LeftMargin - RightMargin, 9, title, "0", 0, AlignEnum.Right);

        SetFont("Helvetica", "I", 9);
        SetTextColor(BandSubText);
        SetXY(LeftMargin, 19);
        Cell(W - LeftMargin - RightMargin, 5, subtitle, "0", 0, AlignEnum.Right);

        SetFont("Helvetica", "", 8);
        SetXY(LeftMargin, 25);
        Cell(W - LeftMargin - RightMargin, 4,
            $"{inst.ActNumber}  ·  {inst.ActDate}", "0", 0, AlignEnum.Right);

        SetXY(LeftMargin, BandHeight + 6);
        SetTextColor(BrandDark);
    }

    // ---- Identification block ---------------------------------------------

    private void DrawIdentificationBlock(Installation inst)
    {
        const double topPad     = 1.5;
        const double labelH     = 3.0;
        const double valueLineH = 3.8;
        const double bottomPad  = 1.5;
        const double cellInsetX = 1.5;
        const double minRowH    = 8.0;
        const int    cols       = 4;

        var pairs = new (string label, string value)[]
        {
            ("TITULAR",        inst.Owner),
            ("CUPS",           inst.Cups),
            ("DIRECCIÓN",      inst.Address),
            ("POTENCIA",       $"{inst.PeakPowerKwp:0.0##} kWp ({inst.Panels} paneles)"),
            ("INSTALADOR",     inst.InstallerCompany),
            ("REGISTRO",       inst.InstallerRegistration),
            ("INVERSOR",       $"{inst.Inverter.Brand} {inst.Inverter.Model}"),
            ("Nº SERIE INV.",  inst.Inverter.Serial),
        };

        using (PushState())
        {
            // 1. Measure with the value font; auto-grow the row to the
            // worst case so no cell clips.
            SetFont("Helvetica", "", 9);
            var w = W - LeftMargin - RightMargin;
            var contentW = w / cols - 2 * cellInsetX;

            double RowHeight(int from)
            {
                int worst = 1;
                for (int i = from; i < from + cols; i++)
                {
                    var h = CellMeasure(contentW, valueLineH, pairs[i].value);
                    worst = Math.Max(worst, (int)Math.Round(h / valueLineH));
                }
                return Math.Max(minRowH, topPad + labelH + worst * valueLineH + bottomPad);
            }

            var rowHeights = new[] { RowHeight(0), RowHeight(cols) };

            // 2. Compose the grid with Stack + Row; no x/y/i%cols math.
            var bounds = new Rect(LeftMargin, Y, w, rowHeights[0] + rowHeights[1]);
            var rows = Stack(bounds, rowHeights);

            // 3. Frame + internal dividers.
            SetDrawColor(LineGray);
            SetLineWidth(0.2);
            Rect(bounds.X, bounds.Y, bounds.W, bounds.H, "D");
            Line(bounds.X, rows[1].Y, bounds.Right, rows[1].Y);
            var colW = bounds.W / cols;
            for (int i = 1; i < cols; i++)
                Line(bounds.X + i * colW, bounds.Y, bounds.X + i * colW, bounds.Bottom);

            // 4. Render each cell inside its inset rect.
            for (int r = 0; r < rows.Length; r++)
            {
                var cellRects = Row(rows[r], cols);
                for (int c = 0; c < cols; c++)
                {
                    var cell = cellRects[c].Inset(cellInsetX, topPad, cellInsetX, bottomPad);
                    var (label, value) = pairs[r * cols + c];

                    SetXY(cell.X, cell.Y);
                    SetFont("Helvetica", "B", 7);
                    SetTextColor(BrandAccent);
                    Cell(cell.W, labelH, label);

                    SetXY(cell.X, cell.Y + labelH);
                    SetFont("Helvetica", "", 9);
                    SetTextColor(BrandDark);
                    MultiCell(cell.W, valueLineH, value, "", AlignEnum.Left, false);
                }
            }

            Y = bounds.Bottom + 4;
        }
    }

    // ---- Three technical panels (DC / AC / Protections) -------------------

    private void DrawThreePanels(Installation inst)
    {
        // Row() splits the printable width into three equal slots with
        // a 3 mm gap between them, returning their Rect coordinates.
        // No manual (x + w + gap) arithmetic, no off-by-one when the
        // count changes.
        var rowBounds = new Rect(LeftMargin, Y, W - LeftMargin - RightMargin, 54.0);
        var slots = Row(rowBounds, 3, gap: 3);

        DrawPanel(slots[0], "MEDIDAS DC", new (string, string)[]
        {
            ("Voc string 1",   $"{inst.Strings[0].Voc:0.0} V"),
            ("Isc string 1",   $"{inst.Strings[0].Isc:0.0} A"),
            ("Vmpp string 1",  $"{inst.Strings[0].Vmpp:0.0} V"),
            ("Voc string 2",   $"{inst.Strings[1].Voc:0.0} V"),
            ("Isc string 2",   $"{inst.Strings[1].Isc:0.0} A"),
            ("Aislamiento +/-", $"{inst.Strings.Min(s => s.Rais):0.#} MOhm"),
        });

        DrawPanel(slots[1], "MEDIDAS AC", new (string, string)[]
        {
            ("Tensión L-N",     $"{inst.AcVoltage:0.0} V"),
            ("Frecuencia",      $"{inst.AcFrequency:0.00} Hz"),
            ("Secuencia fases", "Correcta"),
            ("Pot. nominal inv.", $"{inst.Inverter.MaxPowerKw:0.0} kW"),
            ("Nº MPPT",         inst.Inverter.Mppts.ToString()),
            ("Conexión",        "Monofásica 230 V"),
        });

        DrawPanel(slots[2], "PROTECCIONES", new (string, string)[]
        {
            ("Magnetotérmico",  $"{inst.MagnetoA} A curva C"),
            ("Diferencial",     $"{inst.DiffRatedmA} mA tipo A"),
            ("Puesta a tierra", $"{inst.EarthOhms:0.0} Ohm"),
            ("ICP",             "Sí, precintado"),
            ("Seccionador DC",  "Integrado en inversor"),
            ("Protección sobret.", "Tipo 2 (DC y AC)"),
        });

        Y = rowBounds.Bottom + 4;
    }

    private void DrawPanel(Rect bounds, string title, (string label, string value)[] rows)
    {
        // BrandPanel carries the fill / border / title styling, so this
        // method is purely about laying out rows -- no SetFill / SetDraw
        // / SetFont juggling before the call.
        Panel(bounds, title, BrandPanel, content =>
        {
            var rowRects = Stack(content, rows.Length);
            var labelW = content.W * 0.55;
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rowRects[i];

                SetXY(r.X, r.Y);
                SetFont("Helvetica", "", 8);
                SetTextColor(TextMuted);
                Cell(labelW, r.H, rows[i].label);

                SetXY(r.X + labelW, r.Y);
                SetFont("Helvetica", "B", 8);
                SetTextColor(BrandDark);
                Cell(r.W - labelW, r.H, rows[i].value, "0", 0, AlignEnum.Right);
            }
        });
    }

    // ---- Unifilar schematic (drawn in code) -------------------------------

    private void DrawUnifilarSchematic(Installation inst)
    {
        var x0 = LeftMargin;
        var y0 = Y;
        var w = W - LeftMargin - RightMargin;
        var h = 30.0;

        SetDrawColor(LineGray);
        SetLineWidth(0.2);
        Rect(x0, y0, w, h, "D");

        SetXY(x0 + 3, y0 + 1.5);
        SetFont("Helvetica", "B", 8);
        SetTextColor(BrandAccent);
        Cell(w - 6, 4, "ESQUEMA UNIFILAR");

        // Components from left to right.
        var components = new (string label, string measure)[]
        {
            ("CAMPO FV",   $"{inst.PeakPowerKwp:0.0##} kWp"),
            ("STRING BOX", "fusibles 15 A"),
            ("INVERSOR",   $"{inst.Inverter.MaxPowerKw:0.0} kW"),
            ("SECC. AC",   "interruptor"),
            ("PROT.",      $"{inst.MagnetoA} A · {inst.DiffRatedmA} mA"),
            ("CONTADOR",   "bidireccional"),
            ("RED",        "230 V / 50 Hz"),
        };

        var n = components.Length;
        var slotW = (w - 6) / n;
        var lineY = y0 + 16;
        var boxH = 7.0;
        var boxW = slotW * 0.75;

        SetDrawColor(BrandDark);
        SetLineWidth(0.35);
        // Continuous bus line at lineY.
        Line(x0 + 3 + boxW * 0.5, lineY + boxH / 2,
             x0 + 3 + slotW * (n - 1) + boxW * 0.5, lineY + boxH / 2);

        SetFont("Helvetica", "B", 7);
        for (int i = 0; i < n; i++)
        {
            var bx = x0 + 3 + slotW * i + (slotW - boxW) / 2;
            var by = lineY;
            SetFillColor(Color.White);
            SetDrawColor(BrandDark);
            SetLineWidth(0.35);
            Rect(bx, by, boxW, boxH, "DF");

            SetXY(bx, by + 1);
            SetTextColor(BrandDark);
            Cell(boxW, 3, components[i].label, "0", 0, AlignEnum.Center);
            SetXY(bx, by + 4);
            SetFont("Helvetica", "", 6);
            SetTextColor(TextMuted);
            Cell(boxW, 2.5, components[i].measure, "0", 0, AlignEnum.Center);
            SetFont("Helvetica", "B", 7);
        }

        // Earth symbol below the "PROT." block: three diminishing horizontal lines.
        var earthCenterX = x0 + 3 + slotW * 4 + slotW / 2;
        var earthY = lineY + boxH + 1.5;
        SetDrawColor(BrandDark);
        SetLineWidth(0.35);
        Line(earthCenterX, lineY + boxH, earthCenterX, earthY); // drop wire
        Line(earthCenterX - 3, earthY,     earthCenterX + 3, earthY);
        Line(earthCenterX - 2, earthY + 1, earthCenterX + 2, earthY + 1);
        Line(earthCenterX - 1, earthY + 2, earthCenterX + 1, earthY + 2);
        SetFont("Helvetica", "", 6);
        SetTextColor(TextMuted);
        SetXY(earthCenterX - 8, earthY + 3);
        Cell(16, 2.5, $"PaT {inst.EarthOhms:0.0} Ohm", "0", 0, AlignEnum.Center);

        Y = y0 + h + 4;
    }

    // ---- Strings measurement table ----------------------------------------

    private void DrawStringsTable(Installation inst)
    {
        var x0 = LeftMargin;
        var y0 = Y;
        var w = W - LeftMargin - RightMargin;

        SetXY(x0, y0);
        SetFont("Helvetica", "B", 8);
        SetTextColor(BrandAccent);
        Cell(w, 5, "MEDIDAS POR STRING");

        var headerY = y0 + 6;
        var rowH = 6.0;
        var cols = new (string title, double width, AlignEnum align)[]
        {
            ("Nº",           w * 0.06, AlignEnum.Center),
            ("Paneles",      w * 0.12, AlignEnum.Center),
            ("Voc (V)",      w * 0.14, AlignEnum.Right),
            ("Isc (A)",      w * 0.14, AlignEnum.Right),
            ("Vmpp (V)",     w * 0.14, AlignEnum.Right),
            ("Riso (MOhm)",    w * 0.14, AlignEnum.Right),
            ("Tolerancia",   w * 0.16, AlignEnum.Center),
            ("",             w * 0.10, AlignEnum.Center), // OK badge
        };

        SetXY(x0, headerY);
        SetFillColor(BrandDark);
        SetTextColor(Color.White);
        SetFont("Helvetica", "B", 7);
        for (int i = 0; i < cols.Length; i++)
        {
            Cell(cols[i].width, rowH, cols[i].title, "0", 0, cols[i].align, true);
        }
        Y = headerY + rowH;

        SetFont("Helvetica", "", 8);
        SetTextColor(BrandDark);
        SetDrawColor(LineGray);
        foreach (var s in inst.Strings)
        {
            SetX(x0);
            var vals = new[]
            {
                s.Number.ToString(),
                s.PanelCount.ToString(),
                s.Voc.ToString("0.0"),
                s.Isc.ToString("0.0"),
                s.Vmpp.ToString("0.0"),
                s.Rais.ToString("0.0"),
                "±3 %",
                "", // filled below with coloured badge
            };
            for (int i = 0; i < cols.Length - 1; i++)
            {
                Cell(cols[i].width, rowH, vals[i], "B", 0, cols[i].align);
            }
            // OK / NOK badge in last cell.
            var badgeX = X;
            var badgeY = Y;
            var bw = cols[^1].width;
            SetFillColor(s.Ok ? OkGreen : BrandAccent);
            Rect(badgeX + bw * 0.2, badgeY + 1.2, bw * 0.6, rowH - 2.4, "F");
            SetXY(badgeX, badgeY);
            SetTextColor(Color.White);
            SetFont("Helvetica", "B", 7);
            Cell(bw, rowH, s.Ok ? "OK" : "NOK", "B", 1, AlignEnum.Center);
            SetTextColor(BrandDark);
            SetFont("Helvetica", "", 8);
        }

        Y += 3;
    }

    // ---- Compliance checklist ---------------------------------------------

    private void DrawComplianceChecks(Installation inst)
    {
        var x0 = LeftMargin;
        var y0 = Y;
        var w = W - LeftMargin - RightMargin;

        SetXY(x0, y0);
        SetFont("Helvetica", "B", 8);
        SetTextColor(BrandAccent);
        Cell(w, 5, "CUMPLIMIENTO NORMATIVO");

        var listY = y0 + 6;
        var colW = w / 2.0;
        var rowH = 5.0;
        var perCol = (int)Math.Ceiling(inst.Compliance.Length / 2.0);

        for (int i = 0; i < inst.Compliance.Length; i++)
        {
            int col = i / perCol;
            int row = i % perCol;
            var cellX = x0 + col * colW;
            var cellY = listY + row * rowH;
            DrawCheckedItem(cellX, cellY, colW - 2, rowH, inst.Compliance[i]);
        }

        Y = listY + perCol * rowH + 3;
    }

    private void DrawCheckedItem(double x, double y, double w, double h, ComplianceItem item)
    {
        var boxSize = 3.2;
        SetDrawColor(BrandDark);
        SetLineWidth(0.25);
        SetFillColor(item.Pass ? BrandDark : Color.White);
        Rect(x, y + (h - boxSize) / 2, boxSize, boxSize, item.Pass ? "DF" : "D");

        if (item.Pass)
        {
            // Coral check-mark inside the dark box.
            SetDrawColor(BrandAccent);
            SetLineWidth(0.5);
            Line(x + 0.6, y + h / 2,        x + 1.4, y + h / 2 + 0.9);
            Line(x + 1.4, y + h / 2 + 0.9,  x + 2.7, y + h / 2 - 1.0);
            SetLineWidth(0.25);
        }

        SetXY(x + boxSize + 2, y);
        SetFont("Helvetica", "B", 8);
        SetTextColor(BrandDark);
        Cell(w * 0.32, h, item.Code);
        SetXY(x + boxSize + 2 + w * 0.32, y);
        SetFont("Helvetica", "", 8);
        SetTextColor(TextMuted);
        Cell(w - boxSize - 4 - w * 0.32, h, item.Title);
    }

    // ---- Observations & signatures ----------------------------------------

    private void DrawObservations(string observations)
    {
        var x0 = LeftMargin;
        var y0 = Y;
        var w = W - LeftMargin - RightMargin;

        SetXY(x0, y0);
        SetFont("Helvetica", "B", 8);
        SetTextColor(BrandAccent);
        Cell(w, 5, "OBSERVACIONES");

        var bodyY = y0 + 6;
        var bodyH = 22.0;

        SetFillColor(LightFill);
        SetDrawColor(LineGray);
        Rect(x0, bodyY, w, bodyH, "DF");

        SetXY(x0 + 2, bodyY + 1.5);
        SetFont("Helvetica", "", 8);
        SetTextColor(BrandDark);
        MultiCell(w - 4, 4, observations);

        Y = bodyY + bodyH + 4;
    }

    private void DrawSignatures(Installation inst)
    {
        var x0 = LeftMargin;
        var y0 = Y;
        var w = W - LeftMargin - RightMargin;
        var colW = w / 3.0;
        var headerH = 5.0;
        var signH = 14.0;
        var nameH = 6.0;

        var labels = new[] { "REALIZADO", "VERIFICADO", "ACEPTADO" };
        var names  = new[] { inst.DoneBy, inst.ReviewedBy, inst.AcceptedBy };

        for (int i = 0; i < 3; i++)
        {
            var x = x0 + i * colW;
            SetFillColor(BrandDark);
            SetTextColor(Color.White);
            SetFont("Helvetica", "B", 8);
            SetXY(x, y0);
            Cell(colW - (i == 2 ? 0 : 1), headerH, labels[i], "0", 0, AlignEnum.Center, true);

            SetDrawColor(LineGray);
            SetFillColor(Color.White);
            Rect(x, y0 + headerH, colW - (i == 2 ? 0 : 1), signH, "D");

            SetXY(x, y0 + headerH + signH);
            SetFont("Helvetica", "", 8);
            SetTextColor(BrandDark);
            Cell(colW - (i == 2 ? 0 : 1), nameH, names[i], "B", 0, AlignEnum.Center);
        }

        Y = y0 + headerH + signH + nameH + 2;
    }

    // ---- Annex 1: thermography grid ---------------------------------------

    private void DrawThermographyGrid(Installation inst)
    {
        var x0 = LeftMargin;
        var y0 = Y;
        var w = W - LeftMargin - RightMargin;
        var gap = 4.0;
        var cellW = (w - gap) / 2.0;
        var cellH = 92.0;

        for (int i = 0; i < Math.Min(inst.Thermography.Length, 4); i++)
        {
            int col = i % 2;
            int row = i / 2;
            var cx = x0 + col * (cellW + gap);
            var cy = y0 + row * (cellH + gap);
            DrawThermographyCell(cx, cy, cellW, cellH, inst.Thermography[i]);
        }
    }

    private void DrawThermographyCell(double x, double y, double w, double h, ThermographyEntry entry)
    {
        var captionH = 14.0;
        var imageH = h - captionH;

        DrawImageOrPlaceholder(entry.FileName, x, y, w, imageH);

        // Caption block beneath the image.
        SetFillColor(LightFill);
        SetDrawColor(LineGray);
        SetLineWidth(0.2);
        Rect(x, y + imageH, w, captionH, "DF");

        SetXY(x + 2, y + imageH + 1.5);
        SetFont("Helvetica", "B", 8);
        SetTextColor(BrandAccent);
        Cell(w - 4, 4, entry.Heading);

        SetXY(x + 2, y + imageH + 5.5);
        SetFont("Helvetica", "", 7.5);
        SetTextColor(BrandDark);
        MultiCell(w - 4, 3, entry.Note);
    }

    // ---- Annex 2: photo gallery -------------------------------------------

    private void DrawPhotoGallery(Installation inst)
    {
        var x0 = LeftMargin;
        var y0 = Y;
        var w = W - LeftMargin - RightMargin;
        var gap = 4.0;
        var cellW = (w - gap) / 2.0;
        var cellH = 64.0;
        var captionH = 8.0;
        var imageH = cellH - captionH;

        for (int i = 0; i < inst.Photos.Length; i++)
        {
            int col = i % 2;
            int row = i / 2;
            var cx = x0 + col * (cellW + gap);
            var cy = y0 + row * (cellH + gap);
            if (cy + cellH > H - BottomMarginBudget()) break;

            DrawImageOrPlaceholder(inst.Photos[i].FileName, cx, cy, cellW, imageH);

            SetXY(cx, cy + imageH + 1);
            SetFont("Helvetica", "", 8);
            SetTextColor(BrandDark);
            Cell(cellW, captionH - 1, inst.Photos[i].Caption);
        }
    }

    private double BottomMarginBudget() => 10;

    // ---- Image / placeholder ----------------------------------------------

    private void DrawImageOrPlaceholder(string fileName, double x, double y, double w, double h)
    {
        var fullPath = Path.Combine(_assetsPath, fileName);
        if (File.Exists(fullPath))
        {
            try
            {
                Image(fullPath, x, y, w, h);
                return;
            }
            catch { /* fall through to placeholder */ }
        }

        // Placeholder rectangle with the expected file name in the centre.
        SetFillColor(LightFill);
        SetDrawColor(LineGray);
        SetLineWidth(0.2);
        Rect(x, y, w, h, "DF");

        SetFont("Helvetica", "I", 9);
        SetTextColor(TextMuted);
        SetXY(x, y + h / 2 - 3);
        Cell(w, 4, $"[ {fileName} pending ]", "0", 0, AlignEnum.Center);
        SetFont("Helvetica", "", 7);
        SetXY(x, y + h / 2 + 1);
        Cell(w, 3, "drop the file next to the test binary to replace this placeholder", "0", 0, AlignEnum.Center);
    }

    // ---- Sample data -------------------------------------------------------

    private static Installation BuildSampleInstallation()
    {
        return new Installation(
            Owner: "Lucía Martínez Pérez",
            Address: "Avenida del Sol 42, 28015 Madrid",
            Cups: "ES0024 0001 2345 6789 AB 1P",
            InstallerCompany: "Soluciones Solares Iberia, S.L.",
            InstallerRegistration: "RIITE 28/12345",
            ActDate: "12 mayo 2026",
            ActNumber: "ACT-2026-0042",
            PeakPowerKwp: 4.4,
            Panels: 11,
            PanelModel: "JA Solar JAM54S30-400",
            Inverter: new InverterInfo(
                Brand: "Huawei",
                Model: "SUN2000-4KTL-L1",
                Serial: "INV2026-A0042",
                MaxPowerKw: 4.0,
                Mppts: 2),
            Strings: new[]
            {
                new StringMeasurement(1, 6, 247.4, 9.6, 18.5, 195.0, true),
                new StringMeasurement(2, 5, 206.1, 9.7, 22.1, 162.3, true),
            },
            AcVoltage: 230.4,
            AcFrequency: 50.02,
            EarthOhms: 11.8,
            DiffRatedmA: 30,
            MagnetoA: 25,
            Compliance: new[]
            {
                new ComplianceItem("RD 244/2019",  "Autoconsumo eléctrico",                true),
                new ComplianceItem("RD 842/2002",  "REBT - Reglamento Electrotécnico BT",   true),
                new ComplianceItem("ITC-BT-40",    "Instalaciones generadoras BT",          true),
                new ComplianceItem("ITC-BT-29",    "Locales con riesgo de incendio",        true),
                new ComplianceItem("UNE 217001",   "Conectores eléctricos en módulos FV",   true),
                new ComplianceItem("CIE firmado",  "Certificado de Instalación Eléctrica",  true),
            },
            Observations:
                "Instalación de 4.4 kWp en cubierta inclinada con orientación sur y 30° de pendiente. " +
                "Los dos strings presentan medidas Voc/Isc dentro de la tolerancia ±3 % respecto al cálculo " +
                "previo. Termografía sin anomalías térmicas relevantes. Puesta a tierra TT con dispersión " +
                "de 11.8 Ohm, por debajo del límite normativo. Instalación apta para conexión a red y " +
                "tramitación de alta como autoconsumo según RD 244/2019.",
            DoneBy: "Carlos Vidal\ninstalador electricista",
            ReviewedBy: "Marta Llopis\ningeniera técnica industrial\ncolegiada nº 4521",
            AcceptedBy: "Lucía Martínez Pérez\ntitular",
            Photos: new[]
            {
                new PhotoEntry("pv-rooftop.png",      "1. Panorámica de la instalación en cubierta"),
                new PhotoEntry("pv-inverter.png",     "2. Inversor SUN2000-4KTL-L1 en operación"),
                new PhotoEntry("pv-stringbox.png",    "3. Caja de strings DC con regletas etiquetadas"),
                new PhotoEntry("pv-rooftop.png",      "4. Detalle de fijaciones mecánicas y pasarelas"),
                new PhotoEntry("pv-rooftop.png",      "5. Cableado DC en bandeja perforada"),
                new PhotoEntry("pv-inverter.png",     "6. Pantalla del inversor con producción inicial"),
            },
            Thermography: new[]
            {
                new ThermographyEntry("pv-thermography.png", "String 1 - vista general",
                    "Sin anomalías térmicas. dT entre módulos < 5 °C. Operación nominal."),
                new ThermographyEntry("pv-thermography.png", "String 2 - módulo 3",
                    "Punto caliente localizado de 14 °C sobre ambiente. Dentro de tolerancia; revisar próxima campaña."),
                new ThermographyEntry("pv-thermography.png", "Inversor",
                    "Carcasa y radiador en temperatura nominal. Sin fugas térmicas en conexionado AC."),
                new ThermographyEntry("pv-thermography.png", "Cuadro general AC",
                    "Sin sobrecalentamientos en magnetotérmico ni diferencial. Aprietes correctos."),
            });
    }

    // ---- Records -----------------------------------------------------------

    private sealed record Installation(
        string Owner, string Address, string Cups,
        string InstallerCompany, string InstallerRegistration,
        string ActDate, string ActNumber,
        double PeakPowerKwp, int Panels, string PanelModel,
        InverterInfo Inverter,
        StringMeasurement[] Strings,
        double AcVoltage, double AcFrequency,
        double EarthOhms, int DiffRatedmA, int MagnetoA,
        ComplianceItem[] Compliance,
        string Observations,
        string DoneBy, string ReviewedBy, string AcceptedBy,
        PhotoEntry[] Photos,
        ThermographyEntry[] Thermography);

    private sealed record InverterInfo(string Brand, string Model, string Serial, double MaxPowerKw, int Mppts);
    private sealed record StringMeasurement(int Number, int PanelCount, double Voc, double Isc, double Rais, double Vmpp, bool Ok);
    private sealed record ComplianceItem(string Code, string Title, bool Pass);
    private sealed record PhotoEntry(string FileName, string Caption);
    private sealed record ThermographyEntry(string FileName, string Heading, string Note);
}
