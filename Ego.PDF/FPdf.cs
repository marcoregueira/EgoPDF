using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Media.Imaging;

using Ionic.Zlib;
using MiscUtil.Conversion;
using MiscUtil.IO;

using Ego.PDF.Data;
using Ego.PDF.Font;
using Ego.PDF.PHP;
using Ego.PDF.Support;

using static Ego.PDF.Printf.SprintfTools;

/*******************************************************************************
* FPDF                                                                         *
*                                                                              *
* Version: 1.7                                                                 *
* Date:    2011-06-18                                                          *
* Author:  Olivier PLATHEY                                                     *
*******************************************************************************/

/*******************************************************************************
* FPDF.net                                                                     *
* .NET port and adaptation                                                     *
* Version: 1.0 prealpha                                                        *
* Date:    2012-09-30                                                          *
* Author:  Marco Antonio Regueira                                              *
*******************************************************************************/

namespace Ego.PDF
{
    public sealed class FPdf
    {
        public static readonly Encoding PrivateEncoding = Encoding.GetEncoding(1252);
        public readonly string FpdfVersion = "1.7";
        public bool ColorFlag;
        public LayoutEnum LayoutMode;
        public double Ws;
        public ZoomEnum ZoomMode;
        public decimal ZoomValue = 1;
        public OrderedMap CMaps { get; set; } = new OrderedMap();
        public OrderedMap Encodings { get; set; } = new OrderedMap();


