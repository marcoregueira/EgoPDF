using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Media.Imaging;
using Ego.PDF.Data;
using Ego.PDF.Font;
using Ego.PDF.PHP;
using Ionic.Zlib;
using Ego.PDF.Support;

using MiscUtil.Conversion;
using MiscUtil.IO;

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
    public class FPdf
    {
        public static readonly Encoding PrivateEncoding = Encoding.GetEncoding(1252);
        public readonly string FpdfVersion = "1.7";
        public bool ColorFlag;
        public string LayoutMode;
        public double Ws;
        public string ZoomMode;

        public FPdf(PageOrientation orientation, UnitEnum unit, PageSizeEnum pageSize)
        {
            pageSize = pageSize == PageSizeEnum.Default ? PageSizeEnum.A4 : pageSize;
            unit = unit == UnitEnum.Default ? UnitEnum.Milimeter : unit;
            pageSize = pageSize == PageSizeEnum.Default ? PageSizeEnum.A4 : pageSize;

            // Initialization of properties
            Page = 0;
            ObjectCount = 2;
            Buffer = PrivateEncoding.GetString(new byte[] {});
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
            CoreFonts = new List<string> {"courier", "helvetica", "times", "symbol", "zapfdingbats"};
            // Scale factor
            k = 72/25.4; // unidades en milímetros

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
                k = 72/25.4;
            }
            else if (unit == UnitEnum.Centimeter)
            {
                k = 72/2.54;
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
            CurPageSize = size;

            // Page orientation
            DefOrientation = orientation;
            CurOrientation = DefOrientation;

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

            WPt = W*k;
            HPt = H*k;
            // Page margins (1 cm)
            double margin = 28.35/k;
            SetMargins(margin, margin);
            // Interior cell margin (1 mm)
            CMargin = margin/10;
            // Line width (0.2 mm)
            LineWidth = .567/k;
            // Automatic page break
            SetAutoPageBreak(true, 2*margin);
            // Default display mode
            SetDisplayMode("default", "default");
            // Enable compression

            //TODO: PONER TRUE
            SetCompression(false);
            // Set default PDF version number
            PdfVersion = "1.3";
        }

        public FPdf() : this(PageOrientation.Portrait, UnitEnum.Milimeter, PageSizeEnum.A4)
        {
        }

        /// <summary>
        ///     current page number
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        ///     current object number
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
        public PageOrientation CurOrientation { get; set; }

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
        public Dimensions CurPageSize { get; set; }

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
        public double FontSizePt { get; set; }
        public double FontSize { get; set; }
        public string DrawColor { get; set; }
        public string FillColor { get; set; }
        public string TextColor { get; set; }
        public Dictionary<string, ImageInfo> Images { get; set; }
        public List<LinkDataInternal> Links { get; set; }
        public bool AutoPageBreak { get; set; }
        public double PageBreakTrigger { get; set; }
        public bool InHeader { get; set; }
        public bool InFooter { get; set; }
        public string Title { get; set; }
        public string Subject { get; set; }
        public string Author { get; set; }
        public string Keywords { get; set; }
        public string Creator { get; set; }
        public string AliasNbPagesRenamed { get; set; }
        public string PdfVersion { get; set; }

        public string FpdfFontpath { get; set; }
        private string Fontpath { get; set; }


        /// <summary>
        ///     El margen derecho es igual al izquierdo
        ///     <param name="top"></param>
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
        public virtual void SetDisplayMode(string zoom, string layout)
        {
            if (zoom == "fullpage" || zoom == "fullwidth" || zoom == "real" || zoom == "default")
            {
                ZoomMode = zoom;
            }
            else
            {
                Error("Incorrect zoom display mode: " + zoom);
            }
            if (layout == "single" || layout == "continuous" || layout == "two" || layout == "default")
            {
                LayoutMode = layout;
            }
            else
            {
                Error("Incorrect layout display mode: " + layout);
            }
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
        public FPdf AliasNbPages()
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
        public virtual void Close()
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
            _enddoc();
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
            _out("2 J");
            // Set line width
            LineWidth = lw;
            _out(sprintf("%.2F w", lw*k));

            // Set font
            if (TypeSupport.ToBoolean(family))
            {
                SetFont(family, style, fontsize);
            }
            // Set colors
            DrawColor = dc;
            if (TypeSupport.ToString(dc) != "0 G")
            {
                _out(dc);
            }
            FillColor = fc;
            if (TypeSupport.ToString(fc) != "0 g")
            {
                _out(fc);
            }
            TextColor = tc;
            ColorFlag = cf;
            // Page header
            InHeader = true;
            Header();
            InHeader = false;
            // Restore line width
            if (LineWidth != lw)
            {
                LineWidth = lw;
                _out(sprintf("%.2F w", lw*k));
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
                _out(dc);
            }
            //CONVERSION_WARNING: Converted Operator might not behave as expected. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1009.htm 
            if (FillColor != fc)
            {
                FillColor = fc;
                _out(fc);
            }
            TextColor = tc;
            ColorFlag = cf;
        }

        public virtual void Header()
        {
            // To be implemented in your own inherited class
        }

        public virtual void Footer()
        {
            // To be implemented in your own inherited class
        }

        public virtual int PageNo()
        {
            // Get current page number
            return Page;
        }

        public virtual void SetDrawColor(int red, int? green, int? blue)
        {
            int r = red;
            int? g = green;
            int? b = blue;

            // Set color for all stroking operations
            if ((r == 0 && g == 0 && b == 0) || (!g.HasValue))
            {
                DrawColor = sprintf("%.3F G", r/255);
            }
            else
            {
                DrawColor = sprintf("%.3F %.3F %.3F RG", r/255, g/255, b/255);
            }
            if (Page > 0)
            {
                _out(DrawColor);
            }
        }

        public virtual void SetFillColor(int grey)
        {
            FillColor = sprintf("%.3F g", grey/255);
            ColorFlag = (FillColor != TextColor);
            if (Page > 0)
            {
                _out(FillColor);
            }
        }

        public virtual void SetFillColor(int red, int green, int blue)
        {
            int r = red;
            int g = green;
            int b = blue;

            // Set color for all filling operations
            FillColor = sprintf("%.3F %.3F %.3F rg", r/255, g/255, b/255);
            ColorFlag = (FillColor != TextColor);
            if (Page > 0)
            {
                _out(FillColor);
            }
        }

        public void SetTextColor(int greyColor)
        {
            TextColor = sprintf("%.3F g", greyColor/255);
            ColorFlag = (FillColor != TextColor);
        }

        public virtual void SetTextColor(int red, int green, int blue)
        {
            double r = red;
            double g = green;
            double b = blue;
            TextColor = sprintf("%.3F %.3F %.3F rg", r/255, g/255, b/255);
            ColorFlag = (FillColor != TextColor);
        }

        public virtual double GetStringWidth(string s)
        {
            int i;
            double w = 0;
            var l = TypeSupport.ToString(s).Length;
            for (i = 0; i < l; i++)
                w = w + TypeSupport.ToDouble(CurrentFont.Widths[s[i].ToString()]);
            return w*FontSize/1000;
        }

        public virtual void SetLineWidth(double width)
        {
            // Set line width
            LineWidth = width;
            if (Page > 0)
            {
                _out(sprintf("%.2F w", width*k));
            }
        }

        public virtual void Line(double x1, double y1, double x2, double y2)
        {
            // Draw a line
            _out(sprintf("%.2F %.2F m %.2F %.2F l S",
                         x1*k,
                         (H - y1)*k, x2*k,
                         (H - y2)*k));
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
            _out(sprintf("%.2F %.2F %.2F %.2F re %s", x*k, (H - y)*k, w*k, (-h)*k, op));
        }

        public virtual void AddFont(string family, string style, string file)
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


        public virtual void SetFont(string family)
        {
            SetFont(family, string.Empty);
        }

        public virtual void SetFont(string family, string style)
        {
            SetFont(family, style, 0);
        }

        public virtual void SetFont(string family, string style, double size)
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
            FontSize = size/k;
            CurrentFont = Fonts[fontkey];
            if (Page > 0)
            {
                _out(sprintf("BT /F%d %.2F Tf ET", CurrentFont.i, FontSizePt));
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
            FontSize = size/k;
            if (Page > 0)
            {
                _out(sprintf("BT /F%d %.2F Tf ET", CurrentFont.i, FontSizePt));
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

        public virtual LinkDataInternal SetLink(int link, double y)
        {
            LinkDataInternal l = SetLink(link, y, -1);
            return l;
        }

        public virtual LinkDataInternal SetLink(int link, double y, int page)
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
            Links[link] = linkInternal;
            return linkInternal;
        }

        public virtual void Link(double x, double y, double w, double h, LinkData link)
        {
            Pages[Page].PageLinks.Add(new PageLink(x*k, HPt - y*k, w*k, h*k, link));
        }

        public virtual void Text(double x, double y, string txt)
        {
            // Output a string
            object s;
            s = sprintf("BT %.2F %.2F Td (%s) Tj ET", x*k, (H - y)*k, Escape(txt));
            if (Underline && TypeSupport.ToString(txt) != "")
            {
                s = TypeSupport.ToString(s) + " " + DoUnderline(x, y, txt);
            }
            if (ColorFlag)
            {
                s = "q " + TypeSupport.ToString(TextColor) + " " + TypeSupport.ToString(s) + " Q";
            }
            _out(s);
        }

        public virtual bool AcceptPageBreak()
        {
            // Accept automatic page break or not
            return AutoPageBreak;
        }

        public virtual void Cell(double w)
        {
            Cell(w, null, null, "0", 0, AlignEnum.Default, false, null);
        }

        public virtual void Cell(double w, double? h, string txt)
        {
            Cell(w, h, txt, "0", 0, AlignEnum.Default, false, null);
        }

        public virtual void Cell(double w, double? h, string txt, string border)
        {
            Cell(w, h, txt, border, 0, AlignEnum.Default, false, null);
        }

        public virtual void Cell(double w, double? h, string txt, string border, int ln)
        {
            Cell(w, h, txt, border, ln, AlignEnum.Default, false, null);
        }

        public virtual void Cell(double w, double? h, string txt, string border, int ln, AlignEnum align)
        {
            Cell(w, h, txt, border, ln, align, false, null);
        }

        public virtual void Cell(double w, double? h, string txt, string border, int ln, AlignEnum align, bool fill)
        {
            Cell(w, h, txt, border, ln, align, fill, null);
        }

        public virtual void Cell(double w, double? h, string txt, string border, int ln, AlignEnum align, bool fill,
                                 LinkData link)
        {
            // Output a cell
            string s;

            if (!h.HasValue) h = 0;

            if (Y + h > PageBreakTrigger && !InHeader && !InFooter && AcceptPageBreak())
            {
                // Automatic page break
                double xxx = X;
                double ws = Ws;
                if (ws > 0)
                {
                    Ws = 0;
                    _out("0 Tw");
                }
                AddPage(CurOrientation, CurPageSize);
                X = xxx;
                if (ws > 0)
                {
                    Ws = ws;
                    _out(sprintf("%.3F Tw", ws*k));
                }
            }

            if (w == 0)
            {
                w = W - RightMargin - X;
            }

            s = string.Empty;

            int borderi = TypeSupport.ToInt32(border);
            if (fill || borderi == 1)
            {
                string op = string.Empty;
                if (fill)
                {
                    op = (borderi == 1) ? "B" : "f";
                }
                else
                {
                    op = "S";
                }
                s = sprintf("%.2F %.2F %.2F %.2F re %s ", X*k, (H - Y)*k, w*k, -h*k, op);
            }

            if (!string.IsNullOrEmpty(border))
            {
                if (border.Contains("L"))
                {
                    s = s + sprintf("%.2F %.2F m %.2F %.2F l S ", X*k, (H - Y)*k, X*k, (H - (Y + h))*k);
                }
                if (border.Contains("T"))
                {
                    s = s + sprintf("%.2F %.2F m %.2F %.2F l S ", X*k, (H - Y)*k, (X + w)*k, (H - Y)*k);
                }
                if (border.Contains("R"))
                {
                    s = s +
                        sprintf("%.2F %.2F m %.2F %.2F l S ", (X + w)*k, (H - Y)*k, (X + w)*k, (H - (Y + h))*k);
                }
                if (border.Contains("B"))
                {
                    s = s +
                        sprintf("%.2F %.2F m %.2F %.2F l S ", X*k, (H - (Y + h))*k, (X + w)*k, (H - (Y + h))*k);
                }
            }

            if (!string.IsNullOrEmpty(txt))
            {
                double dx;
                if (align == AlignEnum.Right)
                {
                    dx = w - CMargin - GetStringWidth(txt);
                }
                else if (align == AlignEnum.Center)
                {
                    dx = (w - GetStringWidth(txt))/2;
                }
                else
                {
                    dx = CMargin;
                }
                if (ColorFlag)
                {
                    s = s + "q " + TypeSupport.ToString(TextColor) + " ";
                }

                string txt2 = txt
                    .Replace("\\", "\\\\")
                    .Replace("(", "\\(")
                    .Replace(")", "\\)");

                s = s + sprintf("BT %.2F %.2F Td (%s) Tj ET", (X + dx)*k, (H - (Y + .5*h + .3*FontSize))*k, txt2);
                if (Underline)
                {
                    s = s + " " + DoUnderline(X + dx, Y + .5*h.Value + .3*FontSize, txt);
                }
                if (ColorFlag)
                {
                    s = s + " Q";
                }
                if (TypeSupport.ToBoolean(link))
                {
                    Link(X + dx, Y + .5*h.Value - .5*FontSize, GetStringWidth(txt), FontSize, link);
                }
            }
            if (!string.IsNullOrEmpty(s))
            {
                _out(s);
            }
            Lasth = h.Value;
            if (ln > 0)
            {
                // Go to next line
                Y += h.Value;
                if (ln == 1)
                {
                    X = LeftMargin;
                }
            }
            else
            {
                X += w;
            }
        }

        public virtual void MultiCell(double w, int h, string txt)
        {
            MultiCell(w, h, txt, null, AlignEnum.Default, false);
        }

        public virtual void MultiCell(double w, int h, string txt, string border, AlignEnum align, bool fill)
        {
            if (align == AlignEnum.Default)
            {
                align = AlignEnum.Justified;
            }

            // Output text with automatic or explicit line breaks
            double wmax;
            string s;
            int nb;
            string b;
            string b2 = string.Empty;
            int sep;
            int i;
            int j;
            double l;
            int ns;
            int nl;
            string c;
            double ls = 0;
            FontDefinition cw = CurrentFont;
            if (w == 0)
            {
                w = TypeSupport.ToDouble(W) - RightMargin - X;
            }
            wmax = (w - 2*CMargin)*1000/FontSize;
            s = txt.Replace("\r", "");
            nb = s.Length;
            if (nb > 0 && TypeSupport.ToString(s[nb - 1]) == "\n")
            {
                nb--;
            }
            b = 0.ToString();
            if (TypeSupport.ToBoolean(border))
            {
                if (TypeSupport.ToInt32(border) == 1)
                {
                    border = "LTRB";
                    b = "LRT";
                    b2 = "LR";
                }
                else
                {
                    b2 = "";
                    //CONVERSION_TODO: The equivalent in .NET for strpos may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
                    if (border.IndexOf("L") != Convert.ToInt32(false) ||
                        !(border.IndexOf("L").GetType() == false.GetType()))
                    {
                        b2 += "L";
                    }
                    //CONVERSION_TODO: The equivalent in .NET for strpos may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
                    if (border.IndexOf("R") != Convert.ToInt32(false) ||
                        !(border.IndexOf("R").GetType() == false.GetType()))
                    {
                        b2 += "R";
                    }
                    //CONVERSION_TODO: The equivalent in .NET for strpos may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
                    b = (border.IndexOf("T") != Convert.ToInt32(false) ||
                         !(border.IndexOf("T").GetType() == false.GetType()))
                            ? b2 + "T"
                            : b2;
                }
            }
            sep = -1;
            i = 0;
            j = 0;
            l = 0;
            ns = 0;
            nl = 1;
            while (i < nb)
            {
                // Get next character
                c = s[i].ToString();
                if (TypeSupport.ToString(c) == "\n")
                {
                    // Explicit line break
                    if (Ws > 0)
                    {
                        Ws = 0;
                        _out("0 Tw");
                    }
                    //CONVERSION_WARNING: Method 'substr' was converted to 'System.String.Substring' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/substr.htm 
                    Cell(w, h, TypeSupport.ToString(s).Substring(j, i - j), b, 2, align, fill, null);
                    i++;
                    sep = -1;
                    j = i;
                    l = 0;
                    ns = 0;
                    nl++;
                    if (TypeSupport.ToBoolean(border) && nl == 2)
                    {
                        b = b2;
                    }
                    continue;
                }
                if (TypeSupport.ToString(c) == " ")
                {
                    sep = i;
                    ls = l;
                    ns++;
                }
                l = l + TypeSupport.ToDouble(cw.Widths[c]);
                if (l > wmax)
                {
                    // Automatic line break
                    if (sep == -1)
                    {
                        if (i == j)
                        {
                            i++;
                        }
                        if (Ws > 0)
                        {
                            Ws = 0;
                            _out("0 Tw");
                        }
                        //CONVERSION_WARNING: Method 'substr' was converted to 'System.String.Substring' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/substr.htm 
                        Cell(w, h, TypeSupport.ToString(s).Substring(j, i - j), b, 2, align, fill, null);
                    }
                    else
                    {
                        if (align == AlignEnum.Justified)
                        {
                            Ws = (ns > 1) ? (wmax - ls)/1000*FontSize/(ns - 1) : 0;

                            _out(sprintf("%.3F Tw", Ws*k));
                        }
                        //CONVERSION_WARNING: Method 'substr' was converted to 'System.String.Substring' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/substr.htm 
                        Cell(w, h, TypeSupport.ToString(s).Substring(j, sep - j), b, 2, align, fill, null);
                        i = sep + 1;
                    }
                    sep = -1;
                    j = i;
                    l = 0;
                    ns = 0;
                    nl++;
                    if (TypeSupport.ToBoolean(border) && nl == 2)
                    {
                        b = b2;
                    }
                }
                else
                {
                    i++;
                }
            }
            // Last chunk
            if (Ws > 0)
            {
                Ws = 0;
                _out("0 Tw");
            }
            //CONVERSION_TODO: The equivalent in .NET for strpos may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
            if (TypeSupport.ToBoolean(border) &&
                (border.IndexOf("B") != Convert.ToInt32(false) || !(border.IndexOf("B").GetType() == false.GetType())))
            {
                b += "B";
            }
            Cell(w, h, TypeSupport.ToString(s).Substring(j, i - j), b, 2, align, fill, null);
            X = LeftMargin;
        }

        public virtual void Write(int h, string txt)
        {
            Write(h, txt, (LinkData) null);
        }

        public virtual void Write(int h, string txt, int internalLink)
        {
            LinkDataInternal link = Links[internalLink];
            Write(h, txt, internalLink);
        }

        public virtual void Write(int h, string txt, string uri)
        {
            var data = new LinkDataUri(uri);
            Write(h, txt, data);
        }

        protected virtual void Write(int h, string txt, LinkData link)
        {
            // Output text in flowing mode
            var cw = CurrentFont;
            double localWidth = W - RightMargin - X;
            double wmax = (localWidth - 2*CMargin)*1000/FontSize;
            var s = txt.Replace("\r", "");
            int nb = TypeSupport.ToString(s).Length;
            int sep = -1;
            int i = 0;
            int j = 0;
            int l = 0;
            int nl = 1;
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
                        wmax = (localWidth - 2*CMargin)*1000/FontSize;
                    }
                    nl++;
                    continue;
                }
                if (TypeSupport.ToString(c) == " ")
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
                            wmax = (localWidth - 2*CMargin)*1000/FontSize;
                            i++;
                            nl++;
                            continue;
                        }
                        if (i == j)
                        {
                            i++;
                        }
                        Cell(localWidth, h, s.Substring(j, i - j), 0.ToString(), 2, AlignEnum.Default, false, link);
                    }
                    else
                    {
                        Cell(localWidth, h, s.Substring(j, sep - j), 0.ToString(), 2, AlignEnum.Default, false, link);
                        i = sep + 1;
                    }
                    sep = -1;
                    j = i;
                    l = 0;
                    if (nl == 1)
                    {
                        X = LeftMargin;
                        localWidth = W - RightMargin - X;
                        wmax = (localWidth - 2*CMargin)*1000/FontSize;
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
            double w2 = (double) l/1000*FontSize;
            Cell(w2, h, s.Substring(j), 0.ToString(), 0, AlignEnum.Default, false, link);
            //string tail = l + " " + Convert.ToString(this.x, CultureInfo.InvariantCulture) + " " + Convert.ToString(this.ws, CultureInfo.InvariantCulture) + " " + Convert.ToString(this.RightMargin, CultureInfo.InvariantCulture);
            //this._out(tail);
        }

        public virtual void Ln()
        {
            // Line feed; default value is last cell height
            X = LeftMargin;
            Y += Lasth;
        }

        public virtual void Ln(int h)
        {
            // Line feed; 
            X = LeftMargin;
            Y += h;
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
                        imageData = _parsejpg(file);
                        break;
                    case ImageTypeEnum.Png:
                        imageData = ParsePng(file);
                        break;
                    case ImageTypeEnum.Gif:
                        imageData = _parsegif(file);
                        break;
                    case ImageTypeEnum.Default:
                    default:
                        Error("Image file has no extension and no type was specified or unsupported type (" + file + ")");
                        break;
                }
                var typeName = type.ToString().ToLower();
                //CONVERSION_ISSUE: Variable function '$mtd' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
                imageInfo = imageData;
                imageInfo.i = OrderedMap.CountElements(Images) + 1;
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
                w = TypeSupport.ToDouble(-TypeSupport.ToDouble(imageInfo.w))*72/w/k;
            }
            if (h < 0)
            {
                h = TypeSupport.ToDouble(-TypeSupport.ToDouble(imageInfo.h))*72/h/k;
            }
            if (w == 0)
            {
                w = h*TypeSupport.ToDouble(imageInfo.w)/TypeSupport.ToDouble(imageInfo.h);
            }
            if (h == 0)
            {
                h = w*TypeSupport.ToDouble(imageInfo.h)/TypeSupport.ToDouble(imageInfo.w);
            }

            // Flowing mode
            if (!y.HasValue)
            {
                if (Y + h > PageBreakTrigger && !InHeader && !InFooter && AcceptPageBreak())
                {
                    // Automatic page break
                    double x2 = X;
                    AddPage(CurOrientation, CurPageSize);
                    X = x2;
                }
                y = Y;
                Y += h;
            }

            if (!x.HasValue)
            {
                x = X;
            }
            _out(sprintf("q %.2F 0 0 %.2F %.2F %.2F cm /I%d Do Q", w*k, h*k, x*k, ((H - (y + h))*k), imageInfo.i));
            if (link != null)
            {
                Link(x.Value, y.Value, w, h, link);
            }
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
                X = TypeSupport.ToDouble(W) + x;
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
        *                              Protected methods                               *
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
                       ? new Dimensions {Width = dimensions.Heigth, Heigth = dimensions.Width}
                       : dimensions;
        }

        internal virtual void BeginPage(PageOrientation orientation, Dimensions size)
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
            if (size == null)
            {
                size = DefPageSize;
            }
            else
            {
                size = GetPageSize(size);
            }

            if (orientation != CurOrientation || size.Width != CurPageSize.Width || size.Heigth != CurPageSize.Heigth)
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

                WPt = TypeSupport.ToDouble(W)*k;
                HPt = TypeSupport.ToDouble(H)*k;
                PageBreakTrigger = TypeSupport.ToDouble(H) - PageBreakMargin;
                CurOrientation = orientation;
                CurPageSize = size;
            }
            if (orientation != DefOrientation || size.Width != DefPageSize.Width || size.Heigth != DefPageSize.Heigth)
            {
                PageSizes[Page] = new Dimensions {Width = WPt, Heigth = HPt};
            }
        }

        internal virtual void EndPage()
        {
            State = 1;
        }

        internal virtual FontDefinition LoadFont(string font)
        {
            // Load a font definition file from the font directory
            FontDefinition fontData;
            FontBuilder.Fonts.TryGetValue(font, out fontData);
            if (fontData == null || string.IsNullOrEmpty(fontData.name))
            {
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
            var w = GetStringWidth(txt) + Ws*StringSupport.SubstringCount(txt, " ");
            return sprintf("%.2F %.2F %.2F %.2F re f", x*k, (H - (y - up/(double) 1000*FontSize))*k, w*k,
                           (-ut)/(double) 1000*FontSizePt);
        }

        internal virtual ImageInfo _parsejpg(string file)
        {
            // Extract info from a JPEG file

            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Debug.Assert(path != null, "path != null");
            path = Path.Combine(path, file);
            var bi = new BitmapImage(new Uri(path));

            /*           
                        if (!PHP.TypeSupport.ToBoolean(a))
                        {
                            this.Error("Missing or incorrect image file: " + file);
                        }

                        if (PHP.TypeSupport.ToInt32(a[2]) != 2)
                        {
                            this.Error("Not a JPEG file: " + file);
                        }
                        */
            var channels = 3;
            const string colspace = "DeviceRGB";
            
            /*
            if (b.PixelFormat== System.Drawing.Imaging.PixelFormat.)
            {
                colspace = "DeviceRGB";
            }
            else if (channels == 4)
                {
                    colspace = "DeviceCMYK";
                }
                else
                {
                    colspace = "DeviceGray";
            }
             * */
            var bpc = bi.Format.BitsPerPixel;
            var data = new List<byte[]> {FileSystemSupport.ReadContentBytes(file)};
            return new ImageInfo
                {
                    w = bi.PixelWidth,
                    h = bi.PixelHeight,
                    cs = colspace,
                    bpc = bpc,
                    f = "DCTDecode",
                    data = data
                };
            //return new PHP.OrderedMap(new object[] { "w", b.Width }, new object[] { "h", a[1] }, new object[] { "cs", colspace },
            //    new object[] { "bpc", bpc }, new object[] { "f", "DCTDecode" }, new object[] { "data", data });
        }

        /// <summary>
        /// Extract info from a PNG file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        internal virtual ImageInfo ParsePng(string file)
        {
            var f = FileSystemSupport.FileOpen(file, "rb");
            if (!TypeSupport.ToBoolean(f))
            {
                Error("Can\'t open image file: " + file);
            }

            EndianBitConverter converter = new BigEndianBitConverter();
            var reader = new EndianBinaryReader(converter, f, Encoding.ASCII);
            var info = _parsepngstream(f, reader, file);
            reader.Close();
            return info;
        }

        internal virtual ImageInfo _parsepngstream(FileStream f, EndianBinaryReader reader, string file)
        {
            int n;
            int pos;
            string line;
            string signature = _readstream(reader, 8);
            if (!signature.Contains("PNG"))
            {
                Error("Not a PNG file: " + file);
            }

            /*
            if ( signature != System.Convert.ToString((char)137) + "PNG" + System.Convert.ToString((char)13) + System.Convert.ToString((char)10) + System.Convert.ToString((char)26) + System.Convert.ToString((char)10))
            {
                this.Error("Not a PNG file: " + file);
            }
            */

            // Read header chunk
            _readstream(reader, 4);
            if (_readstream(reader, 4) != "IHDR")
            {
                Error("Incorrect PNG file: " + file);
            }
            int w = reader.ReadInt32();
            int height = reader.ReadInt32();
            int bpc = _readstream(reader, 1)[0];
            if (bpc > 8)
            {
                Error("16-bit depth not supported: " + file);
            }
            int ct = _readstream(reader, 1)[0];

            string colspace = "DeviceRGB";

            if (ct == 0 || ct == 4)
            {
                colspace = "DeviceGray";
            }
            else if (ct == 2 || ct == 6)
            {
                colspace = "DeviceRGB";
            }
            else if (ct == 3)
            {
                colspace = "Indexed";
            }
            else
            {
                Error("Unknown color type: " + file);
            }
            if (_readstream(reader, 1)[0] != 0)
            {
                Error("Unknown compression method: " + file);
            }
            if (_readstream(reader, 1)[0] != 0)
            {
                Error("Unknown filter method: " + file);
            }
            if (_readstream(reader, 1)[0] != 0)
            {
                Error("Interlacing not supported: " + file);
            }
            _readstream(reader, 4);
            string dp = "/Predictor 15 /Colors " + ((colspace == "DeviceRGB") ? 3 : 1).ToString() + " /BitsPerComponent " +
                        bpc.ToString() + " /Columns " + w.ToString();

            // Scan chunks looking for palette, transparency and image data
            var pal = new byte[] {};
            var trns = new int[] {};
            byte[] data = new byte[] {};
            do
            {
                n = reader.ReadInt32();
                string type = _readstream(reader, 4);
                if (type == "PLTE")
                {
                    // Read palette
                    pal = _readStreamBytes(reader, n);
                    _readstream(reader, 4);
                }
                else if (type == "tRNS")
                {
                    // Read transparency info
                    string t = _readstream(reader, n);
                    if (ct == 0)
                    {
                        trns = new[] {Convert.ToInt32(t[1])}; // new PHP.OrderedMap((int)t.Substring(1, 1)[0]);
                    }
                    else if (ct == 2)
                    {
                        //trns = new PHP.OrderedMap((int)t.Substring(1, 1)[0], (int)t.Substring(3, 1)[0], (int)t.Substring(5, 1)[0]);
                        trns = new[] {Convert.ToInt32(t[1]), Convert.ToInt32(t[3]), Convert.ToInt32(t[5])};
                    }
                    else
                    {
                        pos = t.IndexOf(Convert.ToString((char) 0));
                        if (pos > 0)
                        {
                            trns = new[] {pos};
                        }
                    }
                    _readstream(reader, 4);
                }
                else if (type == "IDAT")
                {
                    // Read image data block
                    data = _readStreamBytes(reader, n);
                    _readstream(reader, 4);
                }

                else if (type == "IEND")
                {
                    break;
                }
                else
                {
                    _readstream(reader, n + 4);
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
            //new PHP.OrderedMap(new object[] { "w", w }, new object[] { "h", h }, new object[] { "cs", colspace }, new object[] { "bpc", bpc }, new object[] { "f", "FlateDecode" }, new object[] { "dp", dp }, new object[] { "pal", pal }, new object[] { "trns", trns });


            if (ct >= 4)
            {
                // Extract alpha channel
                var newData = GzUncompressString(data);
                var color = new StringBuilder();
                var alpha = new StringBuilder();
                int len;
                if (ct == 4)
                {
                    // Gray image
                    len = 2*w;
                    for (var i = 0; i < height; i++)
                    {
                        pos = (1 + len)*i;
                        color.Append(newData[pos]);
                        alpha.Append(newData[pos]);
                        line = newData.Substring(pos + 1, len);
                        //color.Append(new System.Text.RegularExpressions.Regex("/(.)./s").Replace(line, "$1"));
                        //alpha.Append(new System.Text.RegularExpressions.Regex("/.(.)/s").Replace(line, "$1"));
                        for (int posLinea = 0; posLinea < line.Length; posLinea += 2)
                        {
                            color.Append(line[posLinea]);
                            alpha.Append(line[posLinea + 1]);
                        }
                    }
                }
                else
                {
                    // RGB image
                    len = 4*w;
                    for (var i = 0; i < height; i++)
                    {
                        pos = (1 + len)*i;
                        color.Append(newData[pos]);
                        alpha.Append(newData[pos]);
                        line = newData.Substring(pos + 1, len);
                        for (var posLinea = 0; posLinea < line.Length; posLinea += 4)
                        {
                            color.Append(line.Substring(posLinea, 3));
                            alpha.Append(line[posLinea + 3]);
                        }
                        //color.Append(new Regex("/(.{3})./s").Replace(line, new MatchEvaluator(FPdf.CapText)));
                        //alpha.Append(new Regex("/.{3}(.)/s").Replace(line, "$1"));
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
                info.data = new List<byte[]> {data};
            }
            return info;
        }


        private static string CapText(Match m)
        {
            // Get the matched string.
            var x = m.ToString();
            // If the first char is lower case...
            if (char.IsLower(x[0]))
            {
                // Capitalize it.
                return char.ToUpper(x[0]) + x.Substring(1, x.Length - 1);
            }
            return x;
        }


        internal virtual byte[] _readStreamBytes(EndianBinaryReader br, int n)
        {
            byte[] result = br.ReadBytes(n);
            return result;
        }

        internal virtual string _readstream(EndianBinaryReader br, int n)
        {
            // Read n bytes from stream
            string res;
            string s;
            res = "";

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

        internal virtual Int32 _readint(FileStream f, BinaryReader br)
        {
            // Read a 4-byte integer from stream
            //PHP.OrderedMap a;
            //CONVERSION_ISSUE: Method 'unpack' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
            //a = unpack("Ni", this._readstream(f, 4));
            //return PHP.TypeSupport.ToInt32(a["i"]);
            Int32 a = br.ReadInt32();
            return a;
        }

        internal virtual ImageInfo _parsegif(string file)
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

        internal virtual void _newobj()
        {
            // Begin a new object
            ObjectCount++;
            Offsets[ObjectCount] = Buffer.Length;
            _out(ObjectCount.ToString() + " 0 obj");
        }

        internal virtual void _putstream(string s)
        {
            _out("stream");
            _out(s);
            _out("endstream");
        }

        internal virtual void _putstream(byte[] bytes)
        {
            _out("stream");
            _out(bytes);
            _out("endstream");
        }

        internal virtual void _putstream(List<byte[]> bytes)
        {
            _out("stream");
            _out(bytes);
            _out("endstream");
        }

        internal virtual void _out(object s)
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
                    Buffer += PrivateEncoding.GetString((byte[]) s);
                    Buffer += "\n";
                }
                else
                {
                    Buffer += TypeSupport.ToString(s) + "\n";
                }
            }
        }

        internal virtual void _putpages()
        {
            int n;
            double wPt;
            double hPt;
            int i;
            var nb = Page;
            if (!VariableSupport.Empty(AliasNbPagesRenamed))
            {
                // Replace number of pages
                for (n = 1; n <= nb; n++)
                {
                    //string page = Convert.ToString(this.pages[n]);
                    Pages[n].Replace(AliasNbPagesRenamed, nb.ToString());
                }
            }
            if (DefOrientation == PageOrientation.Portrait)
            {
                wPt = TypeSupport.ToDouble(DefPageSize.Width)*k;
                hPt = TypeSupport.ToDouble(DefPageSize.Heigth)*k;
            }
            else
            {
                wPt = TypeSupport.ToDouble(DefPageSize.Heigth)*k;
                hPt = TypeSupport.ToDouble(DefPageSize.Width)*k;
            }
            var filter = (Compress) ? "/Filter /FlateDecode " : "";
            for (n = 1; n <= nb; n++)
            {
                // Page
                _newobj();
                _out("<</Type /Page");
                _out("/Parent 1 0 R");
                if (PageSizes.ContainsKey(n))
                {
                    _out(sprintf("/MediaBox [0 0 %.2F %.2F]", PageSizes[n].Width, PageSizes[n].Heigth));
                    //this._out(sprintf("/MediaBox [0 0 %.2F %.2F]", this.PageSizes[n].Widht, this.PageSizes[n].Height));
                }
                _out("/Resources 2 0 R");
                if (Pages[n].PageLinks.Count > 0)
                {
                    // Links
                    var annots = "/Annots [";
                    foreach (PageLink pl in Pages[n].PageLinks)
                    {
                        string rect = sprintf("%.2F %.2F %.2F %.2F", pl.P0, pl.P1, pl.P0 + pl.P2, pl.P1 - pl.P3);
                        annots += "<</Type /Annot /Subtype /Link /Rect [" + rect + "] /Border [0 0 0] ";


                        if (pl.Link is LinkDataInternal)
                        {
                            var link = Links[(pl.Link as LinkDataInternal).InternalLink];
                            var l0 = link.PageIndex;
                            double h = (PageSizes.ContainsKey(l0)) ? TypeSupport.ToDouble(PageSizes[l0].Heigth) : hPt;
                            annots += sprintf("/Dest [%d 0 R /XYZ 0 %.2F null]>>", 1 + 2*link.PageIndex, h - link.Y*k);
                        }
                        else if (pl.Link is LinkDataUri)
                        {
                            annots += "/A <</S /URI /URI (" + (pl.Link as LinkDataUri).Uri + ")>>>>";
                        }
                        else
                            throw new NotImplementedException();
                    }
                    _out(annots + "]");
                }
                if (PdfVersion.CompareTo("1.3") > 0)
                {
                    _out("/Group <</Type /Group /S /Transparency /CS /DeviceRGB>>");
                }
                _out("/Contents " + (this.ObjectCount + 1).ToString() + " 0 R>>");
                _out("endobj");
                // Page content
                if (Compress)
                {
                    var p = GzCompressString(Pages[n].ToString());
                    _newobj();
                    _out("<<" + filter + "/Length " + p.Length.ToString() + ">>");
                    _putstream(p);
                    _out("endobj");
                }
                else
                {
                    var p1 = Pages[n].ToString();
                    _newobj();
                    _out("<<" + filter + "/Length " + p1.Length.ToString() + ">>");
                    _putstream(p1);
                    _out("endobj");
                }
            }
            // Pages root
            Offsets[1] = Buffer.Length;
            _out("1 0 obj");
            _out("<</Type /Pages");
            var kids = "/Kids [";
            for (i = 0; i < nb; i++)
                kids += (3 + 2*i).ToString() + " 0 R ";
            _out(kids + "]");
            _out("/Count " + nb.ToString());
            _out(sprintf("/MediaBox [0 0 %.2F %.2F]", wPt, hPt));
            _out(">>");
            _out("endobj");
        }

        internal virtual void _putfonts()
        {
            FontTypeEnum type;
            string name;
            OrderedMap cw;
            string s;
            int i;
            string mtd;
            string font;
            int nf = ObjectCount;
            foreach (object diff in Diffs.Values)
            {
                // Encodings
                _newobj();
                _out("<</Type /Encoding /BaseEncoding /WinAnsiEncoding /Differences [" + TypeSupport.ToString(diff) +
                     "]>>");
                _out("endobj");
            }

            foreach (var file in FontFiles.Keys)
            {
                FontDefinition info = Fonts[file];
                // Font file embedding
                _newobj();
                info.n = ObjectCount;
                //file_get_contents' returns a string 
                font = FileSystemSupport.ReadContents(Fontpath + file);
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
                _out("<</Length " + font.Length.ToString());
                if (compressed)
                {
                    _out("/Filter /FlateDecode");
                }
                _out("/Length1 " + TypeSupport.ToString(info.length1));
                if (info.length2 != 0)
                {
                    _out("/Length2 " + TypeSupport.ToString(info.length2) + " /Length3 0");
                }
                _out(">>");
                _putstream(font);
                _out("endobj");
            }

            foreach (string k in Fonts.Keys)
            {
                FontDefinition font1 = Fonts[k];
                // Font objects
                font1.n = ObjectCount + 1;
                type = font1.type;
                name = font1.name;
                if (type == FontTypeEnum.Core)
                {
                    // Core font
                    _newobj();
                    _out("<</Type /Font");
                    _out("/BaseFont /" + TypeSupport.ToString(name));
                    _out("/Subtype /Type1");
                    if (TypeSupport.ToString(name) != "Symbol" && TypeSupport.ToString(name) != "ZapfDingbats")
                    {
                        _out("/Encoding /WinAnsiEncoding");
                    }
                    _out(">>");
                    _out("endobj");
                }
                else if (type == FontTypeEnum.Type1 || type == FontTypeEnum.TrueType)
                {
                    // Additional Type1 or TrueType/OpenType font
                    _newobj();
                    _out("<</Type /Font");
                    _out("/BaseFont /" + TypeSupport.ToString(name));
                    _out("/Subtype /" + TypeSupport.ToString(type));
                    _out("/FirstChar 32 /LastChar 255");
                    _out("/Widths " + (ObjectCount + 1).ToString() + " 0 R");
                    _out("/FontDescriptor " + (ObjectCount + 2).ToString() + " 0 R");
                    if (font1.diffn.HasValue)
                    {
                        _out("/Encoding " + (nf + font1.diffn).ToString() + " 0 R");
                    }
                    else
                    {
                        _out("/Encoding /WinAnsiEncoding");
                    }
                    _out(">>");
                    _out("endobj");
                    // Widths
                    _newobj();
                    cw = TypeSupport.ToArray(font1.cw);
                    s = "[";
                    for (i = 32; i <= 255; i++)
                        s += TypeSupport.ToString(cw[Convert.ToString((char) i)]) + " ";
                    _out(s + "]");
                    _out("endobj");
                    // Descriptor
                    _newobj();
                    s = "<</Type /FontDescriptor /FontName /" + TypeSupport.ToString(name);
                    foreach (string k1 in font1.desc.Keys)
                    {
                        object v = font1.desc[k1];
                        s += " /" + k1 + " " + TypeSupport.ToString(v);
                    }

                    if (!string.IsNullOrEmpty(font1.file))
                    {
                        s += " /FontFile" + (type == FontTypeEnum.Type1 ? "" : "2") + " " +
                             TypeSupport.ToString(Fonts[font1.file].n) + " 0 R";
                    }
                    _out(s + ">>");
                    _out("endobj");
                }
                else
                {
                    Error("Unsupported font type: " + TypeSupport.ToString(type));
                }
            }
        }

        internal virtual void _putimages()
        {
            foreach (var file in Images)
            {
                _putimage(file.Value);
                file.Value.data = null; //unset, probably not needed
                file.Value.smask = null; //unset, probably not needed
            }
        }

        internal virtual void _putimage(ImageInfo info)
        {
            string trns;
            string dp;
            ImageInfo smask;
            string filter;
            byte[] pal;
            _newobj();
            info.n = ObjectCount;
            _out("<</Type /XObject");
            _out("/Subtype /Image");
            _out("/Width " + info.w.ToString());
            _out("/Height " + info.h.ToString());
            if (info.cs == "Indexed")
            {
                _out("/ColorSpace [/Indexed /DeviceRGB " + (info.pal.Length/3 - 1).ToString() + " " + (ObjectCount + 1).ToString() +
                     " 0 R]");
            }
            else
            {
                _out("/ColorSpace /" + TypeSupport.ToString(info.cs));
                if (TypeSupport.ToString(info.cs) == "DeviceCMYK")
                {
                    _out("/Decode [1 0 1 0 1 0 1 0]");
                }
            }
            _out("/BitsPerComponent " + TypeSupport.ToString(info.bpc));
            if (info.f != null)
            {
                _out("/Filter /" + TypeSupport.ToString(info.f));
            }
            if (info.dp != null)
            {
                _out("/DecodeParms <<" + TypeSupport.ToString(info.dp) + ">>");
            }
            if (info.trns != null && info.trns.Count() > 0)
            {
                trns = "";
                foreach (int trn in info.trns)
                {
                    trns += trn + " " + trn + " ";
                }
                _out("/Mask [" + trns + "]");
            }
            if (info.smask != null)
            {
                _out("/SMask " + (ObjectCount + 1).ToString() + " 0 R");
            }

            int largo = info.data.Select(x => x.Length).Sum();

            _out("/Length " + largo.ToString() + ">>");
            _putstream(info.data);
            _out("endobj");
            // Soft mask
            //CONVERSION_WARNING: Method 'isset' was converted to '!=' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/isset.htm 
            if (info.smask != null)
            {
                dp = "/Predictor 15 /Colors 1 /BitsPerComponent 8 /Columns " + TypeSupport.ToString(info.w);
                smask = new ImageInfo
                    {
                        w = info.w,
                        h = info.h,
                        cs = "DeviceGray",
                        bpc = 8,
                        f = info.f,
                        dp = dp,
                        data = new List<byte[]> {info.smask}
                    };
                /*
                smask = new PHP.OrderedMap(
                        new object[] { "w", info.w }, 
                        new object[] { "h", info.h },
                        new object[] { "cs", "DeviceGray" }, 
                        new object[] { "bpc", 8 },
                        new object[] { "f", info.f }, 
                        new object[] { "dp", dp },
                        new object[] { "data", info.smask });
                */
                _putimage(smask);
            }
            // Palette
            if (TypeSupport.ToString(info.cs) == "Indexed")
            {
                filter = (Compress) ? "/Filter /FlateDecode " : "";
                if (Compress)
                {
                    pal = gzcompress(info.pal);
                }
                else
                {
                    pal = info.pal;
                }
                _newobj();
                _out("<<" + filter + "/Length " + pal.Length.ToString() + ">>");
                _putstream(pal);
                _out("endobj");
            }
        }

        internal virtual void PutXObjectDictionary()
        {
            foreach (ImageInfo image in Images.Values)
            {
                _out("/I" + TypeSupport.ToString(image.i) + " " + TypeSupport.ToString(image.n) + " 0 R");
            }
        }

        internal virtual void PutResourceDictionary()
        {
            _out("/ProcSet [/PDF /Text /ImageB /ImageC /ImageI]");
            _out("/Font <<");
            foreach (FontDefinition font in Fonts.Values)
            {
                _out("/F" + TypeSupport.ToString(font.i) + " " + TypeSupport.ToString(font.n) + " 0 R");
            }
            _out(">>");
            _out("/XObject <<");
            PutXObjectDictionary();
            _out(">>");
        }

        internal virtual void PutResources()
        {
            _putfonts();
            _putimages();
            // Resource dictionary
            Offsets[2] = Buffer.Length;
            _out("2 0 obj");
            _out("<<");
            PutResourceDictionary();
            _out(">>");
            _out("endobj");
        }

        //CONVERSION_ISSUE: Operator '@' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
        internal virtual void PutInfo()
        {
            _out("/Producer " + TextString("FPDF " + FpdfVersion));
            if (!VariableSupport.Empty(Title))
            {
                _out("/Title " + TextString(Title));
            }
            if (!VariableSupport.Empty(Subject))
            {
                _out("/Subject " + TextString(Subject));
            }
            if (!VariableSupport.Empty(Author))
            {
                _out("/Author " + TextString(Author));
            }
            if (!VariableSupport.Empty(Keywords))
            {
                _out("/Keywords " + TextString(Keywords));
            }
            if (!VariableSupport.Empty(Creator))
            {
                _out("/Creator " + TextString(Creator));
            }
            //CONVERSION_WARNING: Method 'date' was converted to 'System.DateTime.ToString' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/date.htm 
            //CONVERSION_ISSUE: Operator '@' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
            _out("/CreationDate " + TextString("D:" + DateTime.Now.ToString("YmdHis")));
        }

        internal virtual void PutCatalog()
        {
            _out("/Type /Catalog");
            _out("/Pages 1 0 R");
            if (ZoomMode == "fullpage")
            {
                _out("/OpenAction [3 0 R /Fit]");
            }
            else if (ZoomMode == "fullwidth")
            {
                _out("/OpenAction [3 0 R /FitH null]");
            }
            else if (ZoomMode == "real")
            {
                _out("/OpenAction [3 0 R /XYZ null null 1]");
            }
            else if (!(string.IsNullOrEmpty(ZoomMode)))
            {
                _out("/OpenAction [3 0 R /XYZ null null " + sprintf("%.2F", TypeSupport.ToDouble(ZoomMode)/100) + "]");
            }
            if (LayoutMode == "single")
            {
                _out("/PageLayout /SinglePage");
            }
            else if (LayoutMode == "continuous")
            {
                _out("/PageLayout /OneColumn");
            }
            else if (LayoutMode == "two")
            {
                _out("/PageLayout /TwoColumnLeft");
            }
        }

        internal virtual void _putheader()
        {
            _out("%PDF-" + PdfVersion);
        }

        internal virtual void _puttrailer()
        {
            _out("/Size " + (ObjectCount + 1).ToString());
            _out("/Root " + ObjectCount.ToString() + " 0 R");
            _out("/Info " + (ObjectCount - 1).ToString() + " 0 R");
        }

        internal virtual void _enddoc()
        {
            object o;
            int i;
            _putheader();
            _putpages();
            PutResources();
            // Info
            _newobj();
            _out("<<");
            PutInfo();
            _out(">>");
            _out("endobj");
            // Catalog
            _newobj();
            _out("<<");
            PutCatalog();
            _out(">>");
            _out("endobj");
            // Cross-ref
            o = Buffer.Length;
            _out("xref");
            _out("0 " + (ObjectCount + 1).ToString());
            _out("0000000000 65535 f ");
            for (i = 1; i <= ObjectCount; i++)
            {
                _out(sprintf("%010d 00000 n ", Offsets[i]));
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
            _out("trailer");
            _out("<<");
            _puttrailer();
            _out(">>");
            _out("startxref");
            _out(o);
            _out("%%EOF");
            State = 3;
        }

        public static string sprintf(string Format, params object[] Parameters)
        {
            string result = SprintfTools.sprintf(Format, Parameters);
            return result;
        }

        public byte[] gzcompress(byte[] value)
        {
            var outstream = new MemoryStream();
            var g = new ZlibStream(outstream, CompressionMode.Compress);
            g.Write(value, 0, value.Length);
            g.Close();
            byte[] result = outstream.ToArray();
            return result;
        }

        public byte[] gzuncompress(byte[] value)
        {
            var instream = new MemoryStream(value, false);
            var g = new ZlibStream(instream, CompressionMode.Decompress);
            var reader = new BinaryReader(g);
            byte[] bytes = reader.ReadBytes(Int16.MaxValue*100);
            g.Close();
            return bytes;
        }

        public string GzUncompressString(byte[] value)
        {
            byte[] uncompressedArray = gzuncompress(value);
            string result = PrivateEncoding.GetString(uncompressedArray);
            return result;
        }

        public byte[] GzCompressString(string value)
        {
            byte[] bytes = PrivateEncoding.GetBytes(value);
            byte[] result = gzcompress(bytes);
            return result;
        }
    }
}