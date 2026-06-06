using Ego.PDF.Barcodes.Zpl;
using Ego.PDF.Data;
using System;
using System.IO;
using System.Text;

namespace Ego.PDF.Samples
{
    /// <summary>
    /// Reference labels for the ZPL → PDF pipeline. None of these
    /// reflect a real shipment — the company name, addresses, tracking
    /// numbers and logo are all generic placeholders ("ACME Logistics")
    /// so the sample can ship in an open-source repo.
    /// </summary>
    public class SampleZebra: FPdf
    {
        private SampleZebra(string file) : base(file) { }

        // 4" x 6" label in dots at 203 dpi. Used by the two shipping
        // labels and by the catalogue; the horizontal label leaves it
        // unset because the ZPL stream doesn't depend on the clamp.
        private const int Dpi      = 203;
        private const int LabelW   = 812;
        private const int LabelH   = 1218;

        /// <summary>
        /// Boilerplate-free entry point shared by all three Zebra
        /// samples: configures the FPdf for 203 dpi point units,
        /// embeds the Roboto-Mono Bold TTF used for ZPL monospace
        /// font slot "A", wires the proportional slot "0" to Helvetica
        /// Bold and returns a ready-to-Print <see cref="PdfZpl"/>.
        /// When <paramref name="labelSize"/> is supplied the parser
        /// clamps oversized ^PW / ^LL to that physical bound.
        /// </summary>
        private static PdfZpl OpenLabel(SampleZebra pdf, (int width, int height)? labelSize = null)
        {
            pdf.SetUnitConverionFactor(UnitEnum.Point, Dpi);
            pdf.LoadFont("robotomonob",
                Path.Combine(GetPath(), "Fonts", "Roboto_Mono", "Static", "RobotoMono-Bold.ttf"));
            pdf.AddFont("robotomonob", "");
            // Helvetica-Bold is a PDF core font: no embedding needed.
            pdf.SetFont("helvetica", "B", 16);

            var zpl = new PdfZpl(pdf, Dpi);
            if (labelSize.HasValue)
                zpl.SetLabelSize(labelSize.Value.width, labelSize.Value.height);
            zpl.SetVariableFont("helvetica", "B");
            zpl.SetMonospaceFont("robotomonob");
            return zpl;
        }

        /// <summary>
        /// Read a ZPL string from <paramref name="zplPath"/> and render it
        /// through <see cref="PdfZpl"/>. Generic loader used by the
        /// "open a local ZPL and produce its PDF" debug endpoint in the
        /// WebDemo; lets us point at any ad-hoc ZPL file without baking
        /// its content into the sample assembly. Throws
        /// <see cref="FileNotFoundException"/> if the file is missing.
        /// </summary>
        public static Stream GetSampleFromZplFile(string file, string zplPath)
        {
            if (!File.Exists(zplPath))
                throw new FileNotFoundException("ZPL source not found", zplPath);

            using var pdf = new SampleZebra(file);
            var zpl = OpenLabel(pdf, (LabelW, LabelH));
            zpl.Print(File.ReadAllText(zplPath));
            pdf.Close();
            return pdf.Buffer.BaseStream;
        }

