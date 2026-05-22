using System.Globalization;

namespace Ego.Pdf.Zpl;

/// <summary>
/// Small helpers for the ZPL parser: safe positional access on the
/// comma-split argument string and a placeholder dot → mm conversion.
/// </summary>
internal static class ZplToolExtensions
{
    public static int ToInt(this string[] value, int position, int @default)
    {
        if (value.Length > position && int.TryParse(value[ position ], out var result)) return result;
        return @default;
    }

    public static string ToString(this string[] value, int position, string @default)
    {
        if (value.Length > position) return value[ position ];
        return @default;
    }

    // The "ToMilimeters" helpers exist for historical reasons — the
    // conversion is actually performed in FPdf via SetUnitConverionFactor,
    // so here we just return the raw dot value. Callers parse a numeric
    // value out of the ZPL argument and pass it straight through.

    public static double ToMilimeters(this int value, double dpi) => value;

    public static double ToMilimeters(this double value, double dpi) => value;

    public static double ToMilimeters(this string[] value, int position, double @default, double dpi)
    {
        if (value.Length > position &&
            double.TryParse(value[ position ], NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed.ToMilimeters(dpi);
        return @default.ToMilimeters(dpi);
    }
}