        public FPdf(PageOrientation orientation, UnitEnum unit, PageSizeEnum pageSize)
        {
            pageSize = pageSize == PageSizeEnum.Default ? PageSizeEnum.A4 : pageSize;
            unit = unit == UnitEnum.Default ? UnitEnum.Milimeter : unit;
            pageSize = pageSize == PageSizeEnum.Default ? PageSizeEnum.A4 : pageSize;

            // Initialization of properties
            Page = 0;
            ObjectCount = 2;
            Buffer = PrivateEncoding.GetString(new byte[] { });
            Offsets = new Dictionary<int, int>();
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
            FpdfFontpath = "C:/";
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

            if (unit == UnitEnum.Point)
            {
                k = 1;
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

        public FPdf() : this(PageOrientation.Portrait, UnitEnum.Milimeter, PageSizeEnum.A4)
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
        public Dictionary<int, int> Offsets { get; set; }

        /// <summary>
        ///     buffer holding in-memory PDF
        /// </summary>
        public string Buffer { get; set; }

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
        ///     dimensions of current page in user unit
        /// </summary>
        public double W { get; set; }

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
        ///     <param name="top"></param>
        /// </summary>
        public void SetMargins(double left, double top)
        {
            SetMargins(left, top, left);
        }

        /// <summary>
        ///     Set left, top and right margins
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        public void SetMargins(double left, double top, double right)
        {
            LeftMargin = left;
            TopMargin = top;
            RightMargin = right;
        }

        public void SetLeftMargin(double margin)
        {
            // Set left margin
            LeftMargin = margin;
            if (Page > 0 && X < margin)
            {
                X = margin;
            }
        }

        public void SetTopMargin(double margin)
        {
            // Set top margin
            TopMargin = margin;
        }

        public void SetRightMargin(double margin)
        {
            // Set right margin
            RightMargin = margin;
        }

        public void SetAutoPageBreak(bool auto, double margin)
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
        public void SetDisplayMode(ZoomEnum zoom, LayoutEnum layout)
        {
            SetZoom(zoom);
            SetLayout(layout);
        }

        public void SetDisplayMode(decimal zoomValue, LayoutEnum layout)
        {
            SetZoom(zoomValue);
            SetLayout(layout);
        }

        public void SetZoom(ZoomEnum zoom)
        {
            if (zoom != ZoomEnum.Default)
                ZoomMode = zoom;
        }

        public void SetZoom(decimal zoom)
        {
            ZoomMode = ZoomEnum.Custom;
            ZoomValue = zoom;
        }

        public void SetLayout(LayoutEnum layout)
        {
            LayoutMode = layout;
        }

        /// <summary>
        ///     Set page compression
        /// </summary>
        /// <param name="compress"></param>
        public void SetCompression(bool compress)
        {
            Compress = compress;
        }

        /// <summary>
        ///     Title of document
        /// </summary>
        /// <param name="title"></param>
        public FPdf SetTitle(string title)
        {
            Title = title;
            return this;
        }

        /// <summary>
        ///     Subject of document
        /// </summary>
        /// <param name="subject"></param>
        public void SetSubject(string subject)
        {
            Subject = subject;
        }

        /// <summary>
        ///     Keywords of document
        /// </summary>
        /// <param name="author"></param>
        public FPdf SetAuthor(string author)
        {
            // Author of document
            Author = author;
            return this;
        }

        /// <summary>
        ///     Keywords of document
        /// </summary>
        /// <param name="keywords"></param>
        public FPdf SetKeywords(string keywords)
        {
            Keywords = keywords;
            return this;
        }

        /// <summary>
        ///     Creator of document
        /// </summary>
        /// <param name="creator"></param>
        public void SetCreator(string creator)
        {
            Creator = creator;
        }

        /// <summary>
        ///     Define an alias for total number of pages
        /// </summary>
        public FPdf AliasNbPages()
        {
            AliasNbPages("{nb}");
            return this;
        }

        /// <summary>
        ///     Define an alias for total number of pages
        /// </summary>
        /// <param name="alias"></param>
        public void AliasNbPages(string alias)
        {
            AliasNbPagesRenamed = alias;
        }

        /// <summary>
        ///     Fatal error
        /// </summary>
        /// <param name="msg"></param>
        public void Error(string msg)
        {
            throw new InvalidOperationException(msg);
        }

        /// <summary>
        ///     Begin document
        /// </summary>
        public void Open()
        {
            State = 1;
        }

        /// <summary>
        ///     Terminate document
        /// </summary>
        public void Close()
        {
            if (State == 3)
            {
                return;
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
        }

        public void AddPage()
        {
            AddPage(PageOrientation.Default, DefPageSize);
        }

        public void AddPage(PageOrientation orientation)
        {
            AddPage(orientation, PageSizeEnum.Default);
        }

        public void AddPage(PageSizeEnum size)
        {
            AddPage(PageOrientation.Default, size);
        }

        public void AddPage(PageOrientation orientation, PageSizeEnum pagesize)
        {
            Dimensions page = GetPageSize(pagesize);
            AddPage(orientation, page);
        }

        public void AddPage(PageOrientation orientation, Dimensions size)
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

        public void Header()
        {
            // To be implemented in your own inherited class
        }

        public void Footer()
        {
            // To be implemented in your own inherited class
        }

        public int PageNo()
        {
            // Get current page number
            return Page;
        }

        public void SetDrawColor(Color color)
        {
            SetDrawColor(color.R, color.G, color.B);
        }

        public void SetDrawColor(int red, int? green, int? blue)
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

        public void SetFillColor(int grey)
        {
            FillColor = sprintf("%.3F g", (double)grey / 255);
            ColorFlag = (FillColor != TextColor);
            if (Page > 0)
            {
                Out(FillColor);
            }
        }


        public void SetFillColor(Color color)
        {
            SetFillColor(color.R, color.G, color.B);
        }

        public void SetFillColor(int red, int green, int blue)
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

        public void SetTextColor(int greyColor)
        {
            TextColor = sprintf("%.3F g", greyColor / 255);
            ColorFlag = (FillColor != TextColor);
        }

        public void SetTextColor(Color color)
        {
            SetTextColor(color.R, color.G, color.B);
        }

        public void SetTextColor(int red, int green, int blue)
        {
            double r = red;
            double g = green;
            double b = blue;
            TextColor = sprintf("%.3F %.3F %.3F rg", r / 255, g / 255, b / 255);
            ColorFlag = (FillColor != TextColor);
        }

        public double GetStringWidth(string s)
        {
            int i;
            double w = 0;
            var l = TypeSupport.ToString(s).Length;
            for (i = 0; i < l; i++)
                w = w + TypeSupport.ToDouble(CurrentFont.Widths[s[i].ToString()]);
            return w * FontSize / 1000;
        }

        public void SetLineWidth(double width)
        {
            // Set line width
            LineWidth = width;
            if (Page > 0)
            {
                Out(sprintf("%.2F w", width * k));
            }
        }

        public void Line(double x1, double y1, double x2, double y2)
        {
            // Draw a line
            Out(sprintf("%.2F %.2F m %.2F %.2F l S",
                x1 * k,
                (H - y1) * k, x2 * k,
                (H - y2) * k));
        }

        public void Rect(double x, double y, double w, double h, string style)
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

        public void AddFont(string family, string style, string file)
        {
            // Add a TrueType, OpenType or Type1 font
            int n;
            family = family.ToLower();
            if (file == "")
            {
                //CONVERSION_WARNING: Method 'str_replace' was converted to 'PHP.StringSupport.StringReplace' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/str_replace.htm 
                file = TypeSupport.ToString(family.Replace(" ", "")) + style.ToLower();
            }
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

            FontDefinition fontInfo = LoadFont(file);
            fontInfo.i = OrderedMap.CountElements(Fonts) + 1;

            if (fontInfo.diff != null)
            {
                // Search existing encodings
                n = Convert.ToInt32(Diffs.Search(fontInfo.diff));
                if (!Convert.ToBoolean(n))
                {
                    n = OrderedMap.CountElements(Diffs) + 1;
                    Diffs[n] = fontInfo.diff;
                }
                fontInfo.diffn = n;
            }

            if (!string.IsNullOrEmpty(fontInfo.file))
            {
                // Embedded font
                if (fontInfo.type == FontTypeEnum.TrueType)
                {
                    FontFiles[fontInfo.file] = new FontDefinition
                    {
                        length1 = fontInfo.originalsize
                    };
                }
                else
                {
                    FontFiles[fontInfo.file] = new FontDefinition
                    {
                        length1 = fontInfo.size1,
                        length2 = fontInfo.size2
                    };
                }
            }
            Fonts[fontkey] = fontInfo;
        }


        public void SetFont(string family)
        {
            SetFont(family, string.Empty);
        }

        public void SetFont(string family, string style)
        {
            SetFont(family, style, 0);
        }

        public void SetFont(string family, string style, double size)
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
                //CONVERSION_WARNING: Method 'str_replace' was converted to 'PHP.StringSupport.StringReplace' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/str_replace.htm 
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
                        AddFont(family, style, "");
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
            CurrentFont = Fonts[fontkey];
            if (Page > 0)
            {
                Out(sprintf("BT /F%d %.2F Tf ET", CurrentFont.i, FontSizePt));
            }
        }

        public void SetFontSize(double size)
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
                Out(sprintf("BT /F%d %.2F Tf ET", CurrentFont.i, FontSizePt));
            }
        }

