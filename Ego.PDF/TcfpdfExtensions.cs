using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Ego.PDF.Data;
using Ego.PDF.PHP;

namespace Ego.PDF
{
    public static class TcfpdfExtensions
    {
        public static string GetAnnotOptFromJsProp(this FPdf document, JsOptions prop, string colors, bool rtl = false)
        {
            var opt = new JsOptions2();

            switch (prop.Alignment)
            {
                case AlignEnum.Left:
                    opt.q = 0;
                    break;
                case AlignEnum.Right:
                    opt.q = 2;
                    break;
                case AlignEnum.Center:
                    opt.q = 1;
                    break;
                case AlignEnum.Justified:
                    throw new NotSupportedException("Justified is not supported");
                    break;
                case AlignEnum.Default:
                default:
                    opt.q = rtl ? 0 : 2;
                    break;
            }

            double lineWidth;
            if (prop.LineWidth.HasValue)
            {
                lineWidth = prop.LineWidth.Value;
            }
            else
            {
                lineWidth = 1;
            }


            switch (prop.BorderStyle)
            {
                case BorderStyle.None:
                    break;
                case BorderStyle.Dashed:
                    break;
                case BorderStyle.Beveled:
                    break;
                case BorderStyle.Inset:
                    break;
                case BorderStyle.Underline:
                    break;
                case BorderStyle.Solid:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            opt.Border = new double {};

            return string.Empty;
        }
    }

    public class JsOptions2
    {
        public int q;
        public double Border { get; set; }
    }
}
