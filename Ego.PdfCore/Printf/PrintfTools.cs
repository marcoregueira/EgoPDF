using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ego.PDF.Printf
{
    public static class SprintfTools
    {
        public static bool IsNumericType(object o)
        {
            return o is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
        }

        public static bool IsPositive(object value, bool zeroIsPositive)
        {
            return value switch
            {
                sbyte v => zeroIsPositive ? v >= 0 : v > 0,
                short v => zeroIsPositive ? v >= 0 : v > 0,
                int v => zeroIsPositive ? v >= 0 : v > 0,
                long v => zeroIsPositive ? v >= 0 : v > 0,
                float v => zeroIsPositive ? v >= 0 : v > 0,
                double v => zeroIsPositive ? v >= 0 : v > 0,
                decimal v => zeroIsPositive ? v >= 0 : v > 0,
                byte => true,
                ushort => true,
                uint => true,
                ulong => true,
                char v => zeroIsPositive ? true : v != '\0',
                _ => false,
            };
        }

        public static object ToUnsigned(object value)
        {
            return value switch
            {
                sbyte v => (byte) v,
                short v => (ushort) v,
                int v => (uint) v,
                long v => (ulong) v,
                _ => value,
            };
        }

        public static object ToInteger(object value, bool round)
        {
            return value switch
            {
                float v => round ? (int) Math.Round(v) : (int) v,
                double v => round ? (long) Math.Round(v) : (long) v,
                decimal v => round ? Math.Round(v) : v,
                _ => value,
            };
        }

        public static long UnboxToLong(object value, bool round)
        {
            return value switch
            {
                sbyte v => (long) v,
                short v => (long) v,
                int v => (long) v,
                long v => v,
                byte v => (long) v,
                ushort v => (long) v,
                uint v => (long) v,
                ulong v => (long) v,
                float v => round ? (long) Math.Round(v) : (long) v,
                double v => round ? (long) Math.Round(v) : (long) v,
                decimal v => round ? (long) Math.Round(v) : (long) v,
                _ => 0,
            };
        }

        public static string ReplaceMetaChars(string input)
        {
            return Regex.Replace(input, @"(\\)(\d{3}|[^\d])?", ReplaceMetaCharsMatch);
        }

        private static string ReplaceMetaCharsMatch(Match m)
        {
            if (m.Groups[ 2 ].Length == 3)
                return Convert.ToChar(Convert.ToByte(m.Groups[ 2 ].Value, 8)).ToString();

            return m.Groups[ 2 ].Value switch
            {
                "0" => "\0",
                "a" => "\a",
                "b" => "\b",
                "f" => "\f",
                "v" => "\v",
                "r" => "\r",
                "n" => "\n",
                "t" => "\t",
                _ => m.Groups[ 2 ].Value,
            };
        }

        public static void printf(string format, params object[] parameters)
        {
            Console.Write(sprintf(format, parameters));
        }

        public static void fprintf(TextWriter destination, string format, params object[] parameters)
        {
            destination.Write(sprintf(format, parameters));
        }

        public static string sprintf(string format, params object[] parameters)
        {
            StringBuilder f = new StringBuilder(format);
            Regex r = new Regex(@"\%(\d*\$)?([\'\#\-\+ ]*)(\d*)(?:\.(\d+))?([hl])?([dioxXucsfFeEgGpn%])");

            Match m = r.Match(f.ToString());
            int defaultParamIx = 0;

            while (m.Success)
            {
                int paramIx = defaultParamIx;
                if (!string.IsNullOrEmpty(m.Groups[ 1 ].Value))
                {
                    string val = m.Groups[ 1 ].Value.Substring(0, m.Groups[ 1 ].Value.Length - 1);
                    paramIx = Convert.ToInt32(val) - 1;
                }

                bool flagLeft2Right = m.Groups[ 2 ].Value.Contains('-');
                bool flagAlternate = m.Groups[ 2 ].Value.Contains('#');
                bool flagPositiveSign = m.Groups[ 2 ].Value.Contains('+');
                bool flagPositiveSpace = m.Groups[ 2 ].Value.Contains(' ');
                bool flagGroupThousands = m.Groups[ 2 ].Value.Contains('\'');

                int fieldLength = int.MinValue;
                if (!string.IsNullOrEmpty(m.Groups[ 3 ].Value))
                {
                    fieldLength = Convert.ToInt32(m.Groups[ 3 ].Value);
                }

                int fieldPrecision = int.MinValue;
                if (!string.IsNullOrEmpty(m.Groups[ 4 ].Value))
                {
                    fieldPrecision = Convert.ToInt32(m.Groups[ 4 ].Value);
                }

                char shortLongIndicator = string.IsNullOrEmpty(m.Groups[ 5 ].Value) ? '\0' : m.Groups[ 5 ].Value[ 0 ];
                char formatSpecifier = string.IsNullOrEmpty(m.Groups[ 6 ].Value) ? '\0' : m.Groups[ 6 ].Value[ 0 ];

                if (fieldPrecision == int.MinValue && formatSpecifier != 's' && formatSpecifier != 'c' && char.ToUpper(formatSpecifier) != 'X' && formatSpecifier != 'o')
                {
                    fieldPrecision = 6;
                }

                object o = paramIx >= parameters.Length ? null : parameters[ paramIx ];
                if (shortLongIndicator == 'h')
                {
                    o = o switch
                    {
                        int v => (short) v,
                        long v => (short) v,
                        uint v => (ushort) v,
                        ulong v => (ushort) v,
                        _ => o
                    };
                }
                else if (shortLongIndicator == 'l')
                {
                    o = o switch
                    {
                        short v => (long) v,
                        int v => (long) v,
                        ushort v => (ulong) v,
                        uint v => (ulong) v,
                        _ => o
                    };
                }

                string w = formatSpecifier switch
                {
                    '%' => "%",
                    'd' or 'i' => FormatNumber((flagGroupThousands ? "n" : "d"), flagAlternate, fieldLength, int.MinValue, flagLeft2Right, flagPositiveSign, flagPositiveSpace, ' ', o),
                    'o' => FormatOct("o", flagAlternate, fieldLength, int.MinValue, flagLeft2Right, ' ', o),
                    'x' => FormatHex("x", flagAlternate, fieldLength, fieldPrecision, flagLeft2Right, ' ', o),
                    'X' => FormatHex("X", flagAlternate, fieldLength, fieldPrecision, flagLeft2Right, ' ', o),
                    'u' => FormatNumber((flagGroupThousands ? "n" : "d"), flagAlternate, fieldLength, int.MinValue, flagLeft2Right, false, false, ' ', ToUnsigned(o)),
                    'c' => Convert.ToChar(o).ToString(),
                    's' => string.Format("{0" + (fieldLength != int.MinValue ? "," + (flagLeft2Right ? "-" : string.Empty) + fieldLength.ToString() : string.Empty) + ":s}", o),
                    'f' or 'F' => FormatNumber((flagGroupThousands ? "n" : "f"), flagAlternate, fieldLength, fieldPrecision, flagLeft2Right, flagPositiveSign, flagPositiveSpace, ' ', o),
                    'e' => FormatNumber("e", flagAlternate, fieldLength, fieldPrecision, flagLeft2Right, flagPositiveSign, flagPositiveSpace, ' ', o),
                    'E' => FormatNumber("E", flagAlternate, fieldLength, fieldPrecision, flagLeft2Right, flagPositiveSign, flagPositiveSpace, ' ', o),
                    'g' => FormatNumber("g", flagAlternate, fieldLength, fieldPrecision, flagLeft2Right, flagPositiveSign, flagPositiveSpace, ' ', o),
                    'G' => FormatNumber("G", flagAlternate, fieldLength, fieldPrecision, flagLeft2Right, flagPositiveSign, flagPositiveSpace, ' ', o),
                    'p' => o is IntPtr ptr ? "0x" + ptr.ToString("x") : string.Empty,
                    'n' => FormatNumber("d", flagAlternate, fieldLength, int.MinValue, flagLeft2Right, flagPositiveSign, flagPositiveSpace, ' ', m.Index),
                    _ => string.Empty
                };

                f.Remove(m.Index, m.Length);
                f.Insert(m.Index, w);
                m = r.Match(f.ToString(), m.Index + w.Length);
                defaultParamIx++;
            }

            return f.ToString();
        }

        private static string FormatOct(string nativeFormat, bool alternate, int fieldLength, int fieldPrecision, bool left2Right, char padding, object value)
        {
            string w = IsNumericType(value) ? Convert.ToString(UnboxToLong(value, true), 8) : string.Empty;
            if (left2Right || padding == ' ')
            {
                if (alternate && w != "0")
                    w = "0" + w;
                w = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0" + (fieldLength != int.MinValue ? "," + (left2Right ? "-" : string.Empty) + fieldLength.ToString() : string.Empty) + "}", w);
            }
            else
            {
                if (fieldLength != int.MinValue)
                    w = w.PadLeft(fieldLength - (alternate && w != "0" ? 1 : 0), padding);
                if (alternate && w != "0")
                    w = "0" + w;
            }

            return w;
        }

        private static string FormatHex(string nativeFormat, bool alternate, int fieldLength, int fieldPrecision, bool left2Right, char padding, object value)
        {
            string w = IsNumericType(value) ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:" + nativeFormat + (fieldPrecision != int.MinValue ? fieldPrecision.ToString() : string.Empty) + "}", value) : string.Empty;
            if (left2Right || padding == ' ')
            {
                if (alternate)
                    w = (nativeFormat == "x" ? "0x" : "0X") + w;
                w = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0" + (fieldLength != int.MinValue ? "," + (left2Right ? "-" : string.Empty) + fieldLength.ToString() : string.Empty) + "}", w);
            }
            else
            {
                if (fieldLength != int.MinValue)
                    w = w.PadLeft(fieldLength - (alternate ? 2 : 0), padding);
                if (alternate)
                    w = (nativeFormat == "x" ? "0x" : "0X") + w;
            }

            return w;
        }

        private static string FormatNumber(string nativeFormat, bool alternate, int fieldLength, int fieldPrecision, bool left2Right, bool positiveSign, bool positiveSpace, char padding, object value)
        {
            string w = IsNumericType(value) ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:" + nativeFormat + (fieldPrecision != int.MinValue ? fieldPrecision.ToString() : "0") + "}", value) : string.Empty;
            if (left2Right || padding == ' ')
            {
                if (IsPositive(value, true))
                    w = (positiveSign ? "+" : (positiveSpace ? " " : string.Empty)) + w;
                w = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0" + (fieldLength != int.MinValue ? "," + (left2Right ? "-" : string.Empty) + fieldLength.ToString() : string.Empty) + "}", w);
            }
            else
            {
                if (w.StartsWith("-"))
                    w = w.Substring(1);
                if (fieldLength != int.MinValue)
                    w = w.PadLeft(fieldLength - 1, padding);
                if (IsPositive(value, true))
                    w = (positiveSign ? "+" : (positiveSpace ? " " : (fieldLength != int.MinValue ? padding.ToString() : string.Empty))) + w;
                else
                    w = "-" + w;
            }

            return w;
        }
    }
}