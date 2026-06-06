using System.Globalization;

namespace Ego.PDF;

public class FontScale
{
    // Properties (not fields) so each access returns a fresh instance.
    // FieldDefinition mutates pdf.FontScale.ScaleX directly when a ZPL
    // field needs scaled text; before this change, that mutation leaked
    // into FontScale.Default and polluted every subsequent FPdf in the
    // process. Symptom was Sample6 / Sample8 baselines drifting between
    // "Tm <scale> ... Tj" and "Td <pos> ... Tj" depending on whether a
    // ZPL sample had run first in the same test process.
    public static FontScale Default      => new(1, 1);
    public static FontScale DoubleHeight => new(1, 2);
    public static FontScale DoubleWidth  => new(2, 1);

    public double ScaleX { get; set; } = 1;
    public double ScaleY { get; set; } = 1;


    public FontScale(double scaleX, double scaleY)
    {
        ScaleX = scaleX;
        ScaleY = scaleY;
    }

    public bool HasScale()
    {
        return ScaleX != 1 || ScaleY != 1;
    }

    public override string ToString()
    {
        if (ScaleX != 1 || ScaleY != 1)
        {
            var scale =
                ScaleX.ToString(CultureInfo.InvariantCulture)
                + " 0 0 "
                + ScaleY.ToString(CultureInfo.InvariantCulture)
                + " ";
            return scale;
        }
        return "";
    }
}