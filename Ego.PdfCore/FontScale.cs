using System.Globalization;

namespace Ego.PDF;

public class FontScale
{
    public static FontScale Default = new(1, 1);
    public static FontScale DoubleHeight = new(1, 2);
    public static FontScale DoubleWidth = new(2, 1);

    public double ScaleX { get; private set; } = 1;
    public double ScaleY { get; private set; } = 1;



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