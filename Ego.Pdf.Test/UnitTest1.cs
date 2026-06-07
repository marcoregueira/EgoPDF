using Ego.PDF;
using Ego.PDF.Barcodes.Zpl;
using Ego.PDF.Data;
using Ego.PDF.Samples;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace Ego.Pdf.Test
{
    public class SampleTests
    {
        // Temporary local-debug test. Reads the GLS ZPL the user is
        // investigating from their Downloads folder, renders it through
        // PdfZpl and writes the resulting PDF next to the source for
        // side-by-side comparison with the Labelary reference. Skipped
        // automatically when the input file isn't present (so it
        // doesn't break CI / other devs' clones).
        [Fact]
        public void DoGlsLabelLocalDebug()
        {
            const string zplPath = @"C:\Users\Nitropc\Downloads\324f_1.zpl";
            const string outPath = @"C:\Users\Nitropc\Downloads\324f_1_ours.pdf";
            if (!File.Exists(zplPath)) return;

            using var pdf = new FPdf(outPath);
            pdf.SetUnitConverionFactor(UnitEnum.Point, 203);
            pdf.LoadFont("robotomonob", Path.Combine(GetPath(), "Fonts", "Roboto_Mono", "Static", "RobotoMono-Bold.ttf"));
            pdf.AddFont("robotomonob", "");
            pdf.LoadFont("robotocondensed", Path.Combine(GetPath(), "Fonts", "Roboto_Condensed", "RobotoCondensed-VariableFont_wght.ttf"));
            pdf.AddFont("robotocondensed", "");
            pdf.SetFont("helvetica", "B", 16);

            var zpl = new PdfZpl(pdf, 203);
            zpl.SetVariableFont("helvetica", "B");
            zpl.SetMonospaceFont("robotomonob");
            zpl.SetCondensedFont("robotocondensed");
            zpl.Print(File.ReadAllText(zplPath));
            pdf.Close();
        }

        // Synthetic label with the ^AV* fields + separators + ^ABB rotated
        // labels, all shifted down 40 dots to leave room for a top ruler.
        // Full-height vertical guide lines every 25 dots (light) and every
        // 50 dots (heavier), with the X coordinate written at the top
        // (Y=5) and bottom (Y=1210) of each 50-dot line. The user opens
        // the PDF, looks at where each text ends, and reports the matching
        // X coordinate.
        [Fact]
        public void DoGlsLabelMeasureRuler()
        {
            const string outPath = @"C:\Users\Nitropc\Downloads\324f_measure.pdf";
            const string zplOutPath = @"C:\Users\Nitropc\Downloads\324f_measure.zpl";

            using var pdf = new FPdf(outPath);
            pdf.SetUnitConverionFactor(UnitEnum.Point, 203);
            pdf.LoadFont("robotomonob", Path.Combine(GetPath(), "Fonts", "Roboto_Mono", "Static", "RobotoMono-Bold.ttf"));
            pdf.AddFont("robotomonob", "");
            pdf.LoadFont("robotocondensed", Path.Combine(GetPath(), "Fonts", "Roboto_Condensed", "RobotoCondensed-VariableFont_wght.ttf"));
            pdf.AddFont("robotocondensed", "");
            pdf.SetFont("helvetica", "B", 16);

            var zpl = new PdfZpl(pdf, 203);
            zpl.SetVariableFont("helvetica", "B");
            zpl.SetMonospaceFont("robotomonob");
            zpl.SetCondensedFont("robotocondensed");

            var lines = new List<string>
            {
                "^XA",
                "^PW799",
                "^LL1230",
                "^LS-10",

                // ---- Content (shifted down 40 dots to free Y=0..30 for the top ruler labels) ----
                "^FO250,40^AVN,120,100^FD43001306863287^FS",       // TRACKING
                "^FO0,170^GB799,0,2^FS",                           // full-width separator

                "^FO360,340^AVN,120,200^FDXXX^FS",                 // XXX
                "^FO320,510^GB450,0,4^FS",                         // 450-dot decorative line @ X=320..770

                "^FO320,640^AVN,105,50^FD27004^FS",                // 27004
                "^FO550,640^AVN,110,50^FD    1/1^FS",              // 1/1 (with 4 leading spaces)
                "^FO320,860^GB450,0,4^FS",                         // bottom decorative line @ X=320..770

                "^FO237,950^ABB,10,10^FDDESTINATARIO^FS",          // DESTINATARIO (rotated)
                "^FO307,950^ABB,10,10^FDREMITENTE^FS",
                "^FO377,950^ABB,10,10^FDOBSERVACI.^FS",
                "^FO0,940^GB799,0,1^FS",                           // top of rotated zone
                "^FO0,1080^GB799,0,1^FS",                          // bottom of rotated zone
            };

            // ---- Vertical reference lines spanning the FULL page height ----
            // Tick every 25 dots, heavier at 50, with X coord written at the
            // top (Y=5) and bottom (Y=1210) of each 50-dot line.
            for (int x = 0; x <= 799; x += 25)
            {
                var thickness = (x % 100 == 0) ? 1 : 1;
                lines.Add($"^FO{x},0^GB1,1230,{thickness}^FS");
            }
            // Numbered labels every 50 dots, top and bottom. Two staggered
            // rows so the labels don't overlap: multiples of 100 on the
            // inner row (closer to the page edge), 50-offsets on the outer.
            // Row spacing is 30 dots to clear the actual rendered glyph
            // height of ^A0N,8 (the "font 0" path goes through helvetica
            // with a generous ascent and lands taller than the nominal h).
            for (int x = 0; x <= 799; x += 50)
            {
                var topY = (x % 100 == 0) ? 5 : 35;
                var botY = (x % 100 == 0) ? 1195 : 1165;
                // ^A0N,8,4 — explicit width avoids the SetFont parser
                // early-return when only height is given (^A0N,8 alone
                // leaves Thickness at the previous ^A?h,w value, e.g. 105
                // from 27004, and renders the labels at ~30-dot em).
                lines.Add($"^FO{x + 2},{topY}^A0N,8,4^FD{x}^FS");
                lines.Add($"^FO{x + 2},{botY}^A0N,8,4^FD{x}^FS");
            }
            lines.Add("^FO775,5^A0N,8,4^FD799^FS");
            lines.Add("^FO775,1195^A0N,8,4^FD799^FS");
            lines.Add("^FO799,0^GB1,1230,2^FS");

            lines.Add("^XZ");

            var zplSrc = string.Join("\n", lines);
            File.WriteAllText(zplOutPath, zplSrc);
            zpl.Print(zplSrc);
            pdf.Close();
        }


        [Fact]
        public void DoSample1()
        {
            Sample1.GetSample("sample1.pdf");
        }

        [Fact]
        public void DoSample2()
        {
            Sample2.GetSample("sample2.pdf", imagefile: "logo.png");
        }

        [Fact]
        public void DoSample2b()
        {
            Sample2.GetSample("sample2b.pdf", imagefile: "3d_down.png");
        }

        [Fact]
        public void DoSample2jpg()
        {
            Sample2.GetSample("sample2jpg.pdf", imagefile: "v3v.jpg");
        }

        [Fact]
        public void DoSample2png()
        {
            Sample2.GetSample("sample2png.pdf", imagefile: "v3v.png");
        }

        [Fact]
        public void DoSample3()
        {
            Sample3.GetSample("sample3.pdf", GetPath());
        }

        [Fact]
        public void DoSample4()
        {
            Sample4.GetSample("sample4.pdf", GetPath());
        }

        [Fact]
        public void DoSample5()
        {
            Sample5.GetSample("sample5.pdf", GetPath());
        }

        [Fact]
        public void DoSample6()
        {
            Sample6.GetSample("sample6.pdf", GetPath());
        }

        [Fact]
        public void DoSample7()
        {
            Sample7.GetSample("sample7.pdf");
        }

        [Fact]
        public void DoSample8()
        {
            Sample8.GetSample("sample8.pdf", GetPath());
        }

        [Fact]
        public void DoSample8b()
        {
            Sample8b.GetSample("sample8b.pdf", GetPath());
        }

        [Fact]
        public void DoSample8c()
        {
            Sample8c.GetSample("sample8c.pdf", GetPath());
        }

        [Fact]
        public void DoSampleMarkdown()
        {
            SampleMarkdown.GetSample("sample-markdown.pdf");
        }

        [Fact]
        public void DoSamplePhotovoltaic()
        {
            SamplePhotovoltaic.GetSample("sample-photovoltaic.pdf", GetPath());
        }

        // --------------------------------------------------------------
        // Zebra "tests" (visual smoke tests, not assertions)
        //
        // These [Fact]s drive the ZPL → PDF pipeline end-to-end and drop
        // the resulting PDF next to the test binaries so a human can open
        // them and eyeball the result. They pass as long as the code runs
        // without throwing — there's no automatic comparison.
        //
        // Outputs land in:
        //   Ego.Pdf.Test/bin/{Debug|Release}/net8.0/sample_zebra_*.pdf
        //
        // To compare against Labelary, paste the ZPL emitted by each
        // sample into the Labelary viewer (https://labelary.com/viewer.html)
        // and drop the resulting PDF beside the generated one.
        // --------------------------------------------------------------

        /// <summary>
        /// Horizontal shipping label demo (ACME Logistics, generic data).
        /// Adapted from the classic Intershipping ZPL shipped with the
        /// Zebra documentation, with all real-looking values replaced by
        /// placeholders. Uses ^CF / ^GB / ^FR / ^BC.
        ///
        /// Output: sample_zebra_horizontal.pdf
        /// </summary>
        [Fact]
        public void DoZebraHorizontalShipping()
        {
            SampleZebra.GetSampleHorizontalShipping("sample_zebra_horizontal.pdf", GetPath());
        }

        /// <summary>
        /// Catalogue of every ZPL barcode command the engine maps onto a
        /// ZXing writer: ^BC, ^B3, ^B2, ^BK, ^BE, ^B8, ^BU, ^B9, ^BM,
        /// ^BQ, ^BX, ^B7 and ^BO. Used to eyeball the rendering — there's
        /// no automated check, just "did it crash".
        ///
        /// Output: sample_zebra_barcodes.pdf
        /// </summary>
        [Fact]
        public void DoZebraBarcodes()
        {
            SampleZebra.GetSampleBarcodes("sample_zebra_barcodes.pdf", GetPath());
        }

        /// <summary>
        /// Vertical shipping label (ACME Logistics, generic data). Stress
        /// test for the rotated-text codepath: ^FT bottom-origin, ^A?B
        /// 90° rotation, ^FB wrap inside narrow boxes, an inline ^GFA
        /// placeholder logo and a ^B2 Interleaved 2 of 5 barcode.
        ///
        /// Output: sample_zebra_vertical1.pdf
        /// </summary>
        [Fact]
        public void DoZebraVertical1()
        {
            SampleZebra.GetSampleVertical1("sample_zebra_vertical1.pdf", GetPath());
        }

        /// <summary>
        /// Smoke test for ^FR (Field Reverse) on a text field. A regression
        /// would still produce a PDF (the engine doesn't throw); the visual
        /// baseline catches the byte-level drift.
        /// </summary>
        [Fact]
        public void DoZebraReverseText()
        {
            SampleZebra.GetSampleReverseText("sample_zebra_reverse_text.pdf", GetPath());
        }


        [Fact]
        public void DoSample9()
        {
            Sample9.GetSample("sample9.pdf", GetPath());
        }


        #region Auxiliary methods
        private string GetPath()
        {
            var codeBase = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(codeBase);
        }
        #endregion
    }
}
