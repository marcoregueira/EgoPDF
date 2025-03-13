using Ego.PDF;

namespace Ego.Pdf.Zpl;

public class PdfZpl
{
    public FPdf Pdf { get; }
    public int Thickness { get; private set; } = 1;

    private Dictionary<string, Action<FPdf, string>> Tokens { get; set; } = new();

    public double TopX { get; private set; } = 0;
    public double TopY { get; private set; } = 0;
    public double X { get; private set; }
    public double Y { get; private set; }
    public string Alignment { get; private set; }
    public int Dpi { get; private set; }
    public string Orientation { get; private set; } = "N";
    public string Font { get; private set; } = "C";
    public string DefaultFont { get; private set; }
    public int DefaultFontH { get; private set; }
    public string MonospaceFont { get; private set; }
    public string VariableFont { get; private set; }

    public PdfZpl(FPdf document, int dpi = 300)
    {
        this.Pdf = document;
        this.Orientation = "N";
        this.Dpi = dpi;

        // Format Commands
        Tokens["^LH"] = Unimp_LabelHome;
        Tokens["^LL"] = Unimp_LabelLength;
        Tokens["^LR"] = Unimp_LabelReverse;
        Tokens["^LS"] = Unimp_LabelShift;
        Tokens["^JM"] = Unimp_SetDotsMM;
        Tokens["^PM"] = Unimp_MirrorImage;
        Tokens["^PO"] = Unimp_PrintOrientation;
        Tokens["^PF"] = Unimp_SlewDotRows;

        //^FO 50, 60 ^A 0, 40 ^FD World's Best Griddle ^FS
        Tokens["^XA"] = (pdf, _) => { pdf.X = 0; pdf.Y = 0; };
        Tokens["^XZ"] = (pdf, _) => { pdf.Ln(); };
        Tokens["^FO"] = SetLocation;
        Tokens["^AA"] = SetFont;
        Tokens["^AB"] = SetFont;
        Tokens["^AC"] = SetFont;
        Tokens["^AD"] = SetFont;
        Tokens["^AE"] = SetFont;
        Tokens["^AF"] = SetFont;
        Tokens["^AG"] = SetFont;
        Tokens["^AH"] = SetFont;
        Tokens["^AO"] = SetFont;
        //Tokens["^AGS"] = SetFont;
        Tokens["^AP"] = SetFont;
        Tokens["^AQ"] = SetFont;
        Tokens["^AR"] = SetFont;
        Tokens["^AS"] = SetFont;
        Tokens["^AT"] = SetFont;
        Tokens["^AU"] = SetFont;
        Tokens["^AV"] = SetFont;
        Tokens["^CF"] = SetDefaultFont;
        Tokens["^FD"] = WriteText; // Until next ^FS
        Tokens["^FH"] = EscapeCharacter; // Until next ^FS
        Tokens["^FW"] = SetOrientation; // Until next ^FS

        // Barcodes
        //^FO 60, 120 ^ BY 3 ^ BC , 60, , , , A ^ FD 1234ABC ^ FS
        Tokens["^BY"] = Voidf; //Set bar code field defaults
        Tokens["^BC"] = Voidf; //Code 128 bar code
        Tokens["^BO"] = Voidf; //Aztec bar code
        Tokens["^BQ"] = QrBarcode; //QR Code bar code
        Tokens["^BX"] = Voidf; //Data Matrix bar code
        Tokens["^B3"] = Voidf; //Code 39 bar code

        //Tokens["^B8"] = Voidf; //EAN-8 bar code
        //Tokens["^B9"] = Voidf; //EAN-13 bar code
        //Tokens["^B1"] = Voidf; //UPC-E bar code
        //Tokens["^B2"] = Voidf; //UPC-2 bar code
        //Tokens["^B7"] = Voidf; //UPC-5 bar code
        //Tokens["^B4"] = Voidf; //UPC-E1 bar code
        //Tokens["^B5"] = Voidf; //UPC-E1 bar code
        //Tokens["^BE"] = Voidf; //EAN-8 bar code
        //Tokens["^BF"] = Voidf; //EAN-13 bar code
        //Tokens["^BG"] = Voidf; //EAN-8 bar code
        //Tokens["^BH"] = Voidf; //EAN-13 bar code
        //Tokens["^BI"] = Voidf; //Interleaved 2 of 5 bar code
        //Tokens["^BJ"] = Voidf; //Standard 2 of 5 bar code
        //Tokens["^BK"] = Voidf; //Industrial 2 of 5 bar code
        //Tokens["^BL"] = Voidf; //Code 39 bar code
        //Tokens["^BN"] = Voidf; //Code 39 bar code
        //Tokens["^BO"] = Voidf; //Code 39 bar code
        //Tokens["^BP"] = Voidf; //Postnet bar code
        //Tokens["^BR"] = Voidf; //Micro QR Code bar code
        //Tokens["^BS"] = Voidf; //PDF417 bar code
        //Tokens["^BT"] = Voidf; //MaxiCode bar code
        //Tokens["^BU"] = Voidf; //


        // Images
        Tokens["^GF"] = Voidf; //Draw image
        Tokens["^XG"] = Voidf; //Graphic field
        Tokens["^DG"] = Voidf; //Download graphics
        Tokens["^DY"] = Voidf; //Download objects
        Tokens["^PM"] = Voidf; //Mirror label
        Tokens["^PO"] = Voidf; //Mirror label vertically


        // Text
        Tokens["^CI"] = CharacterSet; //Change character set
        Tokens["^GB"] = RectangleBox;
        Tokens["^FR"] = FieldReverse;

        Tokens["^GC"] = Circle;
        Tokens["^GE"] = Ellipse;
        Tokens["^GD"] = Diagonal;

        // not implemented
        Tokens["^FS"] = FinishField; //Finish field // 
        Tokens["^FX"] = Voidf; //Comments // Doesn't require ^FS and affects the entaire line   
        Tokens["^DU"] = Voidf; //upload font

        Tokens["^IS"] = ThrowNotImplemented; //Save format
        Tokens["^IL"] = ThrowNotImplemented; //Recall format
    }

