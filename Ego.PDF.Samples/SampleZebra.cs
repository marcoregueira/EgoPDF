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
^IDR:LOGO3^FS
^LL1650
^DFSEURCPT1.001^FS
^CI28^FS
^MD10
^PRC,D
^LH1,1^FS
^PW840

^FX CUADROS DE RASL^FS
^FO580,40^GB200,550,2^FS

^FX CUADRO DE SERV/PROD ^FS
^FO10,40^GB100,540,2^FS
^FO10,320^GB100,0,logo2^FS

^FX LINEAS HOR. CUADRO GRANDE^FS
^FO268,600^GB0,590,2^FS
^FO580,600^GB0,590,2^FS

^FX LINEAS VERTICALES DE CUADRO RAS ^FS
^FO580,180^GB200,0,2^FS
^FO690,180^GB0,410,2^FS
^FO690,490^GB90,0,2^FS
^FO690,390^GB90,0,2^FS
^FO690,290^GB90,0,2^FS

^FX LOGO SEUR ^FS^FX^FS
^FO020,60^GFA,2484,2484,12, ,:::::::::::::::::::::::::::::::M01500155,M0I20A22,M0I515H5,M0IA8AA8,L015L5,M0H2A2I2,L015L5,M0LA0,L015L5,L0H202H2,L0150154,M0800A8,L0150150,L0H2H028,L0150150,M0A00A8,L0150150,L0H2H028,L015L5,M08AJA8,L015L5,L0I2A2I2,L015L5,M0LA0,L015L5,L0N2,L015L5,,L015J5,L0M20,L015K50,M0A8H8HA8,L015K54,L0N2,L015K54,M0LA8,L015L5,R0H2,R0H5,R020,R0H5,R0H2,R0H5,R028,R0H5,R0H2,N04045H5,M0KA80,L015L5,L0H2A2J2,L015K54,M0LA8,L015K50,L0I2A2A20,L015J540,,:L0H280H0H2,L015015055,M0A82A028,L015015055,L0H2822022,L015015055,M0A82A020,L015015055,L0H282A022,L015015055,M0H82A028,L015015055,L0H2022022,L015415055,M0KA80,L015L5,L0N2,L015L5,M0HA8A8A8,L015L5,L0N2,L015L5,,Q05,N0202H20,M01405H50,M02A0A8A8,M05415H54,M0AE2J2,L015415H54,M0HA2AA80,L0H5415I5,L0H2A2H2A2,L0H5455455,M0802A828,L0H5055015,L0H2022022,L054055015,M0A0AA02080L0H5055015,L0H2022022,L0J54055,M08A8A2A8,L015H54155,L0J202H2,L015H54154,M0IA82A0,M0I50154,M02A202A0,M01540540,Q0280,,:::::::L015L5,L02EFEIEF,L015L5,L03FBFHFBF,L015L5,L02EKEF,L015L5,L03BFBFIF,L015L5,L02EKEF,L015L5,L03FLF,L015L5,L02EKEF,L015L5,L03BFBFBFF,L015L5,L02EKEF,L015L5,L03FBFJF,L015L5,L02EKEF,L015L5,L03BFBFIF,L015L5,L02EKEF,L015L5,L03FLF,L015L5,L02EKEF,L015L5,L03BFBFBFF,L015L5,L02EKEF,L015L5,L03FBFJF,L015L5,L02EKEF,L015L5,L03BFBFIF,L015L5,L02EKEF,L015L5,L03FLF,L015L5,L02EKEF,L015L5,L03BFBFBFF,L015L5,L02EKEF,L015L5,L03FBFJF,L015L5,L02EKEF,L015L5,L03BFBFIF,L015L5,L02EKEF,L015L5,L03FHFBFHF,L015L5,L02EKEF,L015L5,,L015L5,M0L20,L015L5,N080,L015L5,M0L20,L015L5,M080H08,L015L5,L0M20,L015L5,,L015L5,M0L20,L015L5,M080808,L015L5,M0L20,L015L5,N0808080,L015L5,M0L20,L015L5,M0I8A880,L015L5,,N0404040,,::M04040404,,::N0404040,,::M040404,,::N0404040,,
^BY4,3.0^FS

^FO005,725^XGLOGO,1,1^FS
^BY4,3.0^FS
^FX Servicio / producto ^FS
^FX Recuadro negro SP ^FS^FO10,320^GB100,260,80,B,0^FS^FT65,555^FR^ADB,52,20^FN1^FA9^FS
^FX Fecha del envio ^FS
^FT680,570^ACB,18,10^FDFecha Impresion:^FS
^FT680,360^AFB,26,13^FN2^FA8^FS
^FX Origen, Poblacion Destino ^FS
^FT160,580^FB250,2^AFB,26,13^FN3^FA14 ^FS
^FT160,290^FB250,2^AFB,26,13^FN4^FA17^FS
^FX Delegacion Destino ^FS
^FT270,570^AGB,120,40^FN5^FA11^FS
^FX Reembolso ^FS
^FT750,560^ADB,36,30^FN6^FA1^FS
^FX Asegurado ^FS
^FT750,450^ADB,36,30^FN7^FA1^FS
^FX Comprobante de Entrega ^FS
^FT750,350^ADB,36,30^FN8^FA1^FS
^FX Libro Control ^FS
^FT750,280^ADB,36,30^FN9^FA1^FS
^FX Tipo Pago ^FS
^FT643,360^AFB,52,13^FN10^FA9^FS
^FX Camion, Zona Carga ^FS
^FT720,160^AGB,140,40^FN11^FA3^FS
^FX Datos del remitente ^FS
^FX CCC Ordenante o Remitente^FS
^FO40,615^FB565,1300^ADB,32,18^FN12^FA25^FS
^FX Direccion Remitente^FS
^FO150,615^FB565,1150^ADB,26,14^FN13^FA48^FS
^FX Telefono Remitente^FS
^FT250,1190^ACB,18,10^FD^FX Tlf Remitente:^FS
^FT250,1000^ADB,26,14^FN14^FA48^FS
^FX Codigo postal y poblacion de remitente^FS
^FO190,615^FB565,1150^ADB,26,14^FN15^FA48^FS

