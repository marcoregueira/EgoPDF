using Microsoft.Xna.Framework;

namespace Ego.PDF.Data
{
    /// <summary>
    /// Declarative styling for <c>FPdf.Panel(bounds, title, style, body)</c>.
    /// Every property has a sensible default so an agent can pass
    /// <c>new PanelStyle()</c> and get a readable framed box, then
    /// override just what differs from the brand template.
    ///
    /// <code>
    /// var brand = new PanelStyle
    /// {
    ///     FillColor   = new Color(248, 248, 250),
    ///     BorderColor = new Color(220, 220, 224),
    ///     TitleColor  = new Color(204, 105,  95),
    /// };
    /// pdf.Panel(slot, "MEDIDAS DC", brand, content => { ... });
    /// </code>
    ///
    /// A <c>null</c> colour means "don't paint that part": no fill, no
    /// border, or inherit the current text colour for the title.
    /// </summary>
    public sealed class PanelStyle
    {
        /// <summary>Background colour of the panel box. <c>null</c> means transparent.</summary>
        public Color? FillColor { get; set; }

        /// <summary>Border colour of the panel box. <c>null</c> means no border.</summary>
        public Color? BorderColor { get; set; }

        /// <summary>Title text colour. <c>null</c> inherits the current TextColor.</summary>
        public Color? TitleColor { get; set; }

        /// <summary>Title font family. <c>null</c> inherits the current font.</summary>
        public string TitleFontFamily { get; set; } = "Helvetica";

        /// <summary>Title font style ("", "B", "I", "BI"). Defaults to bold.</summary>
        public string TitleFontStyle { get; set; } = "B";

        /// <summary>Title font size in points. Defaults to 8 pt.</summary>
        public double TitleFontSize { get; set; } = 8;

        /// <summary>Border stroke width in mm.</summary>
        public double LineWidth { get; set; } = 0.2;

        /// <summary>Inner padding between the frame and the content rect, in mm.</summary>
        public double Padding { get; set; } = 3;

        /// <summary>Height of the title band, in mm. Ignored when no title is set.</summary>
        public double TitleHeight { get; set; } = 5;

        /// <summary>
        /// When <c>true</c>, a hairline is drawn under the title in the
        /// border colour. Off by default to keep simple panels clean.
        /// </summary>
        public bool TitleHairline { get; set; } = true;
    }
}
