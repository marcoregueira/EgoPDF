using System.Globalization;
using System.Runtime.CompilerServices;

namespace Ego.Pdf.Zpl;

internal static class ZplToolExtensions
{
    public static int ToInt(this string[] value, int position, int @default)
    {
        if (value.Length > position && int.TryParse(value[position], out var result)) return result;
        return @default;
    }

    public static string ToString(this string[] value, int position, string @default)
    {
        if (value.Length > position) return value[position];
        return @default;
    }


    public static double ToMilimeters(this int value, double dpi)
    {
        return value;
        return value / dpi * 25.4;
    }

    public static double ToMilimeters(this double value, double dpi)
    {
        return value;
        //return value / dpi * 25.4;
    }

    public static double ToMilimeters(this string[] value, int position, double defaultPoints, double dpi)
    {
        // a4 -> 8.27 x 11.69 inches, 210 x 297 mm, 595 x 842 points (1/72 inch) , 793 x 1122 pixels (96 dpi), 2380 x 3366 pixels (300 dpi)
        // 1in= 2.54cm, 1cm = 0.393701in, 1in = 25.4mm
        // 1cm = 37.795276px
        var points = defaultPoints;
        if (value.Length > position && double.TryParse(value[position], NumberStyles.Any, CultureInfo.InvariantCulture, out var pointValue))
            points = pointValue;
        return points.ToMilimeters(dpi);
    }
}