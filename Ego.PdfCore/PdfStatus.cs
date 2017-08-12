using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ego.PDF.Font;

namespace Ego.PDF
{
    public class PdfStatus
    {
        public string DrawColor { get; set; }
        public string FillColor { get; set; }
        public string TextColor { get; set; }
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
    }

    public class FontStatus
    {
        public bool? Underline { get; set; }
        public FontDefinition CurrentFont { get; set; }
        public string FontFamily { get; set; }
        public string FontStyle { get; set; }
        public double FontSizePt { get; set; }
        public double FontSize { get; set; }

    }
}
