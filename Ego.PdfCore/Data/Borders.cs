using System;

namespace Ego.PDF.Data
{
    /// <summary>
    /// Flags-based replacement for the legacy <c>string Border</c> property
    /// on <see cref="TableCell"/> and <see cref="TableRow"/>. Lets callers
    /// say <c>Borders.All &amp; ~Borders.Bottom</c> instead of building the
    /// "BTLR"-style string by hand, and lets agents express intent without
    /// guessing whether <c>"1"</c> meant "all" or "border width 1".
    /// </summary>
    [Flags]
    public enum Borders
    {
        None   = 0,
        Top    = 1 << 0,
        Bottom = 1 << 1,
        Left   = 1 << 2,
        Right  = 1 << 3,
        All    = Top | Bottom | Left | Right,
    }

    public static class BordersExtensions
    {
        /// <summary>
        /// Translate the flags into the FPDF "BTLR" string the underlying
        /// <c>Cell</c> / <c>MultiCell</c> primitives expect. Used by the
        /// row renderer so the new fluent API never has to leak FPDF's
        /// border syntax to its callers.
        /// </summary>
        public static string ToFpdfBorderString(this Borders b)
        {
            if (b == Borders.None) return "0";
            if (b == Borders.All)  return "1";

            var s = "";
            if (b.HasFlag(Borders.Bottom)) s += "B";
            if (b.HasFlag(Borders.Top))    s += "T";
            if (b.HasFlag(Borders.Left))   s += "L";
            if (b.HasFlag(Borders.Right))  s += "R";
            return s;
        }
    }
}