        public LinkDataInternal AddLink()
        {
            // Create a new internal link
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
                y = (int)(Y);
            }
            if (page == -1)
            {
                page = Page;
            }
            var linkInternal = new LinkDataInternal(page, y);
            Links[link] = linkInternal;
            return linkInternal;
        }

        public void Link(double x, double y, double w, double h, LinkData link)
        {
            Pages[Page].PageLinks.Add(new PageLink(x * k, HPt - y * k, w * k, h * k, link));
        }

        public void Text(double x, double y, string txt)
        {
            // Output a string
            object s;
            s = sprintf("BT %.2F %.2F Td (%s) Tj ET", x * k, (H - y) * k, Escape(txt));
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

        public bool AcceptPageBreak()
        {
            // Accept automatic page break or not
            return AutoPageBreak;
        }

        public void Cell(double cellWidth)
        {
            Cell(cellWidth, null, null, "0", 0, AlignEnum.Default, false, null);
        }

        public void Cell(double cellWidth, double? cellHeight, string text)
        {
            Cell(cellWidth, cellHeight, text, "0", 0, AlignEnum.Default, false, null);
        }

        public void Cell(double cellWidth, double? cellHeight, string text, string border)
        {
            Cell(cellWidth, cellHeight, text, border, 0, AlignEnum.Default, false, null);
        }

        public void Cell(double cellWidth, double? cellHeight, string text, string border, int line)
        {
            Cell(cellWidth, cellHeight, text, border, line, AlignEnum.Default, false, null);
        }

        public void Cell(double cellWidth, double? cellHeight, string text, string border, int line, AlignEnum align)
        {
            Cell(cellWidth, cellHeight, text, border, line, align, false, null);
        }

        public void Cell(double cellWidth, double? cellHeight, string text, string border, int line, AlignEnum align, bool fill)
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
        public void Cell(double cellWidth, double? cellHeight, string text, string border, int line, AlignEnum align, bool fill,
            LinkData link)
        {
            if (!cellHeight.HasValue) cellHeight = 0;

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

                outputString = outputString + sprintf("BT %.2F %.2F Td (%s) Tj ET", (X + dx) * k, (H - (Y + .5 * cellHeight + .3 * FontSize)) * k, txt2);
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

        public void MultiCell(double cellWidth, int cellHeight, string text)
        {
            MultiCell(cellWidth, cellHeight, text, null, AlignEnum.Default, false);
        }

        public void MultiCell(double cellWidth, double cellHeight, string text, string border, AlignEnum align, bool fill)
        {
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
            double ls = 0;
            FontDefinition cw = CurrentFont;
            if (cellWidth == 0)
            {
                cellWidth = W - RightMargin - X;
            }
            wmax = (cellWidth - 2 * CMargin) * 1000 / FontSize;
            text = text.Replace("\r", "");
            textLength = text.Length;
            if (textLength > 0 && text[textLength - 1] == '\n')
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
                var nextChar = text[currentPosition].ToString();
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
                l = l + TypeSupport.ToDouble(cw.Widths[nextChar]);
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

        public double CellMeasure(double cellWidth, double cellHeight, string text)
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
            if (textLength > 0 && text[textLength - 1] == '\n')
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
                var nextChar = text[currentPosition].ToString();
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
                //l = l + TypeSupport.ToDouble(cw.Widths[nextChar]);
                l += cw.Widths[nextChar];
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

        public void BoxedText(double cellWidth, double cellHeight, double fullCellHeight, string text, string border, int line, AlignEnum align, bool fill)
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

        public void Write(int h, string txt)
        {
            Write(h, txt, (LinkData)null);
        }

        public void Write(int h, string txt, int internalLink)
        {
            LinkDataInternal link = Links[internalLink];
            Write(h, txt, link);
        }

        public void Write(int h, string txt, string uri)
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
            var l = 0;
            var nl = 1;
            while (i < nb)
            {
                // Get next character
                string c = s[i].ToString();
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
                l = l + cw.Widths[c];
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
            if (i == j) return;
            //this._out(l + " " + Convert.ToString(this.x, CultureInfo.InvariantCulture) + " " + Convert.ToString(this.ws, CultureInfo.InvariantCulture) + " " + Convert.ToString(this.RightMargin, CultureInfo.InvariantCulture));
            double w2 = (double)l / 1000 * FontSize;
            Cell(w2, h, s.Substring(j), 0.ToString(CultureInfo.InvariantCulture), 0, AlignEnum.Default, false, link);
            //string tail = l + " " + Convert.ToString(this.x, CultureInfo.InvariantCulture) + " " + Convert.ToString(this.ws, CultureInfo.InvariantCulture) + " " + Convert.ToString(this.RightMargin, CultureInfo.InvariantCulture);
            //this._out(tail);
        }

        public void Ln()
        {
            // Line feed; default value is last cell height
            X = LeftMargin;
            Y += Lasth;
        }

        public void Ln(int h)
        {
            // Line feed; 
            X = LeftMargin;
            Y += h;
        }

        public void Image(string file, double? x, double? y, double w)
        {
            Image(file, x, y, w, 0, ImageTypeEnum.Default, (LinkData)null);
        }

        public void Image(string file, double? x, double? y, double w, double h)
        {
            Image(file, x, y, w, h, ImageTypeEnum.Default, (LinkData)null);
        }

        public void Image(string file, double w, double h, ImageTypeEnum type, LinkData link)
        {
            Image(file, null, null, w, h, type, link);
        }

        public void Image(string file, double w, double h, ImageTypeEnum type, string link)
        {
            Image(file, null, null, w, h, type, new LinkDataUri(link));
        }

        public void Image(string file, double? x, double? y, double w, double h, ImageTypeEnum type, string link)
        {
            Image(file, x, y, w, h, type, new LinkDataUri(link));
        }

        public void Image(string file, double? x, double? y, double w, double h, ImageTypeEnum type,
            LinkData link)
        {
            // Put an image on the page
            ImageInfo imageInfo;
            if (!Images.ContainsKey(file))
            {
                // First use of this image, get info
                if (type == ImageTypeEnum.Default)
                {
                    if (!Enum.TryParse(Path.GetExtension(file).Replace(".", string.Empty), true, out type))
                    {
                        Error("Image file has no extension and no type was specified: " + file);
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
                        Error("Image file has no extension and no type was specified or unsupported type (" + file + ")");
                        break;
                }
                imageInfo = imageData;
                imageInfo.i = Images.Count + 1;
                Images[file] = imageInfo;
            }
            else
            {
                imageInfo = Images[file];
            }

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

        public double GetX()
        {
            // Get x position
            return X;
        }

        public void SetX(double x)
        {
            // Set x position
            if (x >= 0)
            {
                X = x;
            }
            else
            {
                X = TypeSupport.ToDouble(W) + x;
            }
        }

        /// <summary>
        ///     Get y position
        /// </summary>
        /// <returns></returns>
        public double GetY()
        {
            return Y;
        }

        /// <summary>
        ///     Set y position and reset x
        /// </summary>
        /// <param name="y"></param>
        public void SetY(double y)
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
        public void SetXY(double x, double y)
        {
            SetY(y);
            SetX(x);
        }


        /*******************************************************************************
        *                                                                              *
        *                              Protected methods                               *
        *                                                                              *
        *******************************************************************************/

        internal Dimensions GetPageSize(PageSizeEnum index)
        {
            PageSize size = StdPageSizes.FirstOrDefault(x => x.Name.ToLower() == index.ToString().ToLower());
            Debug.Assert(size != null, "size != null");
            return size.GetDimensions(k);
        }

        internal Dimensions GetPageSize(Dimensions dimensions)
        {
            return dimensions.Width > dimensions.Heigth
                ? new Dimensions { Width = dimensions.Heigth, Heigth = dimensions.Width }
                : dimensions;
        }

        internal void BeginPage(PageOrientation orientation, Dimensions size)
        {
            Page++;
            Pages[Page] = new Page();
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
                PageSizes[Page] = new Dimensions { Width = WPt, Heigth = HPt };
            }
        }

        internal void EndPage()
        {
            State = 1;
        }

        internal FontDefinition LoadFont(string font)
        {
            // Load a font definition file from the font directory
            FontDefinition fontData;
            FontBuilder.Fonts.TryGetValue(font, out fontData);
            if (string.IsNullOrEmpty(fontData?.name))
            {
                Error("Could not include font definition file");
            }
            return fontData;
        }

        internal object Escape(string s)
        {
            s = s
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)")
                .Replace("\r", "\\r");
            return s;
        }

        internal string TextString(string s)
        {
            // Format a text string
            return "(" + TypeSupport.ToString(Escape(s)) + ")";
        }

        internal string DoUnderline(double x, double y, string txt)
        {
            // Underline text
            var up = CurrentFont.up;
            var ut = CurrentFont.ut;
            var w = GetStringWidth(txt) + Ws * StringSupport.SubstringCount(txt, " ");
            return sprintf("%.2F %.2F %.2F %.2F re f", x * k, (H - (y - up / (double)1000 * FontSize)) * k, w * k,
                (-ut) / (double)1000 * FontSizePt);
        }

        internal ImageInfo ParseJpg(string file)
        {
            // Extract info from a JPEG file

            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Debug.Assert(path != null, "path != null");
            path = Path.Combine(path, file);
            var bi = new BitmapImage(new Uri(path));
            const string colspace = "DeviceRGB";
            var bpc = bi.Format.BitsPerPixel;
            var data = new List<byte[]> { FileSystemSupport.ReadContentBytes(file) };
            return new ImageInfo
            {
                w = bi.PixelWidth,
                h = bi.PixelHeight,
                cs = colspace,
                bpc = bpc,
                f = "DCTDecode",
                data = data
            };
        }

        /// <summary>
        /// Extract info from a PNG file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        internal ImageInfo ParsePng(string file)
        {
            var f = FileSystemSupport.FileOpen(file, "rb");
            if (!TypeSupport.ToBoolean(f))
            {
                Error("Can\'t open image file: " + file);
            }

            EndianBitConverter converter = new BigEndianBitConverter();
            var reader = new EndianBinaryReader(converter, f, Encoding.ASCII);
            var info = ParsePngStream(f, reader, file);
            reader.Close();
            return info;
        }

        internal ImageInfo ParsePngStream(FileStream f, EndianBinaryReader reader, string file)
        {
            int n;
            int pos;
            var signature = ReadStream(reader, 8);
            if (!signature.Contains("PNG"))
            {
                Error("Not a PNG file: " + file);
            }

            // Read header chunk
            ReadStream(reader, 4);
            if (ReadStream(reader, 4) != "IHDR")
            {
                Error("Incorrect PNG file: " + file);
            }
            var w = reader.ReadInt32();
            var height = reader.ReadInt32();
            int bpc = ReadStream(reader, 1)[0];
            if (bpc > 8)
            {
                Error("16-bit depth not supported: " + file);
            }
            int ct = ReadStream(reader, 1)[0];

            var colspace = "DeviceRGB";

            switch (ct)
            {
                case 4:
                case 0:
                    colspace = "DeviceGray";
                    break;
                case 6:
                case 2:
                    colspace = "DeviceRGB";
                    break;
                case 3:
                    colspace = "Indexed";
                    break;
                default:
                    Error("Unknown color type: " + file);
                    break;
            }
            if (ReadStream(reader, 1)[0] != 0)
            {
                Error("Unknown compression method: " + file);
            }
            if (ReadStream(reader, 1)[0] != 0)
            {
                Error("Unknown filter method: " + file);
            }
            if (ReadStream(reader, 1)[0] != 0)
            {
                Error("Interlacing not supported: " + file);
            }
            ReadStream(reader, 4);
            var dp = "/Predictor 15 /Colors " + ((colspace == "DeviceRGB") ? 3 : 1) + " /BitsPerComponent " +
                     bpc.ToString() + " /Columns " + w;

            // Scan chunks looking for palette, transparency and image data
            var pal = new byte[] { };
            var trns = new int[] { };
            var data = new byte[] { };
            do
            {
                n = reader.ReadInt32();
                var type = ReadStream(reader, 4);
                switch (type)
                {
                    case "PLTE":
                        pal = ReadStreamBytes(reader, n);
                        ReadStream(reader, 4);
                        break;
                    case "tRNS":
                        {
                            // Read transparency info
                            var t = ReadStream(reader, n);
                            switch (ct)
                            {
                                case 0:
                                    trns = new[] { Convert.ToInt32(t[1]) };
                                    // new PHP.OrderedMap((int)t.Substring(1, 1)[0]);
                                    break;
                                case 2:
                                    trns = new[] { Convert.ToInt32(t[1]), Convert.ToInt32(t[3]), Convert.ToInt32(t[5]) };
                                    break;
                                default:
                                    pos = t.IndexOf(Convert.ToString((char)0));
                                    if (pos >= 0)
                                    {
                                        trns = new[] { pos };
                                    }
                                    break;
                            }
                            ReadStream(reader, 4);
                        }
                        break;
                    case "IDAT":
                        data = ReadStreamBytes(reader, n);
                        ReadStream(reader, 4);
                        break;
                    case "IEND":
                        break;
                    default:
                        ReadStream(reader, n + 4);
                        break;
                }
            } while (Convert.ToBoolean(n));

            if (colspace == "Indexed" && VariableSupport.Empty(pal))
            {
                Error("Missing palette in " + file);
            }
            var info = new ImageInfo
            {
                w = w,
                h = height,
                cs = colspace,
                bpc = bpc,
                f = "FlateDecode",
                dp = dp,
                pal = pal,
                trns = trns
            };

            if (ct >= 4)
            {
                // Extract alpha channel
                var newData = GzUncompressString(data);
                var color = new StringBuilder();
                var alpha = new StringBuilder();
                int len;
                string line;
                if (ct == 4)
                {
                    // Gray image
                    len = 2 * w;
                    for (var i = 0; i < height; i++)
                    {
                        pos = (1 + len) * i;
                        color.Append(newData[pos]);
                        alpha.Append(newData[pos]);
                        line = newData.Substring(pos + 1, len);
                        for (var posLinea = 0; posLinea < line.Length; posLinea += 2)
                        {
                            color.Append(line[posLinea]);
                            alpha.Append(line[posLinea + 1]);
                        }
                    }
                }
                else
                {
                    // RGB image
                    len = 4 * w;
                    for (var i = 0; i < height; i++)
                    {
                        pos = (1 + len) * i;
                        color.Append(newData[pos]);
                        alpha.Append(newData[pos]);
                        line = newData.Substring(pos + 1, len);
                        for (var posLinea = 0; posLinea < line.Length; posLinea += 4)
                        {
                            color.Append(line.Substring(posLinea, 3));
                            alpha.Append(line[posLinea + 3]);
                        }
                    }
                }
                data = GzCompressString(color.ToString());
                info.data.Add(data);
                info.smask = GzCompressString(alpha.ToString());
                if (String.Compare(PdfVersion, "1.4", StringComparison.Ordinal) < 0)
                {
                    PdfVersion = "1.4";
                }
            }
            else
            {
                info.data = new List<byte[]> { data };
            }
            return info;
        }

        internal byte[] ReadStreamBytes(EndianBinaryReader br, int n)
        {
            byte[] result = br.ReadBytes(n);
            return result;
        }

        internal string ReadStream(EndianBinaryReader br, int n)
        {
            // Read n bytes from stream
            string s;
            string res = "";

            while (n > 0 && !(br.BaseStream.Position >= br.BaseStream.Length))
            {
                s = FileSystemSupport.Read(br, n);
                if (s == null)
                {
                    Error("Error while reading stream");
                }
                n -= s.Length;
                res += s;
            }
            if (n > 0)
            {
                Error("Unexpected end of stream");
            }
            return res;
        }

        internal Int32 ReadInt(FileStream f, BinaryReader br)
        {
            Int32 a = br.ReadInt32();
            return a;
        }

        internal ImageInfo ParseGif(string file)
        {
            throw new NotImplementedException();
            /*
            // Extract info from a GIF file (via PNG conversion)
            int im;
            System.IO.FileStream f;
            string data;
            PHP.OrderedMap info;
            string tmp;
            if (!(this.GetType().GetMethod("imagepng") != null))
            {
                this.Error("GD extension is required for GIF support");
            }
            if (!(this.GetType().GetMethod("imagecreatefromgif") != null))
            {
                this.Error("GD has no GIF read support");
            }
            im = imagecreatefromgif(file);
            if (!System.Convert.ToBoolean(im))
            {
                this.Error("Missing or incorrect image file: " + file);
            }
            imageinterlace(im, 0);
            try
            {
                //CONVERSION_WARNING: Method 'fopen' was converted to 'PHP.FileSystemSupport.FileOpen' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fopen.htm 
                f = PHP.FileSystemSupport.FileOpen("php://temp", "rb+");
            }
            catch (System.Exception)
            {
            }
            if (PHP.TypeSupport.ToBoolean(f))
            {
                // Perform conversion in memory
                ob_start();
                imagepng(im);
                data = ob_get_clean() ? "1" : "";
                imagedestroy(im);
                //CONVERSION_WARNING: Method 'fwrite' was converted to 'PHPFileSystemSupport.Write' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fwrite.htm 
                PHP.FileSystemSupport.Write(f, data, -1);
                //CONVERSION_WARNING: Method 'rewind' was converted to 'PHPFileSystemSupport.Rewind' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/rewind.htm 
                PHP.FileSystemSupport.Rewind(f);
                info = this._parsepngstream(f, file);
                //CONVERSION_WARNING: Method 'fclose' was converted to 'PHP.FileSystemSupport.Close' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fclose.htm 
                PHP.FileSystemSupport.Close(f);
            }
            else
            {
                // Use temporary file
                //CONVERSION_WARNING: Method 'tempnam' was converted to 'System.IO.Path.GetTempFileName' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/tempnam.htm 
                tmp = System.IO.Path.GetTempFileName();
                if (!PHP.TypeSupport.ToBoolean(tmp))
                {
                    this.Error("Unable to create a temporary file");
                }
                if (!System.Convert.ToBoolean(imagepng(im, tmp)))
                {
                    this.Error("Error while saving to temporary file");
                }
                imagedestroy(im);
                info = this._parsepng(tmp);
                //CONVERSION_WARNING: Method 'unlink' was converted to 'System.IO.File.Delete' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/unlink.htm 
                System.IO.File.Delete(tmp);
            }
            return info;

             */
        }

        internal void NewObject()
        {
            // Begin a new object
            ObjectCount++;
            Offsets[ObjectCount] = Buffer.Length;
            Out(ObjectCount.ToString() + " 0 obj");
        }

        internal void PutStream(string s)
        {
            Out("stream");
            Out(s);
            Out("endstream");
        }

        internal void PutStream(byte[] bytes)
        {
            Out("stream");
            Out(bytes);
            Out("endstream");
        }

        internal void PutStream(List<byte[]> bytes)
        {
            Out("stream");
            Out(bytes);
            Out("endstream");
        }

        internal void PutStreamObject(string data)
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

        internal void Out(object s)
        {
            // Add a line to the document
            if (State == 2)
            {
                //TODO: APPENDLN ?
                Pages[Page]
                    .Append(TypeSupport.ToString(s));
                Pages[Page]
                    .Append("\n");
            }
            else
            {
                if (s is List<byte[]>)
                {
                    foreach (var v in (s as List<byte[]>))
                    {
                        Buffer += PrivateEncoding.GetString(v);
                    }
                    Buffer += "\n";
                }
                else if (s is byte[])
                {
                    Buffer += PrivateEncoding.GetString((byte[])s);
                    Buffer += "\n";
                }
                else
                {
                    Buffer += TypeSupport.ToString(s) + "\n";
                }
            }
        }

        internal void PutPages()
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
                    Out(sprintf("/MediaBox [0 0 %.2F %.2F]", PageSizes[currentPage.Key].Width, PageSizes[currentPage.Key].Heigth));
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
                            var link = Links[(pl.Link as LinkDataInternal).InternalLink];
                            var pageIndex = link.PageIndex;
                            var h = (PageSizes.ContainsKey(pageIndex)) ? PageSizes[pageIndex].Heigth : hPt;
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
            Offsets[1] = Buffer.Length;
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

        internal string ToUnicodeCMap(Dictionary<int, object> uv)
        {
            var ranges = new StringBuilder();
            var chars = new StringBuilder();
            int nbr = 0;
            int nbc = 0;
            foreach (var key in uv.Keys)
            {
                var keyValue = Convert.ToInt32(key);
                var value = uv[key];
                var values = value as int[];
                if (values != null)
                {
                    ranges.Append(sprintf("<%02X> <%02X> <%04X>\n", keyValue, keyValue + values[1] - 1, values[0]));
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
            var s =
                "/CIDInit /ProcSet findresource begin\n";
            s += "12 dict begin\n";
            s += "begincmap\n";
            s += "/CIDSystemInfo\n";
            s += "<</Registry (Adobe)\n";
            s += "/Ordering (UCS)\n";
            s += "/Supplement 0\n";
            s += ">> def\n";
            s += "/CMapName /Adobe-Identity-UCS def\n";
            s += "/CMapType 2 def\n";
            s += "1 begincodespacerange\n";
            s += "<00> <FF>\n";
            s += "endcodespacerange\n";
            if (nbr > 0)
            {
                s += $"{nbr} beginbfrange\n";
                s += ranges.ToString();
                s += "endbfrange\n";
            }
            if (nbc > 0)
            {
                s += $"{nbc} beginbfchar\n";
                s += chars;
                s += "endbfchar\n";
            }
            s += "endcmap\n";
            s += "CMapName currentdict /CMap defineresource pop\n";
            s += "end\n";
            s += "end";
            return s;
        }

        internal void PutFonts()
        {


            foreach (var file in FontFiles.Keys)
            {
                FontDefinition info = Fonts[file];
                // Font file embedding
                NewObject();
                info.n = ObjectCount;
                //file_get_contents' returns a string 
                var font = FileSystemSupport.ReadContents(Fontpath + file);
                if (string.IsNullOrWhiteSpace(font))
                {
                    Error("Font file not found: " + file);
                }
                var extension = Path.GetExtension(file);
                var compressed = (extension == ".z");
                if (!compressed && info.length2 > 0)
                {
                    font = TypeSupport.ToString(font).Substring(6, info.length1)
                           + TypeSupport.ToString(font).Substring(6 + info.length1 + 6, info.length2);
                }
                Out("<</Length " + font.Length);
                if (compressed)
                {
                    Out("/Filter /FlateDecode");
                }
                Out("/Length1 " + TypeSupport.ToString(info.length1));
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
                var font1 = Fonts[fontKey];

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
                        this.Encodings[font1.enc] = ObjectCount;
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
                        cmapkey = font1.name;
                    }

                    if (!CMaps.KeyExists(cmapkey))
                    {
                        var cmap = ToUnicodeCMap(font1.uv);
                        PutStreamObject(cmap);
                        CMaps[cmapkey] = this.ObjectCount;
                    }
                }

                // Font objects
                Fonts[fontKey].n = ObjectCount + 1;
                var type = font1.type;
                var name = font1.name;
                if (font1.Subsetted)
                {
                    name = "AAAAAA" + name;
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
                        if (font1.uv.Count>0)
                            Out("/ToUnicode " + CMaps[cmapkey] + " 0 R");
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
                                Out("/Encoding " + Encodings[font1.enc] + " 0 R");
                            }
                            else
                            {
                                Out("/Encoding /WinAnsiEncoding");
                            }
                            if (font1.uv.Count > 0)
                                Out("/ToUnicode " + CMaps[cmapkey] + " 0 R");
                            Out(">>");
                            Out("endobj");
                            // Widths
                            NewObject();
                            var cw = TypeSupport.ToArray(font1.cw);
                            string s = "[";
                            int i;
                            for (i = 32; i <= 255; i++)
                                s += TypeSupport.ToString(cw[Convert.ToString((char)i)]) + " ";
                            Out(s + "]");
                            Out("endobj");
                            // Descriptor
                            NewObject();
                            s = "<</Type /FontDescriptor /FontName /" + TypeSupport.ToString(name);
                            foreach (string k1 in font1.desc.Keys)
                            {
                                string v = font1.desc[k1];
                                s += " /" + k1 + " " + v; // TypeSupport.ToString(v);
                            }

                            if (!string.IsNullOrEmpty(font1.file))
                            {
                                s += " /FontFile" + (type == FontTypeEnum.Type1 ? "" : "2") + " " +
                                     TypeSupport.ToString(Fonts[font1.file].n) + " 0 R";
                            }
                            Out(s + ">>");
                            Out("endobj");
                        }
                        break;
                    default:
                        Error("Unsupported font type: " + TypeSupport.ToString(type));
                        break;
                }
            }
        }

        internal void PutImages()
        {
            foreach (var file in Images)
            {
                PutImage(file.Value);
                file.Value.data = null; //unset, probably not needed
                file.Value.smask = null; //unset, probably not needed
            }
        }

        internal void PutImage(ImageInfo info)
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
                string dp = "/Predictor 15 /Colors 1 /BitsPerComponent 8 /Columns " + TypeSupport.ToString(info.w);
                var smask = new ImageInfo
                {
                    w = info.w,
                    h = info.h,
                    cs = "DeviceGray",
                    bpc = 8,
                    f = info.f,
                    dp = dp,
                    data = new List<byte[]> { info.smask }
                };
                /*
                    smask = new PHP.OrderedMap(... new object[] { "data", info.smask });
                */
                PutImage(smask);
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

        internal void PutXObjectDictionary()
        {
            foreach (var image in Images.Values)
            {
                Out("/I" + TypeSupport.ToString(image.i) + " " + TypeSupport.ToString(image.n) + " 0 R");
            }
        }

        internal void PutResourceDictionary()
        {
            Out("/ProcSet [/PDF /Text /ImageB /ImageC /ImageI]");
            Out("/Font <<");
            foreach (var font in Fonts.Values)
            {
                Out("/F" + font.i + " " + font.n + " 0 R");
            }
            Out(">>");
            Out("/XObject <<");
            PutXObjectDictionary();
            Out(">>");
        }

        internal void PutResources()
        {
            PutFonts();
            PutImages();
            // Resource dictionary
            Offsets[2] = Buffer.Length;
            Out("2 0 obj");
            Out("<<");
            PutResourceDictionary();
            Out(">>");
            Out("endobj");
        }

        //CONVERSION_ISSUE: Operator '@' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
        internal void PutInfo()
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
            //CONVERSION_WARNING: Method 'date' was converted to 'System.DateTime.ToString' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/date.htm 
            //CONVERSION_ISSUE: Operator '@' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
            Out("/CreationDate " + TextString("D:" + DateTime.Now.ToString("YmdHis")));
        }