        /// <summary>
        /// Minimal smoke / regression for the ^FR (Field Reverse) handling on
        /// TEXT fields. Paints a solid black ^GB rectangle, drops a ^FR-marked
        /// text on top of it (which should render white -- the engine inverts
        /// the glyph colour against the dark fill), and below it a plain black
        /// text for visual comparison. If the ^FR path regresses, the top
        /// text disappears (black-on-black) and the byte hash shifts.
        /// </summary>
        public static Stream GetSampleReverseText(string file, string resourcePath)
        {
            using var pdf = new SampleZebra(file);
            var zpl = OpenLabel(pdf, (LabelW, LabelH));
            // Three rows exercising every interesting case of ^FR on text:
            //   row 1: ^FR text fully inside a black ^GB rect (canonical).
            //   row 2: ^FR text that runs past the rect edge -- BlendMode
            //          /Difference inverts per pixel, so the half over
            //          black reads white and the half over white reads
            //          black. With the SetTextColor fallback the right
            //          half would be invisible.
            //   row 3: plain text without ^FR for visual comparison.
            zpl.Print(@"
^XA
^FO40,40^GB360,80,80,B,0^FS
^FT60,100^A0N,40^FR^FDREVERSED^FS

^FO40,180^GB200,80,80,B,0^FS
^FT60,240^A0N,40^FR^FDCROSSING THE EDGE^FS

^FT40,360^A0N,40^FDNORMAL TEXT^FS
^XZ
");
            pdf.Close();
            return pdf.Buffer.BaseStream;
        }

        /// <summary>
        /// 4" x 6" catalogue label that exercises every ZPL barcode
        /// command the engine maps onto ZXing.Net (^BC, ^B3, ^B2, ^BK,
        /// ^BE, ^B8, ^BU, ^B9, ^BM, ^BQ, ^BX, ^B7, ^BO). Visual smoke
        /// test, not a regression assertion.
        /// </summary>
        public static Stream GetSampleBarcodes(string file, string resourcePath)
        {
            using var pdf = new SampleZebra(file);
            var zpl = OpenLabel(pdf, (LabelW, LabelH));
            zpl.Print(@"
^XA
^CF0,24

^FX 1D barcodes — left column. Each row is 150 dots tall (label + bars).^FS
^FO40,20^FDCode 128 BC^FS
^FO40,55^BCN,80,N,N,N^FD12345678^FS

^FO40,170^FDCode 39 B3^FS
^FO40,205^B3N,N,80,N,N^FDABC-12345^FS

^FO40,320^FDInterleaved 2 of 5 B2^FS
^FO40,355^B2N,80,N,N,N^FD12345678901234^FS

^FO40,470^FDCodabar BK^FS
^FO40,505^BKN,N,80,N,N,A,B^FD12345678^FS

^FO40,620^FDEAN-13 BE^FS
^FO40,655^BEN,80,N,N^FD012345678912^FS

^FO40,770^FDEAN-8 B8^FS
^FO40,805^B8N,80,N,N^FD1234567^FS

^FO40,920^FDUPC-A BU^FS
^FO40,955^BUN,80,N,N,N^FD12345678901^FS

^FO40,1070^FDMSI BM^FS
^FO40,1105^BMN,A,80,N,N,N^FD12345678^FS

^FX 2D codes — right column.^FS
^FO470,20^FDQR Code BQ^FS
^FO470,55^BQN,2,5^FDQR via ZPL^FS

^FO470,260^FDData Matrix BX^FS
^FO470,295^BXN,5,200^FDDataMatrix via ZPL^FS

^FO470,500^FDPDF417 B7^FS
^FO470,535^B7N,3^FDPDF417 via ZPL^FS

^FO470,750^FDAztec BO^FS
^FO470,785^BON,5^FDAztec via ZPL^FS

^XZ
");

            pdf.Close();
            return pdf.Buffer.BaseStream;
        }

        /// <summary>
        /// Horizontal 4" x 6" shipping label, adapted from the classic
        /// Intershipping demo distributed with Zebra's ZPL documentation.
        /// Demonstrates ^GB / ^FR for the nested-square placeholder logo,
        /// proportional and monospace fonts via ^CF and ^CFA, and a
        /// Code 128 tracking barcode with its human-readable line. All
        /// data is fictitious.
        /// </summary>
        public static Stream GetSampleHorizontalShipping(string file, string resourcePath)
        {
            using var pdf = new SampleZebra(file);
            // No SetLabelSize: the ZPL stream stays within the implicit
            // 4" x 6" envelope so leaving the clamp unset matches the
            // original Intershipping example.
            var zpl = OpenLabel(pdf);
            zpl.Print(@"
^XA

^FX Top section: geometric logo (hollow square + two filled squares
^FX on the descending diagonal), company name and origin address.
^CF0,60
^FO50,50^GB130,130,5^FS
^FO65,65^GB30,30,30^FS
^FO135,135^GB30,30,30^FS
^FO220,50^FDACME Logistics^FS
^CF0,30
^FO220,115^FD500 Industrial Boulevard^FS
^FO220,155^FDPortland OR 97201^FS
^FO220,195^FDUnited States (USA)^FS
^FO50,250^GB700,3,3^FS

^FX Recipient + permit panel.
^CFA,30
^FO50,300^FDJane Doe^FS
^FO50,340^FD42 Example Avenue^FS
^FO50,380^FDPortland OR 97215^FS
^FO50,420^FDUnited States (USA)^FS
^CFA,15
^FO600,300^GB150,150,3^FS
^FO638,340^FDPermit^FS
^FO638,390^FD987654^FS
^FO50,500^GB700,3,3^FS

^FX Code 128 tracking number.
^BY5,2,270
^FO100,550^BC^FD98765432^FS

^FX Footer: container + destination state.
^FO50,900^GB700,250,3^FS
^FO400,900^GB3,250,3^FS
^CF0,40
^FO100,960^FDPkg Y99X-7^FS
^FO100,1010^FDREF1 A11B22^FS
^FO100,1060^FDREF2 C33D44^FS
^CF0,190
^FO470,955^FDOR^FS

^XZ
");

            pdf.Close();
            return pdf.Buffer.BaseStream;
        }

        /// <summary>
        /// Vertical 4" x 6" shipping label modelled on a real-world
        /// rotated-text courier label. Exercises ^FT bottom-origin text,
        /// the ^A?B 90° rotation, ^FB wrap inside boxes, an inline ^GFA
        /// bitmap logo (generated below) and a ^B2 Interleaved 2 of 5
        /// barcode. All field data is fictitious.
        /// </summary>
        public static Stream GetSampleVertical1(string file, string resourcePath)
        {
            using var pdf = new SampleZebra(file);
            // Physical label is 4" x 6" at 203 dpi -- the clamp keeps
            // ^PW / ^LL inside that envelope (matches Labelary's
            // behaviour when you pre-pick a label size).
            var zpl = OpenLabel(pdf, (LabelW, LabelH));

            var logoGfa = BuildEgoLogoGfa();

            zpl.Print($@"
^XA
^LL1218
^PW812

^FX Outer column boxes and dividers.
^FO580,40^GB200,550,2^FS
^FO10,40^GB100,540,2^FS
^FO268,600^GB0,590,2^FS
^FO580,600^GB0,590,2^FS

^FX Horizontal + vertical dividers of the small right-column grid.
^FO580,180^GB200,0,2^FS
^FO690,180^GB0,410,2^FS
^FO690,490^GB90,0,2^FS
^FO690,390^GB90,0,2^FS
^FO690,290^GB90,0,2^FS

^FX Geometric placeholder logo embedded as a ^^GFA bitmap.
^FO20,60{logoGfa}^FS

^FX Service code in the top right cell.
^FT65,555^ADB,52,20^FDSTD-24^FS

^FX Print date + origin / destination.
^FT680,570^ACB,18,10^FDPrint date:^FS
^FT680,360^AFB,26,13^FD2026-01-15^FS
^FT160,580^FB250,2^AFB,26,13^FDfrom PORTLAND^FS
^FT160,290^FB250,2^AFB,26,13^FDto SEATTLE^FS

^FX Big delegation code.
^FT270,570^AGB,120,40^FD206 SEATTL^FS

^FX Payment label + vehicle / zone code.
^FT643,360^AFB,52,13^FDPrePaid^FS
^FT720,160^AGB,140,40^FD7B^FS

^FX Sender (origin) block.
^FO40,615^FB565,1300^ADB,32,18^FDACME Logistics^FS
^FO150,615^FB565,1150^ADB,26,14^FD500 Industrial Boulevard^FS
^FO190,615^FB565,1150^ADB,26,14^FD97201 PORTLAND - US^FS

^FT250,800^ACB,18,10^FDCCC:^FS
^FT250,750^ADB,26,14^FD99001-A^FS

^FX Recipient block.
^FO280,615^FB565,1150^ADB,24,14^FDJane Doe^FS
^FO350,615^FB565,1150^ADB,24,14^FDACME Store - Downtown^FS
^FO390,615^FB565,1150^ADB,18,13^FD42 Example Avenue^FS
^FO450,615^FB565,1150^ADB,39,13^FD97215 SEATTLE - US^FS

^FT570,1190^ACB,18,10^FDAtt:^FS
^FT570,1140^ADB,18,13^FDJane Doe^FS

^FX Reference codes.
^FT620,1190^ADB,26,14^FDRef.Exp:^FS
^FT620,1070^ADB,27,14^FDREF-ACME-001^FS

^FT685,1190^ADB,24,12^FDRef.Bul:^FS
^FT685,1070^ADB,27,14^FDREF-ACME-001/1^FS

^FT750,1190^ADB,26,13^FDObs:^FS

^FX Interleaved 2 of 5 tracking barcode + readable digits. ^BY sets
^FX a 4-dot module width so the bars are wide enough to span the
^FX center column of the label.
^BY4,3.0^FS
^FT490,580^B2B,200,N,N,N^FD99001234567890^FS
^FT535,490^AEB,28,15^FD99 001 2 3456789 0^FS

^FX Package count + weight panel.
^FT613,570^ADB,18,13^FDBoxes:^FS
^FT613,480^ADB,26,13^FD1/1^FS
^FT643,570^ADB,18,13^FDWeight:^FS
^FT643,480^ADB,26,13^FD2.5 kg^FS

^XZ
");

            pdf.Close();
            return pdf.Buffer.BaseStream;
        }

        /// <summary>
        /// Build a 64 x 64 stylised "E" letter as a ZPL ^GFA fragment.
        /// Used as a generic placeholder logo for the vertical sample so
        /// the open-source repo doesn't ship anyone else's artwork. The
        /// output is uncompressed ASCII hex: each row contributes
        /// bytesPerRow bytes (2 hex chars each), the parser autoflushes
        /// rows when the row buffer fills.
        /// </summary>
        private static string BuildEgoLogoGfa()
        {
            const int width = 64;
            const int height = 64;
            const int bytesPerRow = width / 8;        // 8
            const int strokeH = 10;                   // horizontal arm thickness
            const int strokeV = 10;                   // left bar thickness
            const int midArm = 44;                    // middle arm length
            const int midStart = (height - strokeH) / 2;

            bool On(int row, int col)
            {
                if (col < strokeV) return true;                                  // left vertical bar
                if (row < strokeH) return true;                                  // top arm
                if (row >= height - strokeH) return true;                        // bottom arm
                if (row >= midStart && row < midStart + strokeH && col < midArm) // middle arm
                    return true;
                return false;
            }

            var sb = new StringBuilder();
            for (int row = 0; row < height; row++)
            {
                for (int b = 0; b < bytesPerRow; b++)
                {
                    byte v = 0;
                    for (int bit = 0; bit < 8; bit++)
                        if (On(row, b * 8 + bit))
                            v |= (byte) (1 << (7 - bit));
                    sb.Append(v.ToString("X2"));
                }
            }
            var totalBytes = height * bytesPerRow;
            return $"^GFA,{totalBytes},{totalBytes},{bytesPerRow},{sb}";
        }

        private static string GetPath()
        {
            var codeBase = AppDomain.CurrentDomain.BaseDirectory;
            return Path.GetDirectoryName(codeBase);
        }
    }
}