    private void ThrowNotImplemented(FPdf pdf, string arg2)
    {
        throw new NotImplementedException(arg2 + " token not implemented");
    }

    private void Unimp_SlewDotRows(FPdf pdf, string arg2)
    {
        throw new NotImplementedException();
    }

    private void Unimp_PrintOrientation(FPdf pdf, string arg2)
    {
        throw new NotImplementedException();
    }

    private void Unimp_MirrorImage(FPdf pdf, string arg2)
    {
        throw new NotImplementedException();
    }

    private void Unimp_SetDotsMM(FPdf pdf, string arg2)
    {
        throw new NotImplementedException();
    }

    private void Unimp_LabelShift(FPdf pdf, string arg2)
    {
        throw new NotImplementedException();
    }

    private void Unimp_LabelReverse(FPdf pdf, string arg2)
    {
        throw new NotImplementedException();
    }

    private void Unimp_LabelLength(FPdf pdf, string arg2)
    {
        throw new NotImplementedException();
    }

    private void Unimp_LabelHome(FPdf pdf, string arg2)
    {
        arg2 = arg2.Substring(3);
        var parts = arg2.Split(',');
        if (parts.Length != 2) return;
        this.TopX = parts.ToMilimeters(0, 0, Dpi);
        this.TopY = parts.ToMilimeters(1, 0, Dpi);
    }

    private void SetOrientation(FPdf pdf, string arg2)
    {
        var orientation = arg2.Substring(3);
        this.Orientation = orientation;
    }

    private void FinishField(FPdf pdf, string arg2)
    {
        X = 0;
        Y = 0;
        // reset escape character
    }

