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
        [Fact]
        public void DoLocalDebug()
        {
            const string zplPath = @"C:\Users\Nitropc\Downloads\15420-E-2026_1.zpl";
            const string outPath = @"C:\Users\Nitropc\Downloads\15420-E-2026_1.pdf";
            if (!File.Exists(zplPath)) return;
            using var pdf = new FPdf(outPath);
            pdf.SetUnitConverionFactor(UnitEnum.Point, 203);
            pdf.LoadFont("robotomonob", Path.Combine(GetPath(), "Fonts", "Roboto_Mono", "Static", "RobotoMono-Bold.ttf"));
            pdf.AddFont("robotomonob", "");
            pdf.LoadFont("robotocondensed", Path.Combine(GetPath(), "Fonts", "Roboto_Condensed", "RobotoCondensed-VariableFont_wght.ttf"));
            pdf.AddFont("robotocondensed", "");
            pdf.LoadFont("robotocondensedb", Path.Combine(GetPath(), "Roboto", "RobotoCondensed-Bold.ttf"));
            pdf.AddFont("robotocondensed", "B");
            pdf.SetFont("helvetica", "B", 16);
            var zpl = new PdfZpl(pdf, 203);
            zpl.SetVariableFont("helvetica", "B");
            zpl.SetMonospaceFont("robotomonob");
            zpl.SetCondensedFont("robotocondensed", "B");
            zpl.Print(File.ReadAllText(zplPath));
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

        [Fact]
        public void DoSampleFormAndTable()
        {
            SampleFormAndTable.GetSample("sample-form-and-table.pdf");
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
        /// Portrait courier label adapted (and fully anonymised) from a
        /// real Spanish parcel-service ZPL: vertical Interleaved 2 of 5
        /// strip, ^AV* condensed proportional fonts, ^FO + rotated
        /// ^ABB column titles, Code 128 with a small left-aligned
        /// caption.
        /// </summary>
        [Fact]
        public void DoZebraCourierPortrait()
        {
            SampleZebra.GetSampleCourierPortrait("sample_zebra_courier_portrait.pdf", GetPath());
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
