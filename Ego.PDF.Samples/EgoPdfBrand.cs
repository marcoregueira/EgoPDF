using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace Ego.PDF.Samples;

/// <summary>
/// Shared brand identity for every "egoPdf showcase" sample: the colour
/// palette extracted from the logo, plus the two-tone wordmark that
/// appears in every header. Centralising it here means a new sample no
/// longer has to copy six colour declarations and the SetFont / SetXY /
/// SetTextColor / Cell sequence to print "egoPdf" in the corner.
///
/// The wordmark itself uses <b>Poppins Extra Light</b> -- the thin
/// weight that matches the project logo. <see cref="LoadPoppins"/>
/// embeds the TTF; <see cref="DrawWordmark"/> emits the two-coloured
/// "ego" + "Pdf" text.
/// </summary>
public static class EgoPdfBrand
{
    // ---- Palette ----------------------------------------------------------

    /// <summary>Near-black used for the dark header band and body text.</summary>
    public static readonly Color Dark     = new Color(26, 29, 38);
    /// <summary>Coral accent that highlights the "Pdf" half of the wordmark.</summary>
    public static readonly Color Accent   = new Color(204, 105, 95);
    /// <summary>Light grey text for subtitles printed on the dark band.</summary>
    public static readonly Color SubText  = new Color(180, 184, 196);
    /// <summary>Muted grey body text used for secondary labels on white.</summary>
    public static readonly Color Muted    = new Color(110, 115, 130);
    /// <summary>Subtle divider line colour for hairlines on white backgrounds.</summary>
    public static readonly Color HairLine = new Color(220, 220, 224);

    // ---- Font -------------------------------------------------------------

    /// <summary>FPdf family name under which the Extra Light Poppins TTF is registered.</summary>
    public const string PoppinsFamily = "Poppins";

    /// <summary>
    /// Embed the Poppins Extra Light TTF from the standard
    /// <c>Fonts/Poppins/Poppins-ExtraLight.ttf</c> location and register
    /// it with FPdf under the <see cref="PoppinsFamily"/> family.
    /// <paramref name="baseDir"/> defaults to the executing assembly
    /// directory, which is where the sample projects copy the Fonts/
    /// folder at build time.
    /// </summary>
    public static void LoadPoppins(FPdf pdf, string baseDir = null)
    {
        baseDir ??= Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        var path = Path.Combine(baseDir, "Fonts", "Poppins", "Poppins-ExtraLight.ttf");
        pdf.LoadFont(PoppinsFamily, path);
        pdf.AddFont(PoppinsFamily, "");
    }

    // ---- Wordmark ---------------------------------------------------------

    /// <summary>
    /// Stamp the two-tone "ego" + "Pdf" wordmark at (<paramref name="x"/>,
    /// <paramref name="y"/>) using Poppins Extra Light at
    /// <paramref name="sizePt"/> points. The two halves are emitted as
    /// separate <see cref="FPdf.Cell"/> calls with widths derived from
    /// <see cref="FPdf.GetStringWidth"/> so they butt up against each
    /// other with no manual offset.
    /// </summary>
    /// <param name="cellHeight">
    /// Vertical extent of the underlying invisible cell, in mm. Doesn't
    /// affect rendering width but does influence the cursor's Y at the
    /// end of the call, which matters when the caller advances inline.
    /// Match the value the surrounding header uses for the title row.
    /// </param>
    /// <param name="egoColor">Colour for the "ego" half (e.g. white on a dark band, <see cref="Dark"/> on white).</param>
    /// <param name="pdfColor">Colour for the "Pdf" half (typically <see cref="Accent"/>).</param>
    public static void DrawWordmark(FPdf pdf, double x, double y, double sizePt,
        Color egoColor, Color pdfColor, double cellHeight)
    {
        pdf.SetFont(PoppinsFamily, "", sizePt);
        pdf.SetXY(x, y);
        pdf.SetTextColor(egoColor);
        pdf.Cell(pdf.GetStringWidth("ego"), cellHeight, "ego");
        pdf.SetTextColor(pdfColor);
        pdf.Cell(pdf.GetStringWidth("Pdf"), cellHeight, "Pdf");
    }
}