^FX Datos del destinatario ^FS
^FX Destinatario Ocultar ^FS
^FO280,615^FB565,1150^ADB,24,14^FN16^FA50^FS
^FO305,615^FB565,1150^ADB,24,14^FN38^FA50^FS
^FX Destinatario Ocultar ^FS

^FX Nombre Destinatario TIENDA ^FS
^FO350,615^FB565,1150^ADB,24,14^FN37^FA50^FS

^FX Direccion^FS
^FO390,615^FB565,1150^ADB,18,13^FN17^FA48^FS
^FO415,615^FB565,1150^ADB,18,13^FN18^FA48^FS
^FX Codigo postal y población ^FS
^FO450,615^FB565,1150^ADB,39,13^FN19^FA48^FS
^FXTelefono^FS
^FT540,1190^ACB,18,10^FD^FX Tlf:^FS
^FT540,1120^ADB,18,13^FN21^FA14^FS
^FX Persona de contacto^FS
^FT570,1190^ACB,18,10^FDAtt:^FS
^FT570,1140^ADB,18,13^FN23FA42^FS

^FT510,760^ADB,18,10^FN20^FA35^FS

^FX Referencia Expedicion, Referencia Bulto ^FS
^FX Referrencia de Expedicion^FS
^FT620,1190^ADB,26,14^FDRef.Exp:^FS
^FT620,1070^ADB,27,14^FN24^FA25^FS

^FX Referencia de Bulto^FS
^FT685,1190^ADB,24,12^FDRef.Bul:^FS
^FT685,1070^ADB,27,14^FN25^FA25^FS

^FX Observaciones ^FS
^FT750,1190^ADB,26,13^FDObs:^FS
^FO740,575^FB565,1150,5^ABB,16,10^FN27^FA43^FS

^FX Frio label ^FS
^FT800,1190^^ADB,27,14^FN39^FA25^FS

^FT250,800^ACB,18,10^FDCCC:^FS
^FT250,750^ADB,26,14^FN28^FA43^FS

^FT770,1070^ADB,26,13^FN29^FA43^FS
^FX Codigo de Barras ^FS
^FT490,620^B2B,200,N,N,N^FN30^FA14^FS
^FX Numero ECB ^FS
^FT535,520^AEB,28,15^FN31^FA25^FS
^FX Bulto  ^FS
^FT613,570^ADB,18,13^FDBultos:^FS
^FT613,480^ADB,26,13^FN32^FA10^FS
^FX Peso Bulto ^FS
^FT643,570^ADB,18,13^FDPeso  :^FS
^FT643,480^ADB,26,13^FN33^FA10^FS
^FT565,1090^ADB,26,14^FN34^FA15^FS
^FT285,580^AGB,120,40^FN35^FA11^FS
^FX Codigo ^FS
^FT100,555^ACB,26,13^FN36^FA8^FS
^XZ
^^XA^XFSEURCPT1.001^FS
^FN1^FDS-24/ESTD^FS
^FN2^FD06/06/26^FS
^FN3^FDde ZARAGOZA^FS
^FN4^FDa LUGO^FS
^FN5^FD603 LUGO^FS
^FN6^FD^FS
^FN7^FD^FS
^FN8^FD^FS
^FN9^FD^FS
^FN10^FDP.PAGADOS^FS
^FN11^FD11^FS
^FN12^FDSEUR PRUEBAS DE ENVÍO^FS
^FN13^FDPLAZA DE NUESTRA SEÑORA DEL PILAR 1 SEUR^FS
^FN15^FD50003 ZARAGOZA - ES^FS
^FN16^FDDSKFSKDLF^FS
^FN17^FDAVDA AMERICAS 33 ^FS
^FN18^FD^FS
^FN19^FD27004 LUGO - ES^FS
^FN22^FD^FS
^FN23^FDDSKFSKDLF^FS
^FN24^FD3214324^FS
^FN25^FD5345345/1^FS
^FN26^FD^FS
^FN27^FD^FS
^FN28^FD77017-8^FS
^FN30^FD08270148896062^FS
^FN31^FD08 270 1 4889606 2^FS
^FN32^FD1/1^FS
^FN33^FD1.0^FS
^FN34^FD^FS
^FN35^FD^FS
^FN36^FD^FS
^FN37^FD^FS
^FN38^FD^FS
^FN39^FD^FS
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
