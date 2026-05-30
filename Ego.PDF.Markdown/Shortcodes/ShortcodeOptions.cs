using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;

namespace Ego.PDF.Markdown.Shortcodes;

/// <summary>
/// Tiny helpers to read typed values from a shortcode's
/// <c>Options</c> dictionary without each handler writing its own
/// boilerplate. All helpers are tolerant: a missing key, an empty
/// string or a malformed value falls back to the supplied default.
/// </summary>
public static class ShortcodeOptions
{
    public static string GetString(IDictionary<string, string> options, string key, string fallback = "")
    {
        if (options.TryGetValue(key, out var raw) && !string.IsNullOrEmpty(raw))
        {
            return raw;
        }
        return fallback;
    }

    public static double GetDouble(IDictionary<string, string> options, string key, double fallback)
    {
        if (options.TryGetValue(key, out var raw) &&
            double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
        {
            return v;
        }
        return fallback;
    }

    public static int GetInt(IDictionary<string, string> options, string key, int fallback)
    {
        if (options.TryGetValue(key, out var raw) &&
            int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
        {
            return v;
        }
        return fallback;
    }

    public static bool GetBool(IDictionary<string, string> options, string key, bool fallback)
    {
        if (!options.TryGetValue(key, out var raw)) return fallback;
        if (string.IsNullOrEmpty(raw)) return true; // bare flag, e.g. [[box outlined]]
        return raw.Equals("true", System.StringComparison.OrdinalIgnoreCase)
            || raw == "1"
            || raw.Equals("yes", System.StringComparison.OrdinalIgnoreCase)
            || raw.Equals("on", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses a <c>#rrggbb</c> hex colour. Returns <paramref name="fallback"/>
    /// for missing, empty or malformed input.
    /// </summary>
    public static Color GetColor(IDictionary<string, string> options, string key, Color fallback)
    {
        if (!options.TryGetValue(key, out var raw) || string.IsNullOrEmpty(raw)) return fallback;
        var hex = raw.TrimStart('#');
        if (hex.Length != 6) return fallback;
        if (int.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) &&
            int.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) &&
            int.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
        {
            return new Color(r, g, b);
        }
        return fallback;
    }
}