        internal void PutCatalog()
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

        internal void PutHeader()
        {
            Out("%PDF-" + PdfVersion);
        }

        internal void PutTrailer()
        {
            Out("/Size " + (ObjectCount + 1).ToString());
            Out("/Root " + ObjectCount.ToString() + " 0 R");
            Out("/Info " + (ObjectCount - 1).ToString() + " 0 R");
        }

        internal void EndDoc()
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
            var o = Buffer.Length;
            Out("xref");
            Out("0 " + (ObjectCount + 1).ToString());
            Out("0000000000 65535 f ");
            for (i = 1; i <= ObjectCount; i++)
            {
                Out(sprintf("%010d 00000 n ", Offsets[i]));
                /*
                 * Warning: string.format has a different behaviour for negative numbers
                if (this.offsets[i] < 0)
                {
                    this._out(string.Format(CultureInfo.InvariantCulture, "{0:D9} 00000 n ", this.offsets[i]));
                }
                else
                {
                    this._out(string.Format(CultureInfo.InvariantCulture, "{0:D10} 00000 n ", this.offsets[i]));
                }*/
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

        public byte[] GzCompress(byte[] value)
        {
            var outstream = new MemoryStream();
            var g = new ZlibStream(outstream, CompressionMode.Compress);
            g.Write(value, 0, value.Length);
            g.Close();
            byte[] result = outstream.ToArray();
            return result;
        }

        public byte[] GzUncompress(byte[] value)
        {
            var instream = new MemoryStream(value, false);
            var g = new ZlibStream(instream, CompressionMode.Decompress);
            var reader = new BinaryReader(g);
            byte[] bytes = reader.ReadBytes(Int16.MaxValue * 100);
            g.Close();
            return bytes;
        }

        public string GzUncompressString(byte[] value)
        {
            byte[] uncompressedArray = GzUncompress(value);
            string result = PrivateEncoding.GetString(uncompressedArray);
            return result;
        }

        public byte[] GzCompressString(string value)
        {
            byte[] bytes = PrivateEncoding.GetBytes(value);
            byte[] result = GzCompress(bytes);
            return result;
        }



        //ADDONS

        public void DrawArea(Color? fill, double? lineWidth, params DrawingPoint[] points)
        {
            fill = fill ?? Color.Empty;
            if (points.Length < 3) throw new InvalidOperationException("At least three points are required");

            var firstPoint = points.First();
            var doFill = (fill.Value != Color.Empty && fill.Value != Color.Transparent);
            var doLine = true;

            if (doFill)
                SetFillColor(fill.Value);

            if (lineWidth.HasValue)
                SetLineWidth(lineWidth.Value);

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

            if (doFill && doLine) pointString.Append(" b ");
            else if (doFill) pointString.Append(" f ");
            else if (doLine) pointString.Append(" s ");

            this.Out(pointString.ToString());
        }
    }
}