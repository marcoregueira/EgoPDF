/*
MIT License

    Copyright (c) 2017-2026 Marco Antonio Regueira

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.

NOTICE:

    The EgoPDF.Generator package is an evolution of an automated C# port
    of FPDF, originally written by Olivier Plathey:

        FPDF
        http://www.fpdf.org/
        Copyright (c) Olivier Plathey

        FPDF is freeware. There is no usage restriction. You may embed it
        freely in your application (commercial or not), with or without
        modifications. Reference to FPDF in the resulting PDF is not
        mandatory.

    This acknowledgment is included for transparency; FPDF's terms do not
    require attribution. Substantial portions of EgoPDF.Generator have
    been rewritten (Skia-based font embedding, font descriptor
    correctness, modern target frameworks, packaging) and are subject to
    the MIT License above.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using Ego.PDF.Data;
using Ego.PDF.Font;
using Ego.PDF.PHP;
using Ego.PDF.Support;

using Microsoft.Xna.Framework;
using static Ego.PDF.Printf.SprintfTools;
using SkiaSharp;

namespace Ego.PDF;

public class FPdf: IDisposable
{
    public static readonly Encoding PrivateEncoding = CodePagesEncodingProvider.Instance.GetEncoding(1252);
    public readonly string FpdfVersion = "1.7";
    public bool ColorFlag;
    public LayoutEnum LayoutMode;
    public double Ws;
    public ZoomEnum ZoomMode;
    public decimal ZoomValue = 1;
    public OrderedMap CMaps { get; set; } = new OrderedMap();
    public OrderedMap Encodings { get; set; } = new OrderedMap();

    public FPdf() : this(null)
    {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="orientation"></param>
    /// <param name="unit"></param>
    /// <param name="pageSize"></param>
    /// <param name="filePath">If provided, internal storage will be sent to a file, else a memorystream will be used</param>
    public FPdf(PageOrientation orientation, UnitEnum unit, PageSizeEnum pageSize, string filePath)
    {
        pageSize = pageSize == PageSizeEnum.Default ? PageSizeEnum.A4 : pageSize;
        unit = unit == UnitEnum.Default ? UnitEnum.Milimeter : unit;
        pageSize = pageSize == PageSizeEnum.Default ? PageSizeEnum.A4 : pageSize;

        // Initialization of properties
        Page = 0;
        ObjectCount = 2;

        if (Directory.Exists(filePath))
        {
            filePath = Path.Combine(filePath, Guid.NewGuid().ToString() + ".pdf");
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Buffer = new StreamWriter(new MemoryStream(), PrivateEncoding, 2048, true);
        }
        else
        {
            Buffer = new StreamWriter(File.Create(filePath), PrivateEncoding);
        }
        // AutoFlush=true uniformly across both branches. Without it on the
        // file-path branch, the StreamWriter's internal buffer swallows the
        // trailing xref / trailer / startxref / %%EOF written by Close(),
        // and a caller that reads Buffer.BaseStream after Close() ends up
        // with a truncated PDF that finishes at the last "endobj".
        Buffer.AutoFlush = true;

        Offsets = new Dictionary<int, long>();
        Pages = new Dictionary<int, Page>();
        PageSizes = new Dictionary<int, Dimensions>();
        State = 0;
        FontFiles = new Dictionary<string, FontDefinition>();
        Fonts = new Dictionary<string, FontDefinition>();
        Diffs = new OrderedMap();
        Images = new Dictionary<string, ImageInfo>();
        Links = new List<LinkDataInternal>();
        InHeader = false;
        InFooter = false;
        Lasth = 0;
        FontFamily = "";
        FontStyle = "";
        FontSizePt = 12;
        Underline = false;
        DrawColor = "0 G";
        FillColor = "0 g";
        TextColor = "0 g";
        ColorFlag = false;
        Ws = 0;
        // Font path
        //TODO: SET A DEFAULT FONT PATH
        FpdfFontpath = "";
        Fontpath = FpdfFontpath;

        // Core fonts
        CoreFonts = new List<string> { "courier", "helvetica", "times", "symbol", "zapfdingbats" };
        // Scale factor
        k = 72 / 25.4; // unidades en milímetros

        // Page sizes
        StdPageSizes = new List<PageSize>
        {
            new PageSize("a3", 841.89, 1190.55),
            new PageSize("a4", 595.28, 841.89),
            new PageSize("a5", 420.94, 595.28),
            new PageSize("legal", 612, 792),
            new PageSize("letter", 612, 1008),
        };

        SetUnitConverionFactor(unit);

        Dimensions size = GetPageSize(pageSize);
        DefPageSize = size;
        CurrentPageSize = size;

        // Page orientation
        DefOrientation = orientation;
        CurrentOrientation = DefOrientation;

        if (orientation == PageOrientation.Portrait)
        {
            DefOrientation = PageOrientation.Portrait;
            W = size.Width;
            H = size.Heigth;
        }
        else if (orientation == PageOrientation.Landscape)
        {
            DefOrientation = PageOrientation.Landscape;
            W = size.Heigth;
            H = size.Width;
        }
        else
        {
            Error("Incorrect orientation: " + orientation);
        }

        WPt = W * k;
        HPt = H * k;

        // Page margins (1 cm)
        double margin = 28.35 / k;
        SetMargins(margin, margin);

        // Interior cell margin (1 mm)
        CMargin = margin / 10;

        // Line width (0.2 mm)
        LineWidth = .567 / k;

        // Automatic page break
        SetAutoPageBreak(true, 2 * margin);

        // Default display mode
        SetDisplayMode(ZoomEnum.Default, LayoutEnum.Default);

        // Enable compression
        //TODO: PONER TRUE
        SetCompression(false);

        // Set default PDF version number
        PdfVersion = "1.3";
        DefaultCulture = CultureInfo.InvariantCulture;
        PageNumberFormat = "d";
    }

    public void SetUnitConverionFactor(UnitEnum unit, double v = 72)
    {
        this.Unit = unit;
        if (unit == UnitEnum.Point)
        {
            k = 1;
            k = k / (v / 72);
        }
        else if (unit == UnitEnum.Milimeter)
        {
            k = 72 / 25.4;
        }
        else if (unit == UnitEnum.Centimeter)
        {
            k = 72 / 2.54;
        }
        else if (unit == UnitEnum.Inch)
        {
            k = 72;
        }
        else
        {
            Error("Incorrect unit: " + unit);
        }

    }

    public FPdf(string path) : this(PageOrientation.Portrait, UnitEnum.Milimeter, PageSizeEnum.A4, path)
    {
    }


    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Current object number
    /// <remarks>Previously named "n"</remarks>
    /// </summary>
    public int ObjectCount { get; set; }

    /// <summary>
    ///     array of object offsets
    /// </summary>
    public Dictionary<int, long> Offsets { get; set; }

    /// <summary>
    ///     buffer holding in-memory PDF
    /// </summary>
    public StreamWriter Buffer { get; set; }

    /// <summary>
    ///     array containing pages
    /// </summary>
    public Dictionary<int, Page> Pages { get; set; }

    /// <summary>
    ///     current document state
    /// </summary>
    public int State { get; set; }

    /// <summary>
    ///     compression flag
    /// </summary>
    public bool Compress { get; set; }

    /// <summary>
    ///     scale factor (number of points in user unit)
    /// </summary>
    public double k { get; set; }

    /// <summary>
    ///     default orientation
    /// </summary>
    public PageOrientation DefOrientation { get; set; }

    /// <summary>
    ///     current orientation
    /// </summary>
    public PageOrientation CurrentOrientation { get; set; }

    /// <summary>
    ///     standard page sizes
    /// </summary>
    public List<PageSize> StdPageSizes { get; set; }

    /// <summary>
    ///     default page size
    /// </summary>
    public Dimensions DefPageSize { get; set; }

    /// <summary>
    ///     current page size
    /// </summary>
    public Dimensions CurrentPageSize { get; set; }

    /// <summary>
    ///     used for pages with non default sizes or orientations
    /// </summary>
    public Dictionary<int, Dimensions> PageSizes { get; set; }

    /// <summary>
    ///     Current page width in points
    /// </summary>
    public double WPt { get; set; }

    /// <summary>
    ///     Current page height in points
    /// </summary>
    public double HPt { get; set; }

    /// <summary>
    /// Page width in user units
    /// </summary>
    public double W { get; set; }

    /// <summary>
    /// Page height in user units
    /// </summary>
    public double H { get; set; }

    /// <summary>
    ///     left margin
    /// </summary>
    public double LeftMargin { get; set; }

    /// <summary>
    ///     top margin
    /// </summary>
    public double TopMargin { get; set; }

    /// <summary>
    ///     right margin
    /// </summary>
    public double RightMargin { get; set; }

    /// <summary>
    ///     page break margin
    /// </summary>
    public double PageBreakMargin { get; set; }

    public double CMargin { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Lasth { get; set; }
    public double LineWidth { get; set; }
    public List<string> CoreFonts { get; set; }
    private Dictionary<string, FontDefinition> Fonts { get; set; }
    private Dictionary<string, FontDefinition> FontFiles { get; set; }

    public Action OnFooter { get; set; }
    public FontScale FontScale { get; private set; }

    public OrderedMap Diffs { get; set; }
    public string FontFamily { get; set; }
    public string FontStyle { get; set; }
    public bool Underline { get; set; }
    public FontDefinition CurrentFont { get; set; }
    public double FontSizePt { get; private set; }
    public double FontSize { get; private set; }
    public string DrawColor { get; set; }
    public string FillColor { get; set; }
    public string TextColor { get; set; }
    public Dictionary<string, ImageInfo> Images { get; set; }
    public List<LinkDataInternal> Links { get; set; }
    public bool AutoPageBreak { get; set; }
    public double PageBreakTrigger { get; set; }
    public bool InHeader { get; set; }
    public double BelowHeaderY { get; set; }
    public bool InFooter { get; set; }
    public string Title { get; set; }
    public string Subject { get; set; }
    public string Author { get; set; }
    public string Keywords { get; set; }
    public string Creator { get; set; }
    public string AliasNbPagesRenamed { get; set; }
    public string PdfVersion { get; set; }
    public string FpdfFontpath { get; set; }

    public CultureInfo DefaultCulture { get; set; }
    public string PageNumberFormat { get; set; }

    private string Fontpath { get; set; }


    /// <summary>
    ///     El margen derecho es igual al izquierdo
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// </summary>
    public virtual void SetMargins(double left, double top)
    {
        SetMargins(left, top, left);
    }

    /// <summary>
    ///     Set left, top and right margins
    /// </summary>
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// <param name="right"></param>
    public virtual void SetMargins(double left, double top, double right)
    {
        LeftMargin = left;
        TopMargin = top;
        RightMargin = right;
    }

    public virtual void SetLeftMargin(double margin)
    {
        // Set left margin
        LeftMargin = margin;
        if (Page > 0 && X < margin)
        {
            X = margin;
        }
    }

    public virtual void SetTopMargin(double margin)
    {
        // Set top margin
        TopMargin = margin;
    }

    public virtual void SetRightMargin(double margin)
    {
        // Set right margin
        RightMargin = margin;
    }

    public virtual void SetAutoPageBreak(bool auto, double margin)
    {
        // Set auto page break mode and triggering margin
        AutoPageBreak = auto;
        PageBreakMargin = margin;
        PageBreakTrigger = TypeSupport.ToDouble(H) - margin;
    }

    /// <summary>
    ///     Set display mode in viewer
    /// </summary>
    /// <param name="zoom"></param>
    /// <param name="layout"></param>
    public virtual void SetDisplayMode(ZoomEnum zoom, LayoutEnum layout)
    {
        SetZoom(zoom);
        SetLayout(layout);
    }

    public virtual void SetDisplayMode(decimal zoomValue, LayoutEnum layout)
    {
        SetZoom(zoomValue);
        SetLayout(layout);
    }

    public virtual void SetZoom(ZoomEnum zoom)
    {
        if (zoom != ZoomEnum.Default)
            ZoomMode = zoom;
    }

    public virtual void SetZoom(decimal zoom)
    {
        ZoomMode = ZoomEnum.Custom;
        ZoomValue = zoom;
    }

    public virtual void SetLayout(LayoutEnum layout)
    {
        LayoutMode = layout;
    }

    /// <summary>
    ///     Set page compression
    /// </summary>
    /// <param name="compress"></param>
    public virtual void SetCompression(bool compress)
    {
        Compress = compress;
    }

    /// <summary>
    ///     Title of document
    /// </summary>
    /// <param name="title"></param>
    public virtual FPdf SetTitle(string title)
    {
        Title = title;
        return this;
    }

    /// <summary>
    ///     Subject of document
    /// </summary>
    /// <param name="subject"></param>
    public virtual void SetSubject(string subject)
    {
        Subject = subject;
    }

    /// <summary>
    ///     Keywords of document
    /// </summary>
    /// <param name="author"></param>
    public virtual FPdf SetAuthor(string author)
    {
        // Author of document
        Author = author;
        return this;
    }

    /// <summary>
    ///     Keywords of document
    /// </summary>
    /// <param name="keywords"></param>
    public virtual FPdf SetKeywords(string keywords)
    {
        Keywords = keywords;
        return this;
    }

    /// <summary>
    ///     Creator of document
    /// </summary>
    /// <param name="creator"></param>
    public virtual void SetCreator(string creator)
    {
        Creator = creator;
    }

    /// <summary>
    ///     Define an alias for total number of pages
    /// </summary>
    public virtual FPdf AliasNbPages()
    {
        AliasNbPages("{nb}");
        return this;
    }

    /// <summary>
    ///     Define an alias for total number of pages
    /// </summary>
    /// <param name="alias"></param>
    public virtual void AliasNbPages(string alias)
    {
        AliasNbPagesRenamed = alias;
    }

    /// <summary>
    ///     Fatal error
    /// </summary>
    /// <param name="msg"></param>
    public virtual void Error(string msg)
    {
        throw new InvalidOperationException(msg);
    }

    /// <summary>
    ///     Begin document
    /// </summary>
    public virtual void Open()
    {
        State = 1;
    }

    /// <summary>
    ///     Terminate document
    /// </summary>
    public virtual FPdf Close()
    {
        if (State == 3)
        {
            return this;
        }
        if (Page == 0)
        {
            AddPage(PageOrientation.Default, null);
        }
        // Page footer
        InFooter = true;
        Footer();
        InFooter = false;
        // Close page
        EndPage();
        // Close document
        EndDoc();
        return this;
    }

    public virtual void AddPage()
    {
        AddPage(PageOrientation.Default, DefPageSize);
    }

    public virtual void AddPage(PageOrientation orientation)
    {
        AddPage(orientation, PageSizeEnum.Default);
    }

    public virtual void AddPage(PageSizeEnum size)
    {
        AddPage(PageOrientation.Default, size);
    }

    public virtual void AddPage(PageOrientation orientation, PageSizeEnum pagesize)
    {
        Dimensions page = GetPageSize(pagesize);
        AddPage(orientation, page);
    }

    public virtual void AddPage(PageOrientation orientation, Dimensions size)
    {
        // Start a new page
        if (State == 0)
        {
            Open();
        }
        string family = FontFamily;
        string style = FontStyle + (Underline ? "U" : "");
        double fontsize = FontSizePt;
        double lw = LineWidth;
        string dc = DrawColor;
        string fc = FillColor;
        string tc = TextColor;
        bool cf = ColorFlag;
        if (Page > 0)
        {
            // Page footer
            InFooter = true;
            Footer();
            InFooter = false;
            // Close page
            EndPage();
        }
        // Start new page
        BeginPage(orientation, size);
        // Set line cap style to square
        Out("2 J");
        // Set line width
        LineWidth = lw;
        Out(sprintf("%.2F w", lw * k));

        // Set font
        if (TypeSupport.ToBoolean(family))
        {
            SetFont(family, style, fontsize);
        }
        // Set colors
        DrawColor = dc;
        if (TypeSupport.ToString(dc) != "0 G")
        {
            Out(dc);
        }
        FillColor = fc;
        if (TypeSupport.ToString(fc) != "0 g")
        {
            Out(fc);
        }
        TextColor = tc;
        ColorFlag = cf;
        // Page header
        InHeader = true;
        BelowHeaderY = TopMargin;
        Header();
        InHeader = false;
        // Restore line width
        if (LineWidth != lw)
        {
            LineWidth = lw;
            Out(sprintf("%.2F w", lw * k));
        }
        // Restore font
        if (TypeSupport.ToBoolean(family))
        {
            SetFont(family, style, fontsize);
        }
        // Restore colors
        //CONVERSION_WARNING: Converted Operator might not behave as expected. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1009.htm 
        if (DrawColor != dc)
        {
            DrawColor = dc;
            Out(dc);
        }
        //CONVERSION_WARNING: Converted Operator might not behave as expected. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1009.htm 
        if (FillColor != fc)
        {
            FillColor = fc;
            Out(fc);
        }
        TextColor = tc;
        ColorFlag = cf;
    }

    public virtual void Header()
    {
        // To be implemented in your own inherited class
    }

    public UnitEnum Unit { get; private set; }
    public virtual void Footer()
    {
        // To be extended in your own inherited class
        this.OnFooter?.Invoke();
    }

    public virtual int PageNo()
    {
        // Get current page number
        return Page;
    }

    public virtual void SetDrawColor(Color color)
    {
        SetDrawColor(color.R, color.G, color.B);
    }

    public virtual void SetDrawColor(int red, int? green, int? blue)
    {
        double r = red;
        double? g = green;
        double? b = blue;

        // Set color for all stroking operations
        if ((r == 0 && g == 0 && b == 0) || (!g.HasValue))
        {
            DrawColor = sprintf("%.3F G", r / 255);
        }
        else
        {
            DrawColor = sprintf("%.3F %.3F %.3F RG", r / 255, g / 255, b / 255);
        }
        if (Page > 0)
        {
            Out(DrawColor);
        }
    }

    public virtual void SetFillColor(int grey)
    {
        FillColor = sprintf("%.3F g", (double) grey / 255);
        ColorFlag = (FillColor != TextColor);
        if (Page > 0)
        {
            Out(FillColor);
        }
    }


    public virtual void SetFillColor(Color color)
    {
        SetFillColor(color.R, color.G, color.B);
    }

    public virtual void SetFillColor(int red, int green, int blue)
    {
        double r = red;
        double g = green;
        double b = blue;

        // Set color for all filling operations
        FillColor = sprintf("%.3F %.3F %.3F rg", r / 255, g / 255, b / 255);
        ColorFlag = (FillColor != TextColor);
        if (Page > 0)
        {
            Out(FillColor);
        }
    }

    public virtual void SetTextColor(int greyColor)
    {
        TextColor = sprintf("%.3F g", greyColor / 255);
        ColorFlag = (FillColor != TextColor);
    }

    public virtual void SetTextColor(Color color)
    {
        SetTextColor(color.R, color.G, color.B);
    }

    public virtual void SetTextColor(int red, int green, int blue)
    {
        double r = red;
        double g = green;
        double b = blue;
        TextColor = sprintf("%.3F %.3F %.3F rg", r / 255, g / 255, b / 255);
        ColorFlag = (FillColor != TextColor);
    }

    public virtual double GetStringWidth(string s)
    {
        // Fall back to the "A" width for any character whose glyph is
        // not present in the current font's metric table. This matches
        // what MultiCell already does internally and stops the common
        // crash where an em-dash, Greek letter or other off-Latin-1
        // codepoint reaches Cell with a right/center alignment.
        double w = 0;
        var text = TypeSupport.ToString(s);
        if (string.IsNullOrEmpty(text)) return 0;
        var widths = CurrentFont.Widths;
        double fallback = widths.TryGetValue("A", out var aw) ? aw : 500;
        foreach (var ch in text)
        {
            w += widths.TryGetValue(ch.ToString(), out var cw) ? cw : fallback;
        }
        return w * FontSize / 1000;
    }

    public virtual void SetLineWidth(double width)
    {
        // Set line width
        LineWidth = width;
        if (Page > 0)
        {
            Out(sprintf("%.2F w", width * k));
        }
    }

    public virtual void Line(double x1, double y1, double x2, double y2)
    {
        // Draw a line
        Out(sprintf("%.2F %.2F m %.2F %.2F l S",
            x1 * k,
            (H - y1) * k, x2 * k,
            (H - y2) * k));
    }

    public virtual void Rect(double x, double y, double w, double h, string style)
    {
        // Draw a rectangle
        string op;
        if (style == "F")
        {
            op = "f";
        }
        else if (style == "FD" || style == "DF")
        {
            op = "B";
        }
        else
        {
            op = "S";
        }
        Out(sprintf("%.2F %.2F %.2F %.2F re %s", x * k, (H - y) * k, w * k, (-h) * k, op));
    }

    public virtual void AddFont(string family, string style)
    {
        // Add a TrueType, OpenType or Type1 font
        int n;
        family = family.ToLower();
        style = style.ToUpper();
        if (style == "IB")
        {
            style = "BI";
        }

        string fontkey = family + style;
        if (Fonts.ContainsKey(fontkey))
        {
            return;
        }

        FontDefinition fontInfo = GetFontDefinition(fontkey);
        //var file = fontInfo.FontFile;
        //if (string.IsNullOrEmpty(file))
        //{
        //    file = TypeSupport.ToString(family.Replace(" ", "")) + style.ToLower();
        //}

        fontInfo.i = OrderedMap.CountElements(Fonts) + 1;

        if (fontInfo.diff != null)
        {
            // Search existing encodings
            n = Convert.ToInt32(Diffs.Search(fontInfo.diff));
            if (!Convert.ToBoolean(n))
            {
                n = OrderedMap.CountElements(Diffs) + 1;
                Diffs[ n ] = fontInfo.diff;
            }
            fontInfo.diffn = n;
        }

        if (!string.IsNullOrEmpty(fontInfo.FontFile))
        {
            // Embedded font
            // TODO ? SHOULD WE ASSIGN THE SAME FONT OBJECT INSTEAD OF CREATING A COPY?
            if (fontInfo.FontType == FontTypeEnum.TrueType)
            {
                FontFiles[ fontInfo.Name ] = new FontDefinition
                {
                    length1 = fontInfo.originalsize,
                    Name = fontInfo.Name,
                    FontFile = fontInfo.FontFile
                };
            }
            else
            {
                FontFiles[ fontInfo.Name ] = new FontDefinition
                {
                    length1 = fontInfo.size1,
                    length2 = fontInfo.size2,
                    Name = fontInfo.Name,
                    FontFile = fontInfo.FontFile
                };
            }
        }
        Fonts[ fontkey ] = fontInfo;
    }


    public virtual void SetFont(string family) => SetFont(family, string.Empty);

    public virtual void SetFont(string family, string style) => SetFont(family, style, 0);

    public virtual void SetFont(string family, double size, FontScale scale = null) => SetFont(family, string.Empty, size, scale);

    public virtual void SetFont(string family, string style, double size, FontScale scale = null)
    {
        // Select a font; size given in points
        if (string.IsNullOrEmpty(family))
        {
            family = FontFamily;
        }
        else
        {
            family = family.ToLower();
        }

        style = (style ?? string.Empty).ToUpper();
        if (style.Contains("U"))
        {
            Underline = true;
            style = style.Replace("U", string.Empty);
        }
        else
        {
            Underline = false;
        }

        if (style == "IB")
        {
            style = "BI";
        }

        if (size == 0)
        {
            size = FontSizePt;
        }
        // Test if font is already selected
        if (FontFamily == family && FontStyle == style && FontSizePt == size)
        {
            return;
        }
        // Test if font is already loaded
        var fontkey = family + style;
        if (!Fonts.ContainsKey(fontkey))
        {
            // Test if one of the core fonts
            if (family == "arial")
            {
                family = "helvetica";
            }
            if (CoreFonts.Contains(family))
            {
                if (family == "symbol" || family == "zapfdingbats")
                {
                    style = "";
                }
                fontkey = family + style;
                if (!(Fonts.ContainsKey(fontkey)))
                {
                    AddFont(family, style);
                }
            }
            else
            {
                Error("Undefined font: " + family + " " + style);
            }
        }
        // Select it
        FontFamily = family;
        FontStyle = style;
        FontSizePt = size;
        FontSize = size / k;
        FontScale = scale ?? FontScale.Default;
        CurrentFont = Fonts[ fontkey ];
        if (Page > 0)
        {
            Out(sprintf($"BT /F%d %.2F Tf ET", CurrentFont.i, FontSizePt));
        }
    }

    public virtual void SetFontSize(double size)
    {
        // Set font size in points
        if (FontSizePt == size)
        {
            return;
        }
        FontSizePt = size;
        FontSize = size / k;
        if (Page > 0)
        {
            Out(sprintf($"BT /F%d %.2F Tf ET", CurrentFont.i, FontSizePt));
        }
    }

    public virtual LinkDataInternal AddLink()
    {
        // Create a new internal virtual link
        var l = new LinkDataInternal();
        Links.Add(l);
        return l;
    }


    public void SetLink(LinkDataInternal link)
    {
        link.PageIndex = Page;
        link.Y = Y;
    }

    public LinkDataInternal SetLink(int link)
    {
        LinkDataInternal l = SetLink(link, 0, -1);
        return l;
    }

    public LinkDataInternal SetLink(int link, double y)
    {
        LinkDataInternal l = SetLink(link, y, -1);
        return l;
    }

    public LinkDataInternal SetLink(int link, double y, int page)
    {
        // Set destination of internal link
        if (y == -1)
        {
            y = (int) (Y);
        }
        if (page == -1)
        {
            page = Page;
        }
        var linkInternal = new LinkDataInternal(page, y);
        Links[ link ] = linkInternal;
        return linkInternal;
    }

    public virtual void Link(double x, double y, double w, double h, LinkData link)
    {
        Pages[ Page ].PageLinks.Add(new PageLink(x * k, HPt - y * k, w * k, h * k, link));
    }

    public virtual void Text(double x, double y, string txt)
    {
        // Output a string
        string s;
        if (FontScale.HasScale())
        {
            s = sprintf($"BT {FontScale}%.2F %.2F Tm (%s) Tj ET", x * k, (H - y) * k, Escape(txt));
        }
        else
        {
            s = sprintf($"BT %.2F %.2F Td (%s) Tj ET", x * k, (H - y) * k, Escape(txt));
        }
        if (Underline && TypeSupport.ToString(txt) != "")
        {
            s = TypeSupport.ToString(s) + " " + DoUnderline(x, y, txt);
        }
        if (ColorFlag)
        {
            s = "q " + TypeSupport.ToString(TextColor) + " " + TypeSupport.ToString(s) + " Q";
        }
        Out(s);
    }

    private string BuildTextMatrix(double sx, double sy, double thetaDegrees, double x, double y)
    {
        var radians = thetaDegrees * Math.PI / 180.0f;

        var a = sx * Math.Cos(radians);
        var b = sx * Math.Sin(radians);
        var c = -sy * Math.Sin(radians);
        var d = sy * Math.Cos(radians);
        var e = x;
        var f = y;

        return sprintf("%.2f %.2f %.2f %.2f %.2f %.2f Tm", a, b, c, d, e, f);
    }


    public void WriteRotatedText(double x, double y, double lineHeight, string rotation, string txt)
    {
        WriteRotatedText(x, y, lineHeight, rotation, txt, 0);
    }

    public void WriteRotatedTextZpl(double foX, double foY, double ascent, string rotation, string txt)
    {
        WriteRotatedTextZpl(foX, foY, ascent, rotation, txt, 0, false);
    }

    public void WriteRotatedTextZpl(double foX, double foY, double ascent, string rotation, string txt, double tracking)
    {
        WriteRotatedTextZpl(foX, foY, ascent, rotation, txt, tracking, false);
    }

    /// <summary>
    /// Place a rotated text field with ZPL ^FO semantics: (foX, foY) anchors the
    /// top-left of the ROTATED bounding box, not the baseline-start of the
    /// unrotated text. Computes textWidth internally via GetStringWidth so the
    /// "B" / "R" / "I" branches can offset the PDF text matrix correctly. For
    /// "N" the result is identical to <see cref="WriteRotatedText"/>; the
    /// difference shows up only when the field is rotated. Use this from ZPL
    /// rendering paths. For PostScript-style baseline-start anchoring keep
    /// <see cref="WriteRotatedText"/>.
    /// </summary>
    /// <param name="tracking">Extra inter-character advance, in PDF user units. 0 = font-native.</param>
    /// <param name="useTopLeftBboxAnchor">
    /// When true and rotation = "B" the (foX, foY) anchors the top-left of the
    /// rotated bounding box -- text extends DOWN from foY, still reading
    /// upward. Use this for ^FO + ^A?B without ^FB (the GLS courier label
    /// pattern). When false (the default) the FO point anchors the FIRST
    /// char's baseline-origin -- text extends UP from foY. Use that for
    /// ^FT + ^A?B and for per-line calls coming out of DrawFramed (where the
    /// field-level Origin is already absorbed into the line position).
    /// </param>
    public void WriteRotatedTextZpl(double foX, double foY, double ascent, string rotation, string txt, double tracking, bool useTopLeftBboxAnchor)
    {
        // textWidth covers the rotated bbox dimension parallel to the reading
        // direction. GetStringWidth returns the NATURAL advance; the PDF text
        // matrix then applies FontScale.ScaleX on top, so the rendered width
        // is textWidth * ScaleX. Bbox math has to use the rendered value, not
        // the natural one, or the FO point ends up offset by (ScaleX - 1) *
        // textWidth -- which is what pushed DESTINATARIO above the Y=130
        // separator on the GLS courier label (ScaleX=1.67 for ^ABB,10,10).
        var textWidth = GetStringWidth(txt);
        if (tracking > 0 && !string.IsNullOrEmpty(txt) && txt.Length > 1)
            textWidth += tracking * (txt.Length - 1);
        var renderedWidth = textWidth * this.FontScale.ScaleX;

        string result = rotation switch
        {
            // Baseline at (foX, foY + ascent) → glyph top lands at foY. Same as
            // the legacy WriteRotatedText for the normal-orientation path.
            "N" => BuildTextMatrix(this.FontScale.ScaleX, this.FontScale.ScaleY, 0,
                                    foX * k,
                                    (H - foY - ascent) * k),
            // 90° CW rotation. Chars advance downward; ascent extends to the
            // right of the baseline column. FO top-left = (baseline column, top
            // of first char). Rendered width grows downward from foY so the
            // base translation doesn't need it.
            "R" => BuildTextMatrix(this.FontScale.ScaleX, this.FontScale.ScaleY, 270,
                                    foX * k,
                                    (H - foY) * k),
            // 180° rotation. Chars advance leftward (rendered width subtracts
            // from x); ascent points downward. FO top-left = (left edge,
            // baseline level).
            "I" => BuildTextMatrix(this.FontScale.ScaleX, this.FontScale.ScaleY, 180,
                                    (foX + renderedWidth) * k,
                                    (H - foY) * k),
            // 90° CCW rotation (ZPL "read from bottom up"). Chars advance
            // upward; ascent extends to the left of the baseline column.
            // Default anchor is the FIRST char's baseline-origin (text
            // extends UP from foY) -- ^FT semantics and the SEUR vertical1
            // sample's per-line DrawFramed calls all expect this.
            // When useTopLeftBboxAnchor is true, foY anchors the TOP of the
            // rotated bbox instead -- text extends DOWN from foY, still
            // reading upward inside the strip. ^FO without ^FB on the GLS
            // courier label needs this so DESTINATARIO sits below foY=150
            // rather than climbing above the Y=130 separator.
            "B" => BuildTextMatrix(this.FontScale.ScaleX, this.FontScale.ScaleY, 90,
                                    (foX + ascent) * k,
                                    (H - foY - (useTopLeftBboxAnchor ? renderedWidth : 0)) * k),
            _ => throw new ArgumentException("Código de rotación no válido."),
        };

        var tcPrefix = tracking > 0 ? sprintf("%.3f Tc ", tracking * k) : "";
        var tcSuffix = tracking > 0 ? " 0 Tc" : "";
        var s = sprintf($"BT {tcPrefix}{result} (%s) Tj{tcSuffix} ET", Escape(txt));
        if (Underline && TypeSupport.ToString(txt) != "")
        {
            s = TypeSupport.ToString(s) + " " + DoUnderline(foX, foY, txt);
        }
        if (ColorFlag)
        {
            s = "q " + TypeSupport.ToString(TextColor) + " " + TypeSupport.ToString(s) + " Q";
        }
        Out(s);
    }

    /// <param name="tracking">Extra inter-character advance, in PDF user units. 0 = font-native.</param>
    public void WriteRotatedText(double x, double y, double lineHeight, string rotation, string txt, double tracking)
    {
        // ZPL ^FO places the top-left of the text box at (x, y); the PDF text
        // matrix places the baseline. Shift down by lineHeight (≈ font ascent)
        // so glyphs descend from the FO point instead of extending above it.
        string result = rotation switch
        {
            "N" => BuildTextMatrix(this.FontScale.ScaleX, this.FontScale.ScaleY, 0, x * k, (H - y - lineHeight) * k),// Normal
            "B" => BuildTextMatrix(this.FontScale.ScaleX, this.FontScale.ScaleY, 90, (x + lineHeight) * k, (H - y) * k),// Bottom Up
            "I" => BuildTextMatrix(this.FontScale.ScaleX, this.FontScale.ScaleY, 180, x * k + lineHeight * k, (H - y) * k + lineHeight * k),// Rotación de 180 grados
            "R" => BuildTextMatrix(this.FontScale.ScaleX, this.FontScale.ScaleY, 270, x * k + lineHeight * k, (H - y) * k),// Rotación de 270 grados en sentido horario
            _ => throw new ArgumentException("Código de rotación no válido."),
        };

        // Tc adds extra character spacing in unscaled text-space units (≈ user
        // units when the text matrix has scale 1). Always reset it at the end
        // so the state doesn't leak to the next text segment.
        var tcPrefix = tracking > 0 ? sprintf("%.3f Tc ", tracking * k) : "";
        var tcSuffix = tracking > 0 ? " 0 Tc" : "";
        var s = sprintf($"BT {tcPrefix}{result} (%s) Tj{tcSuffix} ET", Escape(txt));
        if (Underline && TypeSupport.ToString(txt) != "")
        {
            s = TypeSupport.ToString(s) + " " + DoUnderline(x, y, txt);
        }
        if (ColorFlag)
        {
            s = "q " + TypeSupport.ToString(TextColor) + " " + TypeSupport.ToString(s) + " Q";
        }
        Out(s);
    }



    public virtual bool AcceptPageBreak()
    {
        // Accept automatic page break or not
        return AutoPageBreak;
    }

    public virtual void Cell(double cellWidth)
    {
        Cell(cellWidth, null, null, "0", 0, AlignEnum.Default, false, null);
    }

    public virtual void Cell(double cellWidth, AlignEnum align)
    {
        Cell(cellWidth, null, null, "0", 0, align, false, null);
    }

    public virtual void Cell(double cellWidth, double? cellHeight, string text)
    {
        Cell(cellWidth, cellHeight, text, "0", 0, AlignEnum.Default, false, null);
    }

    public virtual void Cell(double cellWidth, double? cellHeight, string text, AlignEnum align)
    {
        Cell(cellWidth, cellHeight, text, "0", 0, align, false, null);
    }

    public virtual void Cell(double cellWidth, double? cellHeight, string text, string border)
    {
        Cell(cellWidth, cellHeight, text, border, 0, AlignEnum.Default, false, null);
    }

    public virtual void Cell(double cellWidth, double? cellHeight, string text, string border, int line)
    {
        Cell(cellWidth, cellHeight, text, border, line, AlignEnum.Default, false, null);
    }

    public virtual void Cell(double cellWidth, double? cellHeight, string text, string border, int line, AlignEnum align)
    {
        Cell(cellWidth, cellHeight, text, border, line, align, false, null);
    }

    public virtual void Cell(double cellWidth, double? cellHeight, string text, string border, int line, AlignEnum align, bool fill)
    {
        Cell(cellWidth, cellHeight, text, border, line, align, fill, null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cellWidth"></param>
    /// <param name="cellHeight"></param>
    /// <param name="text"></param>
    /// <param name="border">1, T, B, L, R</param>
    /// <param name="line"></param>
    /// <param name="align"></param>
    /// <param name="fill"></param>
    /// <param name="link"></param>
    public virtual void Cell(double cellWidth, double? cellHeight, string text, string border, int line, AlignEnum align, bool fill,
        LinkData link)
    {
        if (!cellHeight.HasValue)
            cellHeight = 0;

        if (Y + cellHeight > PageBreakTrigger && !InHeader && !InFooter && AcceptPageBreak())
        {
            // Automatic page break
            double savedXPos = X;
            double ws = Ws;
            if (ws > 0)
            {
                Ws = 0;
                Out("0 Tw");
            }
            AddPage(CurrentOrientation, CurrentPageSize);
            X = savedXPos;
            if (ws > 0)
            {
                Ws = ws;
                Out(sprintf("%.3F Tw", ws * k));
            }
        }

        if (cellWidth == 0)
        {
            cellWidth = W - RightMargin - X;
        }

        var outputString = string.Empty;

        var borderi = TypeSupport.ToInt32(border);
        if (fill || borderi == 1)
        {
            var op = string.Empty;
            if (fill)
            {
                op = (borderi == 1) ? "B" : "f";
            }
            else
            {
                op = "S";
            }
            outputString = sprintf("%.2F %.2F %.2F %.2F re %s ", X * k, (H - Y) * k, cellWidth * k, -cellHeight * k, op);
        }

        if (!string.IsNullOrEmpty(border))
        {
            if (border.Contains("L"))
            {
                outputString = outputString + sprintf("%.2F %.2F m %.2F %.2F l S ", X * k, (H - Y) * k, X * k, (H - (Y + cellHeight)) * k);
            }
            if (border.Contains("T"))
            {
                outputString = outputString + sprintf("%.2F %.2F m %.2F %.2F l S ", X * k, (H - Y) * k, (X + cellWidth) * k, (H - Y) * k);
            }
            if (border.Contains("R"))
            {
                outputString = outputString +
                               sprintf("%.2F %.2F m %.2F %.2F l S ", (X + cellWidth) * k, (H - Y) * k, (X + cellWidth) * k, (H - (Y + cellHeight)) * k);
            }
            if (border.Contains("B"))
            {
                outputString = outputString +
                               sprintf("%.2F %.2F m %.2F %.2F l S ", X * k, (H - (Y + cellHeight)) * k, (X + cellWidth) * k, (H - (Y + cellHeight)) * k);
            }
        }

        if (!string.IsNullOrEmpty(text))
        {
            double dx;
            switch (align)
            {
                case AlignEnum.Right:
                    dx = cellWidth - CMargin - GetStringWidth(text);
                    break;
                case AlignEnum.Center:
                    dx = (cellWidth - GetStringWidth(text)) / 2;
                    break;
                default:
                    dx = CMargin;
                    break;
            }
            if (ColorFlag)
            {
                outputString = outputString + "q " + TypeSupport.ToString(TextColor) + " ";
            }

            string txt2 = text
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");

            if (FontScale.HasScale())
            {
                outputString = outputString + sprintf($"BT {FontScale}%.2F %.2F Tm (%s) Tj ET", (X + dx) * k, (H - (Y + .5 * cellHeight + .3 * FontSize)) * k, txt2);
            }
            else
            {
                outputString = outputString + sprintf($"BT %.2F %.2F Td (%s) Tj ET", (X + dx) * k, (H - (Y + .5 * cellHeight + .3 * FontSize)) * k, txt2);
            }
            if (Underline)
            {
                outputString = outputString + " " + DoUnderline(X + dx, Y + .5 * cellHeight.Value + .3 * FontSize, text);
            }
            if (ColorFlag)
            {
                outputString = outputString + " Q";
            }
            if (link != null)
            {
                Link(X + dx, Y + .5 * cellHeight.Value - .5 * FontSize, GetStringWidth(text), FontSize, link);
            }
        }
        if (!string.IsNullOrEmpty(outputString))
        {
            Out(outputString);
        }
        Lasth = cellHeight.Value;
        if (line > 0)
        {
            // Go to next line
            Y += cellHeight.Value;
            if (line == 1)
            {
                X = LeftMargin;
            }
        }
        else
        {
            X += cellWidth;
        }
    }

    public virtual void MultiCell(double cellWidth, int cellHeight, string text)
    {
        MultiCell(cellWidth, cellHeight, text, null, AlignEnum.Default, false);
    }

    public virtual void MultiCell(double cellWidth, double cellHeight, string text, string border, AlignEnum align, bool fill)
    {
        text = text ?? "";
        if (align == AlignEnum.Default)
        {
            align = AlignEnum.Justified;
        }

        // Output text with automatic or explicit line breaks
        double wmax;
        int textLength;
        string newBorder;
        string b2 = string.Empty;
        int wordSeparator;
        double l;
        int ns;
        int nl;
        double ls = 0; //MARCO. IT WAS AN INTEGER
        FontDefinition cw = CurrentFont;
        if (cellWidth == 0)
        {
            cellWidth = W - RightMargin - X;
        }
        wmax = (cellWidth - 2 * CMargin) * 1000 / FontSize;
        text = text.Replace("\r", "");
        textLength = text.Length;
        if (textLength > 0 && text[ textLength - 1 ] == '\n')
        {
            textLength--;
        }
        newBorder = 0.ToString();
        if (TypeSupport.ToBoolean(border))
        {
            if (TypeSupport.ToInt32(border) == 1)
            {
                border = "LTRB";
                newBorder = "LRT";
                b2 = "LR";
            }
            else
            {
                b2 = "";
                if (border.Contains("L"))
                {
                    b2 += "L";
                }
                if (border.Contains("R"))
                {
                    b2 += "R";
                }
                newBorder = border.Contains("T") ? b2 + "T" : b2;
            }
        }
        wordSeparator = -1;
        var currentPosition = 0;
        var paragraphBeginning = 0;
        l = 0;
        ns = 0;
        nl = 1;
        while (currentPosition < textLength)
        {
            // Get next character
            var nextChar = text[ currentPosition ].ToString();
            if (nextChar == "\n")
            {
                // Explicit line break, dump a cell with the text so far.
                if (Ws > 0)
                {
                    Ws = 0;
                    Out("0 Tw");
                }

                Cell(cellWidth, cellHeight, text.Substring(paragraphBeginning, currentPosition - paragraphBeginning), newBorder, 2, align, fill, null);
                currentPosition++;
                wordSeparator = -1;
                paragraphBeginning = currentPosition;
                l = 0;
                ns = 0;
                nl++;
                if (TypeSupport.ToBoolean(border) && nl == 2)
                {
                    newBorder = b2;
                }
                continue;
            }
            if (nextChar == " ")
            {
                wordSeparator = currentPosition;
                ls = l;
                ns++;
            }
            if (cw.Widths.ContainsKey(nextChar[ 0 ].ToString()))
            {
                l = l + cw.Widths[ nextChar[ 0 ].ToString() ];
            }
            else
            {
                l = l + cw.Widths[ "A" ];
            }
            if (l > wmax)
            {
                // Automatic line break
                if (wordSeparator == -1)
                {
                    if (currentPosition == paragraphBeginning)
                    {
                        currentPosition++;
                    }
                    if (Ws > 0)
                    {
                        Ws = 0;
                        Out("0 Tw");
                    }
                    Cell(cellWidth, cellHeight, TypeSupport.ToString(text).Substring(paragraphBeginning, currentPosition - paragraphBeginning), newBorder, 2, align, fill, null);
                }
                else
                {
                    if (align == AlignEnum.Justified)
                    {
                        Ws = (ns > 1) ? (wmax - ls) / 1000 * FontSize / (ns - 1) : 0;

                        Out(sprintf("%.3F Tw", Ws * k));
                    }
                    Cell(cellWidth, cellHeight, TypeSupport.ToString(text).Substring(paragraphBeginning, wordSeparator - paragraphBeginning), newBorder, 2, align, fill, null);
                    currentPosition = wordSeparator + 1;
                }
                wordSeparator = -1;
                paragraphBeginning = currentPosition;
                l = 0;
                ns = 0;
                nl++;
                if (TypeSupport.ToBoolean(border) && nl == 2)
                {
                    newBorder = b2;
                }
            }
            else
            {
                currentPosition++;
            }
        }
        // Last chunk
        if (Ws > 0)
        {
            Ws = 0;
            Out("0 Tw");
        }
        //CONVERSION_TODO: The equivalent in .NET for strpos may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
        if (TypeSupport.ToBoolean(border) &&
            (border.IndexOf("B") != Convert.ToInt32(false) || !(border.IndexOf("B").GetType() == false.GetType())))
        {
            newBorder += "B";
        }
        Cell(cellWidth, cellHeight, TypeSupport.ToString(text).Substring(paragraphBeginning, currentPosition - paragraphBeginning), newBorder, 2, align, fill, null);
        X = LeftMargin;
    }

    public virtual double CellMeasure(double cellWidth, double cellHeight, string text)
    {
        if (string.IsNullOrEmpty(text))
            text = " ";
        // Output text with automatic or explicit line breaks
        var cw = CurrentFont;
        if (cellWidth == 0)
        {
            cellWidth = W - RightMargin - X;
        }
        var wmax = (cellWidth - 2 * CMargin) * 1000 / FontSize;
        text = text.Replace("\r", "");
        var textLength = text.Length;
        if (textLength > 0 && text[ textLength - 1 ] == '\n')
        {
            textLength--;
        }
        var wordSeparator = -1;
        var currentPosition = 0;
        var paragraphBeginning = 0;
        double l = 0;
        var nl = 1;
        var lines = 1;
        while (currentPosition < textLength)
        {
            // Get next character
            var nextChar = text[ currentPosition ].ToString();
            if (nextChar == "\n")
            {
                // Explicit line break, dump a cell with the text so far.
                if (Ws > 0)
                {
                    Ws = 0;
                }

                lines++;
                currentPosition++;
                wordSeparator = -1;
                paragraphBeginning = currentPosition;
                l = 0;
                nl++;
                continue;
            }
            if (nextChar == " ")
            {
                wordSeparator = currentPosition;
            }

            if (cw.Widths.ContainsKey(nextChar))
            {
                l += cw.Widths[ nextChar[ 0 ].ToString() ];
            }
            else
            {
                l += cw.Widths[ "A" ];
            }

            if (l > wmax)
            {
                // Automatic line break
                if (wordSeparator == -1)
                {
                    if (currentPosition == paragraphBeginning)
                    {
                        currentPosition++;
                    }
                    lines++;
                }
                else
                {
                    lines++;
                    currentPosition = wordSeparator + 1;
                }
                wordSeparator = -1;
                paragraphBeginning = currentPosition;
                l = 0;
                nl++;
            }
            else
            {
                currentPosition++;
            }
        }
        // Last chunk
        return lines * cellHeight;
    }

    public virtual void BoxedText(double cellWidth, double cellHeight, double fullCellHeight, string text, string border, int line, AlignEnum align, bool fill)
    {
        var preserveY = Y;
        var preserveX = X;
        var oldPage = Page;
        Cell(cellWidth, fullCellHeight, string.Empty, border, line);
        var newX = X;
        var newY = Y;
        X = preserveX;
        Y = preserveY;
        if (Page != oldPage)
        {
            Y = BelowHeaderY;
        }

        MultiCell(cellWidth, cellHeight, text, "", align, fill);
        X = newX;
        Y = newY;
        Lasth = fullCellHeight;
    }

    public virtual void Write(int h, string txt)
    {
        Write(h, txt, (LinkData) null);
    }

    public virtual void Write(int h, string txt, int internalLink)
    {
        LinkDataInternal link = Links[ internalLink ];
        Write(h, txt, link);
    }

    public virtual void Write(int h, string txt, string uri)
    {
        var data = new LinkDataUri(uri);
        Write(h, txt, data);
    }

    private void Write(int h, string txt, LinkData link)
    {
        // Output text in flowing mode
        var cw = CurrentFont;
        double localWidth = W - RightMargin - X;
        double wmax = (localWidth - 2 * CMargin) * 1000 / FontSize;
        var s = txt.Replace("\r", "");
        var nb = s.Length;
        var sep = -1;
        var i = 0;
        var j = 0;
        double l = 0;
        var nl = 1;
        while (i < nb)
        {
            // Get next character
            string c = s[ i ].ToString();
            if (TypeSupport.ToString(c) == "\n")
            {
                // Explicit line break
                Cell(localWidth, h, s.Substring(j, i - j), 0.ToString(), 2, AlignEnum.Default, false, link);
                i++;
                sep = -1;
                j = i;
                l = 0;
                if (nl == 1)
                {
                    X = LeftMargin;
                    localWidth = W - RightMargin - X;
                    wmax = (localWidth - 2 * CMargin) * 1000 / FontSize;
                }
                nl++;
                continue;
            }
            if (c == " ")
            {
                sep = i;
            }

            if (cw.Widths.ContainsKey(c[ 0 ].ToString()))
            {
                l = l + cw.Widths[ c[ 0 ].ToString() ];
            }
            else
            {
                l = l + cw.Widths[ "A" ];
            }

            if (l > wmax)
            {
                // Automatic line break
                if (sep == -1)
                {
                    if (X > LeftMargin)
                    {
                        // Move to next line
                        X = LeftMargin;
                        Y += h;
                        localWidth = W - RightMargin - X;
                        wmax = (localWidth - 2 * CMargin) * 1000 / FontSize;
                        i++;
                        nl++;
                        continue;
                    }
                    if (i == j)
                    {
                        i++;
                    }
                    Cell(localWidth, h, s.Substring(j, i - j), 0.ToString(CultureInfo.InvariantCulture), 2, AlignEnum.Default, false, link);
                }
                else
                {
                    Cell(localWidth, h, s.Substring(j, sep - j), 0.ToString(CultureInfo.InvariantCulture), 2, AlignEnum.Default, false, link);
                    i = sep + 1;
                }
                sep = -1;
                j = i;
                l = 0;
                if (nl == 1)
                {
                    X = LeftMargin;
                    localWidth = W - RightMargin - X;
                    wmax = (localWidth - 2 * CMargin) * 1000 / FontSize;
                }
                nl++;
            }
            else
            {
                i++;
            }
        }
        // Last chunk
        if (i == j)
            return;
        //this._out(l + " " + Convert.ToString(this.x, CultureInfo.InvariantCulture) + " " + Convert.ToString(this.ws, CultureInfo.InvariantCulture) + " " + Convert.ToString(this.RightMargin, CultureInfo.InvariantCulture));
        double w2 = (double) l / 1000 * FontSize;
        Cell(w2, h, s.Substring(j), 0.ToString(CultureInfo.InvariantCulture), 0, AlignEnum.Default, false, link);
        //string tail = l + " " + Convert.ToString(this.x, CultureInfo.InvariantCulture) + " " + Convert.ToString(this.ws, CultureInfo.InvariantCulture) + " " + Convert.ToString(this.RightMargin, CultureInfo.InvariantCulture);
        //this._out(tail);
    }

    public virtual void Ln()
    {
        // Line feed; default value is last cell height
        X = LeftMargin;
        Y += Lasth;
    }

    public virtual void Ln(double h)
    {
        // Line feed; 
        X = LeftMargin;
        Y += h;
    }

    public virtual void Image(string file, double? x, double? y, double w)
    {
        Image(file, x, y, w, 0, ImageTypeEnum.Default, (LinkData) null);
    }

    public virtual void Image(string file, double? x, double? y, double w, double h)
    {
        Image(file, x, y, w, h, ImageTypeEnum.Default, (LinkData) null);
    }

    public virtual void Image(string file, double w, double h, ImageTypeEnum type, LinkData link)
    {
        Image(file, null, null, w, h, type, link);
    }

    public virtual void Image(string file, double w, double h, ImageTypeEnum type, string link)
    {
        Image(file, null, null, w, h, type, new LinkDataUri(link));
    }

    public virtual void Image(string file, double? x, double? y, double w, double h, ImageTypeEnum type, string link)
    {
        Image(file, x, y, w, h, type, new LinkDataUri(link));
    }

    public virtual void Image(string file, double? x, double? y, double w, double h, ImageTypeEnum type,
        LinkData link)
    {
        var holew = w;
        var holey = h;// ?? 0;

        // Put an image on the page
        ImageInfo imageInfo;
        if (!Images.ContainsKey(file))
        {

            // First use of this image, get info
            if (type == ImageTypeEnum.Default)
            {
                using (var codec = SKCodec.Create(file))
                {
                    var format = codec.EncodedFormat;
                    var dimensions = codec.GetScaledDimensions(1);

                    type =
                        format == SKEncodedImageFormat.Jpeg ? ImageTypeEnum.Jpg :
                        format == SKEncodedImageFormat.Png ? ImageTypeEnum.Png :
                        format == SKEncodedImageFormat.Gif ? ImageTypeEnum.Gif : ImageTypeEnum.Default;

                    if (type == ImageTypeEnum.Default)
                    {
                        Error("Unable to detect image type: " + file);
                    }
                }
            }

            ImageInfo imageData = null;
            switch (type)
            {
                case ImageTypeEnum.Jpg:
                    imageData = ParseJpg(file);
                    break;
                case ImageTypeEnum.Png:
                    imageData = ParsePng(file);
                    break;
                case ImageTypeEnum.Gif:
                    imageData = ParseGif(file);
                    break;
                case ImageTypeEnum.Default:
                default:
                    Error("Unable to detect image type (" + file + ")");
                    break;
            }
            imageInfo = imageData;
            imageInfo.i = Images.Count + 1;
            Images[ file ] = imageInfo;
        }
        else
        {
            imageInfo = Images[ file ];
        }

        var calculateRatio = w > 0 && h > 0;

        // Automatic width and height calculation if needed
        if (w == 0 && h == 0)
        {
            // Put image at 96 dpi
            w = -96;
            h = -96;
        }
        if (w < 0)
        {
            w = TypeSupport.ToDouble(-TypeSupport.ToDouble(imageInfo.w)) * 72 / w / k;
        }
        if (h < 0)
        {
            h = TypeSupport.ToDouble(-TypeSupport.ToDouble(imageInfo.h)) * 72 / h / k;
        }
        if (w == 0)
        {
            w = h * TypeSupport.ToDouble(imageInfo.w) / TypeSupport.ToDouble(imageInfo.h);
        }
        if (h == 0)
        {
            h = w * TypeSupport.ToDouble(imageInfo.h) / TypeSupport.ToDouble(imageInfo.w);
        }

        if (calculateRatio)
        {
            var ratiow = imageInfo.w / w;
            var ratioh = imageInfo.h / h;
            if (ratioh > ratiow)
            {
                w = w * ratiow / ratioh;
                x = x + (holew - w) / 2;
            }
            if (ratiow >= ratioh)
            {
                h = h * ratioh / ratiow;
                y = y + (holey - h) / 2;
            }
        }

        // Flowing mode
        if (!y.HasValue)
        {
            if (Y + h > PageBreakTrigger && !InHeader && !InFooter && AcceptPageBreak())
            {
                // Automatic page break
                var x2 = X;
                AddPage(CurrentOrientation, CurrentPageSize);
                X = x2;
            }
            y = Y;
            Y += h;
        }

        if (!x.HasValue)
        {
            x = X;
        }
        Out(sprintf("q %.2F 0 0 %.2F %.2F %.2F cm /I%d Do Q", w * k, h * k, x * k, ((H - (y + h)) * k), imageInfo.i));
        if (link != null)
        {
            Link(x.Value, y.Value, w, h, link);
        }
    }

    public virtual DrawingPoint GetPos()
    {
        return new DrawingPoint(this.X, this.Y);
    }

    public virtual void SetPos(DrawingPoint point)
    {
        this.SetX(point.X);
        this.SetY(point.Y);
    }

    private Stack<DrawingPoint> SavedPositions = new Stack<DrawingPoint>();

    public void SavePos()
    {
        SavedPositions.Push(GetPos());
    }

    public enum GoBackMode
    {
        Both,
        X,
        Y
    }

    public DrawingPoint GoBack(GoBackMode mode = GoBackMode.Both)
    {
        var pos = SavedPositions.Pop();
        if (mode != GoBackMode.Y) this.X = pos.X;
        if (mode != GoBackMode.X) this.Y = pos.Y;
        return pos;
    }

    /// <summary>
    /// Pushes the current (X, Y) onto <see cref="SavePos"/>'s stack and
    /// returns a scope handle that pops it back on dispose. Equivalent
    /// to a paired <c>SavePos()</c> / <c>GoBack()</c> guarded against
    /// being forgotten on exceptional paths:
    ///
    /// <code>
    /// using (pdf.PushPos())
    /// {
    ///     pdf.SetXY(120, 50);
    ///     pdf.Image(file, pdf.X, pdf.Y, 30, 0);
    /// }   // cursor restored even if an exception was thrown above
    /// </code>
    ///
    /// Nests freely: each <c>using</c> is an independent stack frame.
    /// </summary>
    public IDisposable PushPos(GoBackMode restore = GoBackMode.Both)
    {
        SavePos();
        return new PositionScope(this, restore);
    }

    private sealed class PositionScope : IDisposable
    {
        private readonly FPdf _pdf;
        private readonly GoBackMode _mode;
        private bool _disposed;
        public PositionScope(FPdf pdf, GoBackMode mode)
        {
            _pdf = pdf;
            _mode = mode;
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _pdf.GoBack(_mode);
        }
    }

    /// <summary>
    /// Snapshots the current text-and-drawing state — font family,
    /// style and size; text/fill/draw colours; line width — and
    /// restores it when the returned scope is disposed. Cursor
    /// position is left alone (use <see cref="PushPos"/> for that).
    ///
    /// <code>
    /// pdf.SetFont("Helvetica", "", 11);
    /// pdf.SetTextColor(Color.Black);
    /// using (pdf.PushState())
    /// {
    ///     pdf.SetFont("Helvetica", "B", 7);
    ///     pdf.SetTextColor(BrandAccent);
    ///     pdf.Cell(40, 4, "LABEL");
    /// }
    /// // back to Helvetica regular 11, black text.
    /// </code>
    /// </summary>
    public IDisposable PushState()
    {
        return new StateScope(this);
    }

    private sealed class StateScope : IDisposable
    {
        private readonly FPdf _pdf;
        private readonly string _family;
        private readonly string _style;
        private readonly double _sizePt;
        private readonly string _drawColor;
        private readonly string _fillColor;
        private readonly string _textColor;
        private readonly bool _colorFlag;
        private readonly double _lineWidth;
        private bool _disposed;

        public StateScope(FPdf pdf)
        {
            _pdf = pdf;
            _family = pdf.FontFamily;
            _style = pdf.FontStyle;
            _sizePt = pdf.FontSizePt;
            _drawColor = pdf.DrawColor;
            _fillColor = pdf.FillColor;
            _textColor = pdf.TextColor;
            _colorFlag = pdf.ColorFlag;
            _lineWidth = pdf.LineWidth;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // Restore font first (which emits its own /Fn ... Tf
            // operator) before the colour state, so the saved colour
            // strings hit the now-current text.
            if (!string.IsNullOrEmpty(_family))
            {
                _pdf.SetFont(_family, _style, _sizePt);
            }
            _pdf.DrawColor = _drawColor;
            _pdf.FillColor = _fillColor;
            _pdf.TextColor = _textColor;
            _pdf.ColorFlag = _colorFlag;
            if (_pdf.Page > 0)
            {
                _pdf.Out(_drawColor);
                if (_fillColor != _drawColor) _pdf.Out(_fillColor);
            }
            _pdf.SetLineWidth(_lineWidth);
        }
    }

    public DrawingPoint LastPosition()
    {
        return SavedPositions.Peek();
    }

    // ====================================================================
    //  Layout primitives -- Bounds / Row / Stack / Panel
    // ====================================================================

    /// <summary>
    /// Returns the printable area of the current page as a <see cref="Rect"/>:
    /// the page size minus the four margins. Convenient starting point for
    /// composing layouts with <see cref="Row(Rect,int,double)"/> and friends.
    /// </summary>
    public Rect Bounds()
    {
        var x = LeftMargin;
        var y = TopMargin;
        var w = W - LeftMargin - RightMargin;
        var h = H - TopMargin - PageBreakMargin;
        return new Rect(x, y, w, h);
    }

    /// <summary>
    /// Splits <paramref name="bounds"/> into <paramref name="count"/>
    /// equal-width side-by-side rectangles separated by <paramref name="gap"/>
    /// mm. Equivalent to <c>Row(bounds, Enumerable.Repeat(1.0, count), gap)</c>.
    /// </summary>
    public Rect[] Row(Rect bounds, int count, double gap = 0)
    {
        if (count <= 0) return Array.Empty<Rect>();
        var weights = new double[count];
        for (int i = 0; i < count; i++) weights[i] = 1.0;
        return Row(bounds, weights, gap);
    }

    /// <summary>
    /// Splits <paramref name="bounds"/> horizontally according to
    /// <paramref name="weights"/>. Weights are normalised, so <c>{1, 2, 1}</c>
    /// and <c>{0.25, 0.5, 0.25}</c> produce the same result.
    /// <paramref name="gap"/> mm of horizontal space sits between each slot.
    ///
    /// <code>
    /// var slots = pdf.Row(pdf.Bounds(), new[] { 1.0, 2.0, 1.0 }, gap: 3);
    /// // slots[0].W : slots[1].W : slots[2].W == 1 : 2 : 1
    /// </code>
    /// </summary>
    public Rect[] Row(Rect bounds, double[] weights, double gap = 0)
    {
        if (weights == null || weights.Length == 0) return Array.Empty<Rect>();
        double sum = 0;
        for (int i = 0; i < weights.Length; i++) sum += Math.Max(0, weights[i]);
        if (sum <= 0) return Array.Empty<Rect>();

        var available = Math.Max(0, bounds.W - gap * (weights.Length - 1));
        var slots = new Rect[weights.Length];
        var cursor = bounds.X;
        for (int i = 0; i < weights.Length; i++)
        {
            var w = available * Math.Max(0, weights[i]) / sum;
            slots[i] = new Rect(cursor, bounds.Y, w, bounds.H);
            cursor += w + gap;
        }
        return slots;
    }

    /// <summary>
    /// Splits <paramref name="bounds"/> into <paramref name="count"/>
    /// equal-height stacked rectangles separated by <paramref name="gap"/>
    /// mm of vertical space.
    /// </summary>
    public Rect[] Stack(Rect bounds, int count, double gap = 0)
    {
        if (count <= 0) return Array.Empty<Rect>();
        var weights = new double[count];
        for (int i = 0; i < count; i++) weights[i] = 1.0;
        return Stack(bounds, weights, gap);
    }

    /// <summary>
    /// Splits <paramref name="bounds"/> vertically according to
    /// <paramref name="weights"/> (normalised). Useful when the relative
    /// heights matter more than absolute mm.
    ///
    /// <code>
    /// var rows = pdf.Stack(pdf.Bounds(), new[] { 1.0, 3.0 }, gap: 4);
    /// // rows[0] gets 25% of the height, rows[1] gets 75%
    /// </code>
    /// </summary>
    public Rect[] Stack(Rect bounds, double[] weights, double gap = 0)
    {
        if (weights == null || weights.Length == 0) return Array.Empty<Rect>();
        double sum = 0;
        for (int i = 0; i < weights.Length; i++) sum += Math.Max(0, weights[i]);
        if (sum <= 0) return Array.Empty<Rect>();

        var available = Math.Max(0, bounds.H - gap * (weights.Length - 1));
        var slots = new Rect[weights.Length];
        var cursor = bounds.Y;
        for (int i = 0; i < weights.Length; i++)
        {
            var h = available * Math.Max(0, weights[i]) / sum;
            slots[i] = new Rect(bounds.X, cursor, bounds.W, h);
            cursor += h + gap;
        }
        return slots;
    }

    /// <summary>
    /// Paints a titled framed box at <paramref name="bounds"/> using the
    /// current draw/fill/font/text colours, then invokes <paramref name="body"/>
    /// with the inner content rectangle (below the title band, padded). The
    /// font and colour state in effect on entry is restored after the body
    /// runs, so the caller doesn't have to clean up.
    ///
    /// <code>
    /// pdf.SetFillColor(PanelFill);
    /// pdf.SetDrawColor(LineGray);
    /// pdf.SetTextColor(BrandAccent);
    /// pdf.SetFont("Helvetica", "B", 8);
    /// pdf.Panel(slot, "MEDIDAS DC", content =>
    /// {
    ///     // free to set any font/colour here -- restored on exit
    ///     pdf.SetFont("Helvetica", "", 8);
    ///     pdf.SetXY(content.X, content.Y);
    ///     pdf.Cell(content.W, 5, "Voc: 247 V");
    /// });
    /// </code>
    /// </summary>
    /// <param name="bounds">Outer rectangle (including frame and title band).</param>
    /// <param name="title">Title text; pass <c>null</c> or empty for no title band.</param>
    /// <param name="body">Renderer for the panel contents. Receives the inner rect.</param>
    /// <param name="padding">Padding from the frame to the inner content, in mm.</param>
    /// <param name="titleHeight">Height of the title band, in mm.</param>
    public void Panel(Rect bounds, string title, Action<Rect> body,
        double padding = 3, double titleHeight = 5)
    {
        using (PushState())
        {
            RenderPanelFrame(bounds, title, padding, titleHeight,
                drawFill: true, drawBorder: true, drawHairline: true);
            var content = ComputeContentRect(bounds, title, padding, titleHeight);
            body?.Invoke(content);
        }
    }

    /// <summary>
    /// Title-less variant of <see cref="Panel(Rect, string, Action{Rect}, double, double)"/>
    /// -- just a framed padded box.
    /// </summary>
    public void Panel(Rect bounds, Action<Rect> body, double padding = 3)
        => Panel(bounds, null, body, padding, 0);

    /// <summary>
    /// Styled variant of <see cref="Panel(Rect, string, Action{Rect}, double, double)"/>:
    /// caller passes a <see cref="PanelStyle"/> describing the fill,
    /// border and title look. The frame and title are painted with the
    /// style's colours / font / line width; the rest of the state (and
    /// anything the body sets) is restored on return.
    ///
    /// <code>
    /// var brand = new PanelStyle
    /// {
    ///     FillColor   = panelFill,
    ///     BorderColor = lineGray,
    ///     TitleColor  = brandAccent,
    /// };
    /// pdf.Panel(slot, "MEDIDAS DC", brand, content => { ... });
    /// pdf.Panel(slot2, "MEDIDAS AC", brand, content => { ... });
    /// </code>
    ///
    /// A <c>null</c> <see cref="PanelStyle.FillColor"/> skips the fill,
    /// a <c>null</c> <see cref="PanelStyle.BorderColor"/> skips the border.
    /// </summary>
    public void Panel(Rect bounds, string title, PanelStyle style, Action<Rect> body)
    {
        style ??= new PanelStyle();
        using (PushState())
        {
            if (style.FillColor.HasValue) SetFillColor(style.FillColor.Value);
            if (style.BorderColor.HasValue) SetDrawColor(style.BorderColor.Value);
            SetLineWidth(style.LineWidth);

            if (!string.IsNullOrEmpty(style.TitleFontFamily))
            {
                SetFont(style.TitleFontFamily, style.TitleFontStyle ?? "",
                    style.TitleFontSize);
            }
            if (style.TitleColor.HasValue) SetTextColor(style.TitleColor.Value);

            RenderPanelFrame(bounds, title, style.Padding, style.TitleHeight,
                drawFill: style.FillColor.HasValue,
                drawBorder: style.BorderColor.HasValue,
                drawHairline: style.TitleHairline && style.BorderColor.HasValue);

            var content = ComputeContentRect(bounds, title, style.Padding, style.TitleHeight);
            body?.Invoke(content);
        }
    }

    /// <summary>Untitled, styled variant of <see cref="Panel(Rect, string, PanelStyle, Action{Rect})"/>.</summary>
    public void Panel(Rect bounds, PanelStyle style, Action<Rect> body)
        => Panel(bounds, null, style, body);

    private void RenderPanelFrame(Rect bounds, string title, double padding,
        double titleHeight, bool drawFill, bool drawBorder, bool drawHairline)
    {
        string mode = (drawFill, drawBorder) switch
        {
            (true,  true)  => "DF",
            (true,  false) => "F",
            (false, true)  => "D",
            _              => null,
        };
        if (mode != null) Rect(bounds.X, bounds.Y, bounds.W, bounds.H, mode);

        if (!string.IsNullOrEmpty(title))
        {
            SetXY(bounds.X + padding, bounds.Y + padding * 0.6);
            Cell(bounds.W - 2 * padding, titleHeight, title);
            if (drawHairline)
            {
                var hy = bounds.Y + padding * 0.6 + titleHeight + 0.5;
                Line(bounds.X + padding, hy, bounds.Right - padding, hy);
            }
        }
    }

    private static Rect ComputeContentRect(Rect bounds, string title,
        double padding, double titleHeight)
    {
        double topUsed = padding;
        if (!string.IsNullOrEmpty(title))
            topUsed = padding * 0.6 + titleHeight + 1.5;
        return bounds.Inset(padding, topUsed, padding, padding);
    }

    public virtual double GetX()
    {
        // Get x position
        return X;
    }

    public virtual void SetX(double x)
    {
        // Set x position
        if (x >= 0)
        {
            X = x;
        }
        else
        {
            X = W + x;
        }
    }

    /// <summary>
    ///     Get y position
    /// </summary>
    /// <returns></returns>
    public virtual double GetY()
    {
        return Y;
    }

    /// <summary>
    ///     Set y position and reset x
    /// </summary>
    /// <param name="y"></param>
    public virtual void SetY(double y)
    {
        X = LeftMargin;
        if (y >= 0)
        {
            Y = y;
        }
        else
        {
            Y = TypeSupport.ToDouble(H) + y;
        }
    }

    /// <summary>
    ///     Set x and y positions
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public virtual void SetXY(double x, double y)
    {
        SetY(y);
        SetX(x);
    }


    /*******************************************************************************
    *                                                                              *
    *                              protected virtual methods                               *
    *                                                                              *
    *******************************************************************************/

    internal virtual Dimensions GetPageSize(PageSizeEnum index)
    {
        PageSize size = StdPageSizes.FirstOrDefault(x => x.Name.ToLower() == index.ToString().ToLower());
        Debug.Assert(size != null, "size != null");
        return size.GetDimensions(k);
    }

    internal virtual Dimensions GetPageSize(Dimensions dimensions)
    {
        return dimensions.Width > dimensions.Heigth
            ? new Dimensions { Width = dimensions.Heigth, Heigth = dimensions.Width }
            : dimensions;
    }

    internal virtual void BeginPage(PageOrientation orientation, Dimensions size)
    {
        Page++;
        Pages[ Page ] = new Page();
        State = 2;
        X = LeftMargin;
        Y = TopMargin;
        FontFamily = "";
        // Check page size and orientation
        if (orientation == PageOrientation.Default)
        {
            orientation = DefOrientation;
        }
        size = size == null ? DefPageSize : GetPageSize(size);

        if (orientation != CurrentOrientation || size.Width != CurrentPageSize.Width || size.Heigth != CurrentPageSize.Heigth)
        {
            // New size or orientation
            if (orientation == PageOrientation.Portrait)
            {
                W = size.Width;
                H = size.Heigth;
            }
            else
            {
                W = size.Heigth;
                H = size.Width;
            }

            WPt = W * k;
            HPt = H * k;
            PageBreakTrigger = TypeSupport.ToDouble(H) - PageBreakMargin;
            CurrentOrientation = orientation;
            CurrentPageSize = size;
        }
        if (orientation != DefOrientation || size.Width != DefPageSize.Width || size.Heigth != DefPageSize.Heigth)
        {
            PageSizes[ Page ] = new Dimensions { Width = WPt, Heigth = HPt };
        }
    }

    internal virtual void EndPage()
    {
        State = 1;
    }

    internal virtual FontDefinition GetFontDefinition(string font)
    {
        // Load a font definition file from the font directory
        FontDefinition fontData;
        FontBuilder.Fonts.TryGetValue(font.ToLower(), out fontData);
        if (string.IsNullOrEmpty(fontData?.Name))
        {
            Error($"Font metrics not found for {font}." +
                $" You need to include the font details in your project or wait for Core 3.0" +
                $"Included fonts are: {string.Join(",", FontBuilder.Fonts.Select(x => x.Key))}");
            Error("Could not include font definition file");
        }
        return fontData;
    }

    internal virtual object Escape(string s)
    {
        s = s
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("\r", "\\r");
        return s;
    }

    internal virtual string TextString(string s)
    {
        // Format a text string
        return "(" + TypeSupport.ToString(Escape(s)) + ")";
    }

    internal virtual string DoUnderline(double x, double y, string txt)
    {
        // Underline text
        var up = CurrentFont.up;
        var ut = CurrentFont.ut;
        var w = GetStringWidth(txt) + Ws * StringSupport.SubstringCount(txt, " ");
        return sprintf("%.2F %.2F %.2F %.2F re f", x * k, (H - (y - up / (double) 1000 * FontSize)) * k, w * k,
            (-ut) / (double) 1000 * FontSizePt);
    }

    internal virtual ImageInfo ParseJpg(string file) => ImageParser.ParseJpg(file);

    internal virtual ImageInfo ParsePng(string file) => ImageParser.ParsePng(file);

    internal virtual ImageInfo ParseGif(string file) => ImageParser.ParseGif(file);

    internal virtual void NewObject()
    {
        // Begin a new object
        ObjectCount++;
        Buffer.Flush();
        Offsets[ ObjectCount ] = Buffer.BaseStream.Position;
        Out(ObjectCount.ToString() + " 0 obj");
    }

    internal virtual void PutStream(string s)
    {
        Out("stream");
        Out(s);
        Out("endstream");
    }

    internal virtual void PutStream(byte[] bytes)
    {
        Out("stream");
        Out(bytes);
        Out("endstream");
    }

    internal virtual void PutStream(List<byte[]> bytes)
    {
        Out("stream");
        Out(bytes);
        Out("endstream");
    }

    internal virtual void PutStreamObject(string data)
    {
        string entries;
        if (this.Compress)
        {
            entries = "/Filter /FlateDecode ";
            var data2 = GzCompressString(data);

        }
        else
        {
            entries = "";
        }
        entries += "/Length " + data.Length;
        this.NewObject();
        this.Out("<<" + entries + ">>");
        this.PutStream(data);
        this.Out("endobj");
    }

    internal virtual void Out(object s)
    {
        // Add a line to the document
        if (State == 2)
        {
            //TODO: APPENDLN ?
            Pages[ Page ]
                .Append(TypeSupport.ToString(s));
            Pages[ Page ]
                .Append("\n");
        }
        else
        {
            if (s is List<byte[]>)
            {
                foreach (var v in (s as List<byte[]>))
                {
                    Buffer.Flush();
                    var w2 = new BinaryWriter(Buffer.BaseStream);
                    w2.Write(v.ToArray());
                    w2.Flush();
                    //Buffer.Write(v.ToArray());
                    //Buffer += PrivateEncoding.GetString(v);
                }
                Buffer.Write("\n");
                //Buffer += "\n";
            }
            else if (s is byte[])
            {
                Buffer.Flush();
                var w2 = new BinaryWriter(Buffer.BaseStream);
                w2.Write((byte[]) s);
                w2.Flush();
                //Buffer += PrivateEncoding.GetString((byte[])s);
                Buffer.Write("\n");
                //Buffer += "\n";
            }
            else
            {
                Buffer.Write(s);
                Buffer.Write("\n");
                //Buffer += TypeSupport.ToString(s) + "\n";
            }
        }
    }

    internal virtual void PutPages()
    {
        double wPt;
        double hPt;
        int i;
        if (!string.IsNullOrWhiteSpace(AliasNbPagesRenamed))
        {
            // Replace number of pages
            foreach (var page in Pages.Values)
            {
                page.Replace(AliasNbPagesRenamed, Page.ToString(PageNumberFormat, DefaultCulture));
            }
        }
        if (DefOrientation == PageOrientation.Portrait)
        {
            wPt = DefPageSize.Width * k;
            hPt = DefPageSize.Heigth * k;
        }
        else
        {
            wPt = DefPageSize.Heigth * k;
            hPt = DefPageSize.Width * k;
        }
        var filter = (Compress) ? "/Filter /FlateDecode " : "";
        foreach (var currentPage in Pages)
        {
            // Page
            NewObject();
            Out("<</Type /Page");
            Out("/Parent 1 0 R");
            if (PageSizes.ContainsKey(currentPage.Key))
            {
                Out(sprintf("/MediaBox [0 0 %.2F %.2F]", PageSizes[ currentPage.Key ].Width, PageSizes[ currentPage.Key ].Heigth));
            }
            Out("/Resources 2 0 R");
            if (currentPage.Value.PageLinks.Count > 0)
            {
                // Links
                var annots = "/Annots [";
                foreach (var pl in currentPage.Value.PageLinks)
                {
                    var rect = sprintf("%.2F %.2F %.2F %.2F", pl.P0, pl.P1, pl.P0 + pl.P2, pl.P1 - pl.P3);
                    annots += "<</Type /Annot /Subtype /Link /Rect [" + rect + "] /Border [0 0 0] ";

                    if (pl.Link is LinkDataInternal)
                    {
                        var link = Links[ (pl.Link as LinkDataInternal).InternalLink ];
                        var pageIndex = link.PageIndex;
                        var h = (PageSizes.ContainsKey(pageIndex)) ? PageSizes[ pageIndex ].Heigth : hPt;
                        annots += sprintf("/Dest [%d 0 R /XYZ 0 %.2F null]>>", 1 + 2 * link.PageIndex, h - link.Y * k);
                    }
                    else if (pl.Link is LinkDataUri)
                    {
                        annots += "/A <</S /URI /URI (" + (pl.Link as LinkDataUri).Uri + ")>>>>";
                    }
                    else
                        throw new NotImplementedException();
                }
                Out(annots + "]");
            }
            if (String.Compare(PdfVersion, "1.3", StringComparison.Ordinal) > 0)
            {
                Out("/Group <</Type /Group /S /Transparency /CS /DeviceRGB>>");
            }
            Out("/Contents " + (this.ObjectCount + 1).ToString(CultureInfo.InvariantCulture) + " 0 R>>");
            Out("endobj");
            // Page content
            if (Compress)
            {
                var p = GzCompressString(currentPage.Value.ToString());
                NewObject();
                Out("<<" + filter + "/Length " + p.Length.ToString() + ">>");
                PutStream(p);
                Out("endobj");
            }
            else
            {
                var p1 = currentPage.Value.ToString();
                NewObject();
                Out("<<" + filter + "/Length " + p1.Length.ToString(CultureInfo.InvariantCulture) + ">>");
                PutStream(p1);
                Out("endobj");
            }
        }
        // Pages root
        Buffer.Flush();
        Offsets[ 1 ] = Buffer.BaseStream.Length;
        Out("1 0 obj");
        Out("<</Type /Pages");
        var kids = "/Kids [";
        for (i = 0; i < Page; i++)
            kids += (3 + 2 * i).ToString(CultureInfo.InvariantCulture) + " 0 R ";
        Out(kids + "]");
        Out("/Count " + Page.ToString(CultureInfo.InvariantCulture));
        Out(sprintf("/MediaBox [0 0 %.2F %.2F]", wPt, hPt));
        Out(">>");
        Out("endobj");
    }

    internal virtual string ToUnicodeCMap(Dictionary<int, object> uv)
    {
        var ranges = new StringBuilder();
        var chars = new StringBuilder();
        int nbr = 0;
        int nbc = 0;
        foreach (var key in uv.Keys)
        {
            var keyValue = Convert.ToInt32(key);
            var value = uv[ key ];
            var values = value as int[];
            if (values != null)
            {
                ranges.Append(sprintf("<%02X> <%02X> <%04X>\n", keyValue, keyValue + values[ 1 ] - 1, values[ 0 ]));
                nbr++;
            }
            else
            {
                var val = Convert.ToInt32(value);
                chars.Append(sprintf("<%02X> <%04X>\n", keyValue, val));
                nbc++;
            }
        }
        var result = new StringBuilder();
        result.Append(
                    "/CIDInit /ProcSet findresource begin\n"
                    + "12 dict begin\n"
                    + "begincmap\n"
                    + "/CIDSystemInfo\n"
                    + "<</Registry (Adobe)\n"
                    + "/Ordering (UCS)\n"
                    + "/Supplement 0\n"
                    + ">> def\n"
                    + "/CMapName /Adobe-Identity-UCS def\n"
                    + "/CMapType 2 def\n"
                    + "1 begincodespacerange\n"
                    + "<00> <FF>\n"
                    + "endcodespacerange\n");

        if (nbr > 0)
        {
            result.Append($"{nbr} beginbfrange\n");
            result.Append(ranges.ToString());
            result.Append("endbfrange\n");
        }

        if (nbc > 0)
        {
            result.Append($"{nbc} beginbfchar\n");
            result.Append(chars);
            result.Append("endbfchar\n");
        }

        result.Append(
           "endcmap\n"
         + "CMapName currentdict /CMap defineresource pop\n"
         + "end\n"
         + "end");
        return result.ToString();
    }

    internal virtual void PutFonts()
    {
        // Font file embedding
        foreach (var info in FontFiles.Values)
        {
            NewObject();
            info.n = ObjectCount;
            var font = FileSystemSupport.ReadContentBytes(Fontpath + info.FontFile);
            if (!font.Any())
            {
                Error("Font file not found: " + info.FontFile);
            }

            var compressed = info.FontFile.EndsWith(".z");
            if (!compressed && info.length2 > 0)
            {
                //font = TypeSupport.ToString(font).Substring(6, info.length1)
                //       + TypeSupport.ToString(font).Substring(6 + info.length1 + 6, info.length2);
            }

            Out("<</Length " + font.Length);
            if (compressed)
            {
                Out("/Filter /FlateDecode");
            }

            //Out("/Length1 " + TypeSupport.ToString(info.length1));
            Out("/Length1 " + TypeSupport.ToString(font.Length));
            if (info.length2 != 0)
            {
                Out("/Length2 " + TypeSupport.ToString(info.length2) + " /Length3 0");
            }
            Out(">>");
            PutStream(font);
            Out("endobj");
        }

        foreach (var fontKey in Fonts.Keys)
        {
            var font1 = Fonts[ fontKey ];

            //Encoding
            if (!string.IsNullOrWhiteSpace(font1.diff))
            {
                if (string.IsNullOrWhiteSpace(font1.enc))
                {
                    var nf = ObjectCount;
                    // Encodings
                    NewObject();
                    Out("<</Type /Encoding /BaseEncoding /WinAnsiEncoding /Differences [" + TypeSupport.ToString(font1.diff) +
                        "]>>");
                    Out("endobj");
                    this.Encodings[ font1.enc ] = ObjectCount;
                }
            }

            string cmapkey = "--";

            if (font1.uv != null && font1.uv.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(font1.enc))
                {
                    cmapkey = font1.enc;
                }
                else
                {
                    cmapkey = font1.Name;
                }

                if (!CMaps.KeyExists(cmapkey))
                {
                    var cmap = ToUnicodeCMap(font1.uv);
                    PutStreamObject(cmap);
                    CMaps[ cmapkey ] = this.ObjectCount;
                }
            }

            // Font objects
            Fonts[ fontKey ].n = ObjectCount + 1;
            var type = font1.FontType;
            var name = font1.Name;
            if (font1.Subsetted)
            {
                name = "AAAAAA+" + name;
            }

            switch (type)
            {
                case FontTypeEnum.Core:
                    NewObject();
                    Out("<</Type /Font");
                    Out("/BaseFont /" + TypeSupport.ToString(name));
                    Out("/Subtype /Type1");
                    if (TypeSupport.ToString(name) != "Symbol" && TypeSupport.ToString(name) != "ZapfDingbats")
                    {
                        Out("/Encoding /WinAnsiEncoding");
                    }
                    if (font1.uv.Count > 0)
                        Out("/ToUnicode " + CMaps[ cmapkey ] + " 0 R");
                    Out(">>");
                    Out("endobj");
                    break;
                case FontTypeEnum.TrueType:
                case FontTypeEnum.Type1:
                {
                    // Additional Type1 or TrueType/OpenType font
                    NewObject();
                    Out("<</Type /Font");
                    Out("/BaseFont /" + TypeSupport.ToString(name));
                    Out("/Subtype /" + TypeSupport.ToString(type));
                    Out("/FirstChar 32 /LastChar 255");
                    Out("/Widths " + (ObjectCount + 1).ToString(CultureInfo.InvariantCulture) + " 0 R");
                    Out("/FontDescriptor " + (ObjectCount + 2).ToString(CultureInfo.InvariantCulture) + " 0 R");
                    if (font1.diffn.HasValue)
                    {
                        Out("/Encoding " + Encodings[ font1.enc ] + " 0 R");
                    }
                    else
                    {
                        Out("/Encoding /WinAnsiEncoding");
                    }
                    if (font1.uv.Count > 0)
                        Out("/ToUnicode " + CMaps[ cmapkey ] + " 0 R");
                    Out(">>");
                    Out("endobj");
                    // Widths
                    NewObject();
                    StringBuilder s = new StringBuilder("[");
                    int i;

                    for (i = 32; i <= 255; i++)
                    {
                        font1.Widths.TryGetValue(Convert.ToString((char) i), out var t);
                        s.Append(Convert.ToString(t, CultureInfo.InvariantCulture) + " ");
                    }

                    s.Append("]");
                    Out(s.ToString());
                    Out("endobj");
                    // Descriptor
                    NewObject();
                    s.Clear();
                    s.Append("<</Type /FontDescriptor /FontName /" + TypeSupport.ToString(name));

                    foreach (string k1 in font1.desc.Keys)
                    {
                        var v = Convert.ToString(font1.desc[ k1 ], CultureInfo.InvariantCulture);
                        s.Append(" /" + k1 + " " + v); // TypeSupport.ToString(v);
                    }

                    if (!string.IsNullOrEmpty(font1.FontFile))
                    {
                        s.Append(" /FontFile" + (type == FontTypeEnum.Type1 ? "" : "2") + " " +
                             TypeSupport.ToString(FontFiles[ font1.Name ].n) + " 0 R");
                    }
                    Out(s);
                    Out("/Flags " + font1.Flags);
                    Out("/FontBBox ["
                        + font1.FontBBox.X.ToString(CultureInfo.InvariantCulture) + " "
                        + font1.FontBBox.Y.ToString(CultureInfo.InvariantCulture) + " "
                        + font1.FontBBox.Right.ToString(CultureInfo.InvariantCulture) + " "
                        + font1.FontBBox.Bottom.ToString(CultureInfo.InvariantCulture) + "]");
                    Out("/ItalicAngle " + font1.ItalicAngle.ToString(CultureInfo.InvariantCulture));
                    Out("/Ascent " + font1.Ascent.ToString(CultureInfo.InvariantCulture));
                    Out("/Descent " + font1.Descent.ToString(CultureInfo.InvariantCulture));
                    Out("/StemV " + font1.StemV.ToString(CultureInfo.InvariantCulture));
                    Out("/CapHeight " + font1.CapHeight.ToString(CultureInfo.InvariantCulture));

                    Out(">>");
                    Out("endobj");
                }
                break;
                default:
                    Error("Unsupported font type: " + TypeSupport.ToString(type));
                    break;
            }
        }
    }

    internal virtual void PutImages()
    {
        foreach (var file in Images)
        {
            PutImage(file.Value);
            file.Value.data = null; //unset, probably not needed
            file.Value.smask = null; //unset, probably not needed
        }
    }

    internal virtual void PutImage(ImageInfo info)
    {
        NewObject();
        info.n = ObjectCount;
        Out("<</Type /XObject");
        Out("/Subtype /Image");
        Out("/Width " + info.w.ToString());
        Out("/Height " + info.h.ToString());
        if (info.cs == "Indexed")
        {
            Out("/ColorSpace [/Indexed /DeviceRGB " + (info.pal.Length / 3 - 1).ToString() + " " + (ObjectCount + 1).ToString() +
                " 0 R]");
        }
        else
        {
            Out("/ColorSpace /" + TypeSupport.ToString(info.cs));
            if (TypeSupport.ToString(info.cs) == "DeviceCMYK")
            {
                Out("/Decode [1 0 1 0 1 0 1 0]");
            }
        }
        Out("/BitsPerComponent " + TypeSupport.ToString(info.bpc));
        if (info.f != null)
        {
            Out("/Filter /" + TypeSupport.ToString(info.f));
        }
        if (info.dp != null)
        {
            Out("/DecodeParms <<" + TypeSupport.ToString(info.dp) + ">>");
        }
        if (info.trns != null && info.trns.Count() > 0)
        {
            string trns = "";
            foreach (int trn in info.trns)
            {
                trns += trn + " " + trn + " ";
            }
            Out("/Mask [" + trns + "]");
        }
        if (info.smask != null)
        {
            Out("/SMask " + (ObjectCount + 1).ToString() + " 0 R");
        }

        int largo = info.data.Select(x => x.Length).Sum();

        Out("/Length " + largo.ToString() + ">>");
        PutStream(info.data);
        Out("endobj");
        // Soft mask
        //CONVERSION_WARNING: Method 'isset' was converted to '!=' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/isset.htm 
        if (info.smask != null)
        {
            // The alpha stream is raw 8-bit grayscale already zlib-deflated by
            // ImageParser; no PNG predictor is applied so we leave /DecodeParms off.
            var smask = new ImageInfo
            {
                w = info.w,
                h = info.h,
                cs = "DeviceGray",
                bpc = 8,
                f = info.f,
                data = new List<byte[]> { info.smask }
            };
            PutImage(smask);
            if (string.Compare(PdfVersion, "1.4", StringComparison.Ordinal) < 0)
            {
                PdfVersion = "1.4";
            }
        }
        // Palette
        if (TypeSupport.ToString(info.cs) == "Indexed")
        {
            var filter = (Compress) ? "/Filter /FlateDecode " : "";
            var pal = Compress ? GzCompress(info.pal) : info.pal;
            NewObject();
            Out("<<" + filter + "/Length " + pal.Length.ToString(CultureInfo.InvariantCulture) + ">>");
            PutStream(pal);
            Out("endobj");
        }
    }

    internal virtual void PutXObjectDictionary()
    {
        foreach (var image in Images.Values)
        {
            Out("/I" + TypeSupport.ToString(image.i) + " " + TypeSupport.ToString(image.n) + " 0 R");
        }
    }

    internal virtual void PutResourceDictionary()
    {
        Out("/ProcSet [/PDF /Text /ImageB /ImageC /ImageI]");
        Out("/Font <<");
        foreach (var font in Fonts.Values)
        {
            Out("/F" + font.i + " " + font.n + " 0 R");
        }
        Out(">>");
        // Don't emit /XObject <<>> when there are no images. PDF spec
        // permits empty dictionaries, but Acrobat treats them as a
        // structural defect and silently rewrites the file on open --
        // which is the third source of the "save?" prompt this label
        // was hitting (after /CreationDate, the binary marker and the
        // /ID).
        if (Images.Count > 0)
        {
            Out("/XObject <<");
            PutXObjectDictionary();
            Out(">>");
        }
    }

    internal virtual void PutResources()
    {
        PutFonts();
        PutImages();
        // Resource dictionary
        Buffer.Flush();
        Offsets[ 2 ] = Buffer.BaseStream.Length;
        Out("2 0 obj");
        Out("<<");
        PutResourceDictionary();
        Out(">>");
        Out("endobj");
    }

    //CONVERSION_ISSUE: Operator '@' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
    internal virtual void PutInfo()
    {
        Out("/Producer " + TextString("FPDF " + FpdfVersion));
        if (!VariableSupport.Empty(Title))
        {
            Out("/Title " + TextString(Title));
        }
        if (!VariableSupport.Empty(Subject))
        {
            Out("/Subject " + TextString(Subject));
        }
        if (!VariableSupport.Empty(Author))
        {
            Out("/Author " + TextString(Author));
        }
        if (!VariableSupport.Empty(Keywords))
        {
            Out("/Keywords " + TextString(Keywords));
        }
        if (!VariableSupport.Empty(Creator))
        {
            Out("/Creator " + TextString(Creator));
        }
        // PDF date format (PDF 1.7 §7.9.4): D:YYYYMMDDHHmmSSOHH'mm'
        // where O is +, - or Z. The PHP-era format string "YmdHis" got
        // copy-pasted as a .NET format and rendered nonsense like
        // "D:Y4721i12" -- not parseable as a date, which made Acrobat
        // offer to "save the repaired file" every time you opened ours.
        var now = DateTime.Now;
        var tz = TimeZoneInfo.Local.GetUtcOffset(now);
        var tzSign = tz.Ticks >= 0 ? "+" : "-";
        var dateStr = $"D:{now:yyyyMMddHHmmss}{tzSign}{Math.Abs(tz.Hours):D2}'{Math.Abs(tz.Minutes):D2}'";
        Out("/CreationDate " + TextString(dateStr));
    }

    internal virtual void PutCatalog()
    {
        Out("/Type /Catalog");
        Out("/Pages 1 0 R");
        switch (ZoomMode)
        {
            case ZoomEnum.FullPage:
                Out("/OpenAction [3 0 R /Fit]");
                break;
            case ZoomEnum.FullWidth:
                Out("/OpenAction [3 0 R /FitH null]");
                break;
            case ZoomEnum.Real:
                Out("/OpenAction [3 0 R /XYZ null null 1]");
                break;
            case ZoomEnum.Custom:
                Out("/OpenAction [3 0 R /XYZ null null " + sprintf("%.2F", ZoomValue / 100) + "]");
                break;
        }
        switch (LayoutMode)
        {
            case LayoutEnum.Single:
                Out("/PageLayout /SinglePage");
                break;
            case LayoutEnum.Continuous:
                Out("/PageLayout /OneColumn");
                break;
            case LayoutEnum.Two:
                Out("/PageLayout /TwoColumnLeft");
                break;
        }
    }

    internal virtual void PutHeader()
    {
        Out("%PDF-" + PdfVersion);
        // PDF 1.7 §7.5.2: emit a comment line whose body contains at
        // least four bytes >= 128 so any tool that sniffs the file as
        // text (FTP transfers, line-ending converters, mail gateways)
        // recognises it as binary and leaves the bytes alone. Acrobat
        // also expects this marker and considers files without it as
        // needing "repair" -- one of the reasons it asked to save the
        // file on open.
        Out("%\xE2\xE3\xCF\xD3");
    }

    internal virtual void PutTrailer()
    {
        Out("/Size " + (ObjectCount + 1).ToString());
        Out("/Root " + ObjectCount.ToString() + " 0 R");
        Out("/Info " + (ObjectCount - 1).ToString() + " 0 R");
        // PDF 1.7 §14.4: /ID is a two-element array of 16-byte file
        // identifiers. The first ("permanent") never changes once a
        // file is written; the second ("instance") changes on every
        // save -- because we always write a new file, both elements
        // are the same here. Acrobat treats trailers without /ID as
        // needing "repair", which surfaces as the save-on-close
        // prompt the user reported.
        var id = ComputeFileId();
        Out("/ID [<" + id + "><" + id + ">]");
    }

    /// <summary>
    /// Derive a 16-byte (32 hex char) file identifier for the
    /// trailer's /ID entry. Hash a few process-stable bits so the
    /// same document content reliably gives the same ID, but a fresh
    /// run produces a fresh one -- matches what Acrobat would write.
    /// </summary>
    private string ComputeFileId()
    {
        var seed = string.Concat(
            DateTime.UtcNow.Ticks.ToString(),
            Title ?? "",
            Author ?? "",
            Subject ?? "",
            Creator ?? "",
            ObjectCount.ToString());
        using var md5 = System.Security.Cryptography.MD5.Create();
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(bytes);
    }

    internal virtual void EndDoc()
    {
        int i;
        PutHeader();
        PutPages();
        PutResources();
        // Info
        NewObject();
        Out("<<");
        PutInfo();
        Out(">>");
        Out("endobj");
        // Catalog
        NewObject();
        Out("<<");
        PutCatalog();
        Out(">>");
        Out("endobj");
        // Cross-ref
        Buffer.Flush();
        var o = Buffer.BaseStream.Length;
        Out("xref");
        Out("0 " + (ObjectCount + 1).ToString());
        Out("0000000000 65535 f ");
        for (i = 1; i <= ObjectCount; i++)
        {
            // PDF 1.7 §7.5.4: xref entries are exactly 20 bytes long
            // and the offset field is "a 10-digit number, zero-padded".
            // The custom sprintf("%010d", ...) above space-padded ("       211"
            // instead of "0000000211"); the entry still fit in 20 bytes so
            // most readers parsed it, but Acrobat treated each line as
            // structurally suspect and offered to repair the file on close.
            Out(string.Format(CultureInfo.InvariantCulture, "{0:D10} 00000 n ", Offsets[ i ]));
        }
        // Trailer
        Out("trailer");
        Out("<<");
        PutTrailer();
        Out(">>");
        Out("startxref");
        Out(o);
        Out("%%EOF");
        State = 3;
    }

    public virtual byte[] GzCompress(byte[] value)
    {
        var outstream = new MemoryStream();
        using (var g = new GZipStream(outstream, CompressionMode.Compress))
        {
            g.Write(value, 0, value.Length);
        }
        var result = outstream.ToArray();
        return result;
    }

    public virtual byte[] Deflate(byte[] value)
    {
        using (var instream = new MemoryStream(value, false))
        {
            using (var g = new DeflateStream(instream, CompressionMode.Decompress))
            {
                var reader = new BinaryReader(g);
                var bytes = reader.ReadBytes(Int16.MaxValue * 100);
                return bytes;
            }
        }
    }

    public virtual byte[] DeflateCompress(byte[] value)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
            {
                deflateStream.Write(value, 0, value.Length);
            }
            var compressArray = memoryStream.ToArray();
            return compressArray;
        }
    }

    public virtual string DeflateString(byte[] value)
    {

        byte[] uncompressedArray;

        try
        {
            uncompressedArray = Deflate(value);
        }
        catch
        {
            uncompressedArray = GzUncompress(value);
        }
        string result = PrivateEncoding.GetString(uncompressedArray);
        return result;
    }

    public virtual byte[] GzUncompress(byte[] value)
    {
        var instream = new MemoryStream(value, false);
        var g = new GZipStream(instream, CompressionMode.Decompress);
        var reader = new BinaryReader(g);
        byte[] bytes = reader.ReadBytes(Int16.MaxValue * 100);
        g.Dispose();
        return bytes;
    }

    public virtual string GzUncompressString(byte[] value)
    {
        byte[] uncompressedArray = GzUncompress(value);
        string result = PrivateEncoding.GetString(uncompressedArray);
        return result;
    }

    public virtual byte[] GzCompressString(string value)
    {
        byte[] bytes = PrivateEncoding.GetBytes(value);
        byte[] result = GzCompress(bytes);
        return result;
    }



    //ADDONS

    public virtual void DrawArea(Color? fill, double? lineWidth, params DrawingPoint[] points)
    {
        fill = fill ?? Color.Transparent;
        if (points.Length < 3)
            throw new InvalidOperationException("At least three points are required");

        var firstPoint = points.First();
        var doFill = (fill.Value != Color.TransparentBlack && fill.Value != Color.Transparent);
        var doLine = true;

        if (doFill)
            SetFillColor(fill.Value);

        if (lineWidth.HasValue)
            SetLineWidth(lineWidth.Value);

        if (lineWidth == 0)
        {
            doLine = false;
        }

        var pointString = new StringBuilder();
        var start = H - Y;

        foreach (var drawingPoint in points)
        {
            drawingPoint.Y = start - drawingPoint.Y;
        }

        foreach (var drawingPoint in points)
        {
            pointString.Append(sprintf("%.2F %.2F", drawingPoint.X * k, (drawingPoint.Y) * k));
            pointString.Append(drawingPoint == firstPoint ? " m " : " l ");
        }

        if (doFill && doLine)
            pointString.Append(" b ");
        else if (doFill)
            pointString.Append(" f ");
        else if (doLine)
            pointString.Append(" s ");

        this.Out(pointString.ToString());
    }

    public FontDefinition LoadFont(string name, string path)
    {
        if (Fonts.ContainsKey(name.ToLower()))
            return Fonts[ name.ToLower() ];

        var chars = FontBuilder.Fonts
            .OrderBy(x => x.Value.Widths.Count)
            .SelectMany(x => x.Value.Widths.Keys)
            .Distinct()
            .ToArray();

        var fontFace = GetTypeface(path);
        var fontData = new FontDefinition
        {
            FontType = FontTypeEnum.TrueType,
            FontFile = path,
            // PDF Name objects can't contain whitespace or delimiters, so
            // collapse "Roboto Slab" into "RobotoSlab" before letting it
            // anywhere near /BaseFont or /FontName. Acrobat rejects names
            // with embedded spaces; Chrome's PDF.js silently ignores them
            // which is why this only shows up in Acrobat.
            Name = SanitizePdfName(fontFace.FamilyName),
            up = -100,
            ut = 50
        };

        var font = new SKFont(fontFace, 10);
        var lineSpacing = font.GetFontMetrics(out var metrics);
        // Detect a few descriptor flag bits when we can; default to
        // Nonsymbolic (bit 6) for Western Unicode fonts.
        var flags = 32;
        if (fontFace.IsItalic)
            flags |= 1 << 6;            // bit 7 = Italic
        if (fontFace.IsBold)
            flags |= 1 << 18;           // bit 19 = ForceBold
        if (fontFace.IsFixedPitch)
            flags |= 1;             // bit 1 = FixedPitch
        fontData.Flags = flags;
        // Build the FontBBox from Skia's font metrics. Skia uses +Y down
        // while PDF uses +Y up, so the Top (greatest extent above the
        // baseline) is the most-negative Skia Y and becomes the largest
        // positive PDF y, and similarly Bottom (greatest descent) becomes
        // the most negative PDF y. Multiply by 100 because LoadFont sizes
        // the SKFont at 10 pt and the rest of the descriptor is in the
        // 1000-unit em space PDF expects.
        var xMin = (int) Math.Floor(metrics.XMin * 100);
        var xMax = (int) Math.Ceiling(metrics.XMax * 100);
        var yMin = (int) Math.Floor(-metrics.Bottom * 100);
        var yMax = (int) Math.Ceiling(-metrics.Top * 100);
        // Guard against fonts that don't fill in xMin/xMax — fall back to
        // an em-sized box so the bbox is never [0 0 0 0].
        if (xMax <= xMin)
        { xMin = -200; xMax = 1000; }
        if (yMax <= yMin)
        { yMin = -300; yMax = 1000; }
        fontData.FontBBox = new System.Drawing.Rectangle(xMin, yMin, xMax - xMin, yMax - yMin);
        fontData.ItalicAngle = 0;
        fontData.Ascent = Math.Abs(metrics.Ascent * 100);
        fontData.Descent = metrics.Descent * 100;
        // StemV = 0 makes some PDF validators (Acrobat in strict mode) fail.
        // 80 is the conventional default for normal weight, 120 for bold.
        fontData.StemV = fontFace.IsBold ? 120 : 80;
        fontData.CapHeight = metrics.CapHeight * 100;

        foreach (var ch in chars)
        {
            var advance = font.MeasureText(ch) * 100;
            fontData.Widths[ ch ] = advance;
        }

        var tables = fontFace.TableCount;
        FontBuilder.Fonts[ name.ToLower() ] = fontData;

        return fontData;
    }

    /// <summary>
    /// Strip whitespace and PDF Name delimiters from a font family so it
    /// can be safely emitted as /BaseFont and /FontName. PDF Name objects
    /// can't contain whitespace, '(', ')', '&lt;', '&gt;', '[', ']', '{', '}',
    /// '/', '%' or '#'; PostScript naming convention also omits spaces.
    /// </summary>
    private static string SanitizePdfName(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "Unnamed";
        var sb = new StringBuilder(raw.Length);
        foreach (var c in raw)
        {
            if (c <= 0x20 || c >= 0x7F)
                continue;            // whitespace / non-ASCII
            switch (c)
            {
                case '(':
                case ')':
                case '<':
                case '>':
                case '[':
                case ']':
                case '{':
                case '}':
                case '/':
                case '%':
                case '#':
                    continue;
            }
            sb.Append(c);
        }
        return sb.Length == 0 ? "Unnamed" : sb.ToString();
    }

    public static SKTypeface GetTypeface(string fullFontName)
    {
        var result = SKTypeface.FromStream(File.OpenRead(fullFontName));
        return result;
    }


    public void Dispose()
    {
        if (this.State < 3)
            Close();

        Buffer?.Dispose();
    }
}