using System;

namespace Ego.PDF.Data
{
    /// <summary>
    /// Axis-aligned rectangle in user units (mm by default). Used by
    /// the layout primitives (<c>FPdf.Row</c>, <c>FPdf.Stack</c>,
    /// <c>FPdf.Panel</c>) to avoid the (x, y, w, h) arithmetic that
    /// makes manual FPDF layout brittle.
    ///
    /// Coordinates follow the same convention as the rest of the
    /// library: origin at the page's top-left corner, X grows to the
    /// right, Y grows downwards.
    /// </summary>
    public readonly record struct Rect(double X, double Y, double W, double H)
    {
        public double Right => X + W;
        public double Bottom => Y + H;
        public double CenterX => X + W / 2.0;
        public double CenterY => Y + H / 2.0;

        /// <summary>Shrinks the rect by the same margin on all four sides.</summary>
        public Rect Inset(double margin) => Inset(margin, margin, margin, margin);

        /// <summary>Shrinks by separate horizontal and vertical margins.</summary>
        public Rect Inset(double horizontal, double vertical)
            => Inset(horizontal, vertical, horizontal, vertical);

        /// <summary>Shrinks by independent left/top/right/bottom margins.</summary>
        public Rect Inset(double left, double top, double right, double bottom)
            => new(X + left, Y + top, Math.Max(0, W - left - right), Math.Max(0, H - top - bottom));

        /// <summary>Returns the rect with its top edge moved down by <paramref name="dy"/> mm,
        /// keeping the bottom edge in place (so height shrinks by the same amount).</summary>
        public Rect WithTopOffset(double dy) => new(X, Y + dy, W, Math.Max(0, H - dy));

        /// <summary>Returns a new rect with the given height; the top edge is unchanged.</summary>
        public Rect WithHeight(double newHeight) => new(X, Y, W, newHeight);

        /// <summary>Returns a new rect with the given width; the left edge is unchanged.</summary>
        public Rect WithWidth(double newWidth) => new(X, Y, newWidth, H);
    }
}