    public void Print(string zpl)
    {
        var reader = new StringReader(zpl);
        var line = reader.ReadLine();
        while (line != null)
        {
            line = line.Trim();
            var parts = line.Split(new[] { '^' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                line = reader.ReadLine();
                continue;
            }
            foreach (var part in parts)
            {
                var command = "^" + part;
                var token = command.Substring(0, Math.Min(3, command.Length));
                if (Tokens.ContainsKey(token))
                {
                    try
                    {

                        Tokens[token](Pdf, command);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(command, ex);
                    }

                }
            }

            line = reader.ReadLine();
        }
    }

    private void QrBarcode(FPdf pdf, string arg2)
    {

    }

    private void Diagonal(FPdf pdf, string arg2)
    {
        throw new NotImplementedException();
    }

    private void Ellipse(FPdf pdf, string arg2)
    {
        throw new NotImplementedException();
    }

    private void Circle(FPdf pdf, string arg2)
    {
        // ^GC diameter, thickness, color [example]
        // ^GE width, height, thickness, color [example]
        // ^GD width, height, thickness, color, orientation [example]
        // ^GB width, height, thickness, color, rounding [example]
    }

    private void FieldReverse(FPdf pdf, string arg2)
    {
        // Makes the field reverse image // not sure I can do this
    }

    private void EscapeCharacter(FPdf pdf, string arg2)
    {
        /*
            ^XA
            ^FX FH command parameters:
            ^FX - escape character
            ^CI28
            ^FO25,25^A0,40^FH_^FDone: _5E^FS
            ^FO25,75^A0,40^FH_^FDtwo: _7E^FS
            ^FO25,125^A0,40^FH_^FDthree: _C2_BF?^FS
            ^XZ             
         */

        // Sets the escape character to encode hexadecimal characters with the ^FH command
    }

    private void SetDefaultFont(FPdf pdf, string arg2)
    {
        /*
            ^CF fontName, height, width
            Sets the default font. The default font is used by all subsequent text fields which do not specify a font using the ^A or ^A@ commands.
            Parameters:
            fontName: The name of the new default font. Font names are either a capital letter (A-Z) or a number (0-9). The default value is the previously configured value, or A if no value has been set.
            height: The height of the new default font, in dots. The default value depends on the font selected.
            width: The width of the new default font, in dots. The default value depends on the font selected.
            Example (common usage): ^CF0,50
            Example (full usage): ^CF0,50,50          
        */

        this.Font = arg2[3].ToString();
        this.DefaultFont = arg2[2].ToString();
        var body = arg2.Remove(0, 5);
        var parts = body.Split(',');
        if (parts.Length > 3) return;

        this.Thickness = parts.ToInt(0, this.Thickness);
        this.DefaultFontH = parts.ToInt(2, DefaultFontH);
        pdf.SetFontSize(Thickness / Dpi * 100);
    }

    private void CharacterSet(FPdf pdf, string arg2)
    {
        // 28 = UTF-8
    }

    private void RectangleBox(FPdf pdf, string arg2)
    {
        //The third and final field draws a box around the label using 3 commands: ^FO, ^GB and ^ FS.
        //^ FO 25, 25 ^ GB 380, 200, 2 ^ FS
        //width, height, thickness, color, rounding

        arg2 = arg2.Substring(3);
        var parts = arg2.Split(',');

        var width = parts.ToMilimeters(0, Thickness, Dpi);
        var height = parts.ToMilimeters(1, Thickness, Dpi);
        var thickness = parts.ToMilimeters(3, Thickness, Dpi); ;
        var color = parts.Length > 3 && !string.IsNullOrEmpty(parts[3]) ? parts[3] : "B";
        var rounding = parts.Length > 4 ? int.Parse(parts[4]) : Thickness;

        //pdf.SetXY(PDFX.ToMilimeters(Dpi) + this.TopX, Y.ToMilimeters(Dpi) + this.TopY);
        pdf.BoxedText(width, height, height, "", "1", 5, PDF.Data.AlignEnum.Left, false);
    }

    private void Voidf(FPdf pdf, string arg2)
    {
        //throw new NotImplementedException(arg2);
    }

    private void WriteText(FPdf pdf, string arg2)
    {

        var text = arg2.Substring(3);
        var fontsize = GetFontSize(this.Font, Dpi);
        pdf.SavePos();
        //pdf.SetXY(this.X.ToMilimeters(Dpi), this.Y.ToMilimeters(Dpi));
        var fontPoints = (Convert.ToDouble(Thickness) / Dpi) * 25.4 * 2.54;
        pdf.SetFontSize(fontPoints);


        // 2,54 mm = 1 inch

        if (this.Font == "0")
        {
            pdf.SetFont(this.VariableFont);
            pdf.Text(pdf.X, pdf.Y, text);
            return;
        }

        pdf.SetFont(this.MonospaceFont);
        var charw = Convert.ToDouble(fontsize.InW * 1000).ToMilimeters(Dpi);
        //var charw = Math.Ceiling(Convert.ToDouble(Thickness) / fontsize.DotsW) + 1;
        //charw = (Convert.ToDouble(charw) / Dpi) * 25.4 * 25.4;
        foreach (var c in text)
        {
            pdf.SetFontSize(fontPoints);
            if (c == '^')
            {
                // escape character
            }

            pdf.Text(pdf.X, pdf.Y, c.ToString());
            pdf.X += charw;
        }
        pdf.GetPos();
        //pdf.Text(this.X, this.Y, arg2);
    }

    private void SetFont(FPdf pdf, string arg2)
    {

        /*
            ^ADN
            1.Alter the numbers after the ^ADN,x,x command.
            • 18,10 is the smallest size you can make the D font.
            • The first number is the height of the font in dots.The second number is the width in dots.
            • You can use direct multiples up to ten times that size as a maximum.
            180,100 is the largest you can make the D font.
            • 25,18 would not be a valid size. The printer rounds to the next recognizable s
        */

        this.Font = arg2[2].ToString();
        var body = arg2.Remove(0, 2);
        var parts = body.Split(',');
        if (parts.Length != 3) return;

        var orientation = parts.ToString(0, this.Orientation);// (N/R/I/B) and was trimmed to one character
        Thickness = parts.ToInt(1, Thickness);
        var height = parts.ToInt(2, Thickness);
        pdf.SetFontSize(Thickness / Dpi * 100);
    }

    private void SetLocation(FPdf pdf, string body)
    {
        body = body.Substring(3);
        var parts = body.Split(',');
        var X = parts.ToMilimeters(0, 0, Dpi);
        var Y = parts.ToMilimeters(1, 0, Dpi);
        pdf.SetXY(this.TopX + X, this.TopY + Y);
        pdf.SetXY(X, this.TopY + Y);

        this.Alignment = parts.ToString(2, this.Alignment); //valid values are 0(left alignment), 1(right alignment), and 2(automatic alignment based on the direction of the field data text)
    }
    public void PrintToken()
    {
        Pdf.X = this.TopX;
        Pdf.Y = this.TopY;
    }

    public CharSize GetFontSize(string font, int dpi)
    {
        //if (dpi == 300)
        {
            /*
                A 9 , 5 U-L-D 0.030 , 0.020 50.8 0.75 , 0.50 2.02
                B 11 , 7 U 0.036 , 0.030 33.8 0.91 , 0.75 1.32
                C, D 18 , 10 U-L-D 0.059 , 0.040 25.4 1.50 , 1.00 1.00
                E 42 , 20 OCR-B 0.138 , 0.085 23.4 1.75 , 1.08 0.92
                F 26 , 13 U-L-D 0.085 , 0.053 19.06 2.16 , 1.34 0.74
                G 60 , 40 U-L-D 0.197 , 0.158 6.36 5.00 , 4.00 0.25
                H 34 , 22 OCR-A 0.111 , 0.098 10.20 2.81 , 2.48 0.40
                GS 24 , 24 SYMBOL 0.079 , 0.079 12.70 1.99 , 1.99 0.52
                P 20 , 18 U-L-D 0.067 , 0.060 N/A 1.69 , 1.52 N/A
                Q 28 , 24 U-L-D 0.093 , 0.080 N/A 2.37 , 2.03 N/A
                R 35 , 31 U-L-D 0.117 , 0.103 N/A 2.96 , 2.62 N/A
                S 40 , 35 U-L-D 0.133 , 0.177 N/A 3.39 , 2.96 N/A
                T 48 , 42 U-L-D 0.160 , 0.140 N/A 4.06 , 3.56 N/A
                U 59 , 53 U-L-D 0.197 , 0.177 N/A 5.00 , 4.49 N/A
                V 80 , 71 U-L-D 0.267 , 0.237 N/A 6.77 , 6.01 N/             
             */

            var sizes = new Dictionary<string, CharSize>()
            {
                { "A", new CharSize( "A", 9, 5, 0.030, 0.020, 50.8, 0.75, 0.50, 2.02 ) },
                { "B", new CharSize( "B", 11, 7, 0.036, 0.030, 33.8, 0.91, 0.75, 1.32 ) },
                { "C", new CharSize( "C", 18, 10, 0.059, 0.040, 25.4, 1.50, 1.00, 1.00 ) },
                { "D", new CharSize( "D", 18, 10, 0.059, 0.040, 25.4, 1.50, 1.00, 1.00 ) },
                { "E", new CharSize( "E", 42, 20, 0.138, 0.085, 23.4, 1.75, 1.08, 0.92 ) },
                { "F", new CharSize( "F", 26, 13, 0.085, 0.053, 19.06, 2.16, 1.34, 0.74 ) },
                { "G", new CharSize( "G", 60, 40, 0.197, 0.158, 6.36, 5.00, 4.00, 0.25 ) },
                { "H", new CharSize( "H", 34, 22, 0.111, 0.098, 10.20, 2.81, 2.48, 0.40 ) },
                { "GS",new CharSize( "GS", 24, 24, 0.079, 0.079, 12.70, 1.99, 1.99, 0.52 ) },
                { "P", new CharSize( "P", 20, 18, 0.067, 0.060, 0, 1.69, 1.52, 0 ) },
                //{ "R", new CharSize( "Q", 28, 24, 0.093, 0.080, 0, 2.37, 2,0) },
                //{ "S", new CharSize( "Q", 0, 0, 0.093, 0.080, 0, 2.37, 2,0) }, // INCOMPLETA
                //{ "T", new CharSize( "Q", 0, 0, 0.093, 0.080, 0, 2.37, 2,0) }, // INCOMPLETA
                //{ "U", new CharSize( "Q", 0, 0, 0.093, 0.080, 0, 2.37, 2,0) }, // INCOMPLETA
                //{ "V", new CharSize( "Q", 0, 0, 0.093, 0.080, 0, 2.37, 2,0) }, // INCOMPLETA
                //{ "0", new CharSize( "Q", 0, 0, 0.093, 0.080, 0, 2.37, 2,0) }, // INCOMPLETA
            };

            if (sizes.ContainsKey(font)) return sizes[font];
            return sizes[DefaultFont];
        }
    }

    public void SetVariableFont(string variableFont)
    {
        this.VariableFont = variableFont;
    }

    public void SetMonospaceFont(string monospaceFont)
    {
        this.MonospaceFont = monospaceFont;
    }

    public class CharSize
    {
        public CharSize(string FontName, int DotsH, int DotsW, double InH, double InW, double InChars, double MmH, double MmW, double MmChars)
        {
            this.FontName = FontName;
            this.DotsH = DotsH;
            this.DotsW = DotsW;
            this.InH = InH;
            this.InW = InW;
            this.InChars = InChars;
            this.MmH = MmH;
            this.MmW = MmW;
            this.MmChars = MmChars;
        }

        public string FontName { get; }
        public int DotsH { get; }
        public int DotsW { get; }
        public double InH { get; }
        public double InW { get; }
        public double InChars { get; }
        public double MmH { get; }
        public double MmW { get; }
        public double MmChars { get; }
    }
}
