using System;
using System.Collections.Generic;

namespace Ego.PDF.Barcodes;

/// <summary>
/// Hand-rolled Code 128 encoder. Replaces ZXing.Net's Code128Writer
/// for the ^BC path so we can honour ZPL's "&gt;X" mid-stream escapes
/// (subset switches and FNC characters), which ZXing does not model.
///
/// What ZXing missed:
///   - ">5" as a Code Set C switch (the 15420-E-2026 label puts ">5"
///     between the prefix "%000115007" and the trailing 18-digit run;
///     Labelary emits 23 symbols, matching exactly: Start A + % +
///     9 digits A + CodeC + 9 pairs C + check + stop = 759 dots,
///     matching the &gt;GB0,765,8 ruler the author placed alongside).
///   - ">:" / ">;" / ">>" appearing mid-data as switches (not only
///     as the leading selector ZXing's COMPACT / FORCE_CODESET hints
///     allowed).
///   - FNC1..4 escapes ">1".."&gt;4" interleaved with text.
///
/// The encoder still does best-effort automatic Subset C compression
/// when the caller leaves ^BC in Mode A (or empty mode) and the data
/// has no explicit selector — matching the prior ZXing COMPACT
/// behaviour so digit-heavy fields still render at minimum width.
/// </summary>
internal static class Code128Encoder
{
    // Code 128 symbol values (ISO/IEC 15417).
    private const int CODE_FNC1 = 102;
    private const int CODE_FNC2 = 97;
    private const int CODE_FNC3 = 96;
    private const int CODE_FNC4_A = 101;
    private const int CODE_FNC4_B = 100;
    private const int CODE_SHIFT = 98;
    private const int CODE_TO_C = 99;
    private const int CODE_TO_B = 100;
    private const int CODE_TO_A = 101;
    private const int START_A = 103;
    private const int START_B = 104;
    private const int START_C = 105;
    private const int STOP = 106;

    // Bit-pattern table. 107 entries: values 0..105 are 11 modules,
    // value 106 (STOP) is 13 modules and includes the terminator bar.
    // '1' = bar, '0' = space.
    private static readonly string[] Patterns =
    {
        "11011001100","11001101100","11001100110","10010011000",
        "10010001100","10001001100","10011001000","10011000100",
        "10001100100","11001001000","11001000100","11000100100",
        "10110011100","10011011100","10011001110","10111001100",
        "10011101100","10011100110","11001110010","11001011100",
        "11001001110","11011100100","11001110100","11101101110",
        "11101001100","11100101100","11100100110","11101100100",
        "11100110100","11100110010","11011011000","11011000110",
        "11000110110","10100011000","10001011000","10001000110",
        "10110001000","10001101000","10001100010","11010001000",
        "11000101000","11000100010","10110111000","10110001110",
        "10001101110","10111011000","10111000110","10001110110",
        "11101110110","11010001110","11000101110","11011101000",
        "11011100010","11011101110","11101011000","11101000110",
        "11100010110","11101101000","11101100010","11100011010",
        "11101111010","11001000010","11110001010","10100110000",
        "10100001100","10010110000","10010000110","10000101100",
        "10000100110","10110010000","10110000100","10011010000",
        "10011000010","10000110100","10000110010","11000010010",
        "11001010000","11110111010","11000010100","10001111010",
        "10100111100","10010111100","10010011110","10111100100",
        "10011110100","10011110010","11110100100","11110010100",
        "11110010010","11011011110","11011110110","11110110110",
        "10101111000","10100011110","10001011110","10111101000",
        "10111100010","11110101000","11110100010","10111011110",
        "10111101110","11101011110","11110101110","11010000100",
        "11010010000","11010011100","1100011101011"
    };

    private enum Subset { A, B, C }

    private enum TokenKind
    {
        Char,
        SwitchA, SwitchB, SwitchC,
        Fnc1, Fnc2, Fnc3, Fnc4,
    }

    private struct Token
    {
        public TokenKind Kind;
        public char Ch;
    }

    /// <summary>
    /// Encode a ZPL ^BC data payload into the bool[] bar/space bitmap
    /// the renderer already consumes. Returns null if the payload is
    /// empty after tokenization.
    /// </summary>
    /// <param name="data">Field data WITH the leading ZPL selector
    /// ("&gt;:", "&gt;;", "&gt;&gt;") if any — the encoder strips it
    /// and uses it as the start subset.</param>
    /// <param name="autoCompress">When true (Mode A / empty mode and no
    /// explicit selector), the encoder may switch into Subset C for
    /// digit runs of 4+ to minimise symbol count. When false (explicit
    /// selector, or Mode N/U/D), the encoder honours only the explicit
    /// "&gt;X" switches present in the data.</param>
    public static bool[] Encode(string data, bool autoCompress)
    {
        if (string.IsNullOrEmpty(data)) return Array.Empty<bool>();

        // Strip leading ZPL selector and remember it for the start
        // subset; subsequent ">X" become mid-stream tokens.
        Subset? leadingSelector = null;
        if (data.Length >= 2 && data[0] == '>')
        {
            switch (data[1])
            {
                case ':': leadingSelector = Subset.A; data = data.Substring(2); break;
                case ';': leadingSelector = Subset.C; data = data.Substring(2); break;
                case '>': leadingSelector = Subset.B; data = data.Substring(2); break;
            }
        }

        var tokens = Tokenize(data);
        if (tokens.Count == 0) return Array.Empty<bool>();

        // When the author wrote an explicit selector we lock the
        // automatic Subset-C path: their layout choices win.
        if (leadingSelector.HasValue) autoCompress = false;

        var start = leadingSelector ?? PickStartSubset(tokens);
        var symbols = new List<int>(tokens.Count + 4);
        symbols.Add(StartSymbol(start));

        var current = start;
        int i = 0;
        while (i < tokens.Count)
        {
            var tk = tokens[i];

            switch (tk.Kind)
            {
                case TokenKind.SwitchA:
                    if (current != Subset.A) { symbols.Add(CODE_TO_A); current = Subset.A; }
                    i++;
                    continue;
                case TokenKind.SwitchB:
                    if (current != Subset.B) { symbols.Add(CODE_TO_B); current = Subset.B; }
                    i++;
                    continue;
                case TokenKind.SwitchC:
                    if (current != Subset.C) { symbols.Add(CODE_TO_C); current = Subset.C; }
                    i++;
                    continue;
                case TokenKind.Fnc1:
                    symbols.Add(CODE_FNC1);
                    i++;
                    continue;
                case TokenKind.Fnc2:
                    EnsureNotC(symbols, ref current);
                    symbols.Add(CODE_FNC2);
                    i++;
                    continue;
                case TokenKind.Fnc3:
                    EnsureNotC(symbols, ref current);
                    symbols.Add(CODE_FNC3);
                    i++;
                    continue;
                case TokenKind.Fnc4:
                    EnsureNotC(symbols, ref current);
                    symbols.Add(current == Subset.A ? CODE_FNC4_A : CODE_FNC4_B);
                    i++;
                    continue;
            }

            // Char token. Apply automatic C compression only when the
            // caller asked for it and we're not already locked by an
            // explicit selector.
            if (autoCompress && current != Subset.C && DigitRunLength(tokens, i) >= 4)
            {
                symbols.Add(CODE_TO_C);
                current = Subset.C;
            }

            if (current == Subset.C)
            {
                // Need a pair of digit chars to stay in C. If we don't
                // have one, pop back to B (the broader subset).
                if (i + 1 < tokens.Count
                    && tokens[i].Kind == TokenKind.Char && IsDigit(tokens[i].Ch)
                    && tokens[i + 1].Kind == TokenKind.Char && IsDigit(tokens[i + 1].Ch))
                {
                    symbols.Add((tokens[i].Ch - '0') * 10 + (tokens[i + 1].Ch - '0'));
                    i += 2;
                    continue;
                }
                symbols.Add(CODE_TO_B);
                current = Subset.B;
            }

            symbols.Add(EncodeChar(current, tokens[i].Ch));
            i++;
        }

        // Checksum: (startValue * 1) + sum(position * value) for
        // position starting at 1 over the data symbols. The start
        // symbol is index 0 in our list and weighted 1, which is
        // equivalent to weighting subsequent symbols at 1, 2, 3, ...
        long sum = symbols[0];
        for (int k = 1; k < symbols.Count; k++) sum += (long)symbols[k] * k;
        symbols.Add((int)(sum % 103));
        symbols.Add(STOP);

        return Expand(symbols);
    }

    private static List<Token> Tokenize(string data)
    {
        var list = new List<Token>(data.Length);
        int i = 0;
        while (i < data.Length)
        {
            if (data[i] == '>' && i + 1 < data.Length)
            {
                char x = data[i + 1];
                Token? mapped = MapEscape(x);
                if (mapped.HasValue) { list.Add(mapped.Value); i += 2; continue; }
                // Unknown ">X" — drop silently (the legacy regex did
                // the same; treating it as literal would scan as ">"
                // + char, which is almost never the author's intent).
                i += 2;
                continue;
            }
            list.Add(new Token { Kind = TokenKind.Char, Ch = data[i] });
            i++;
        }
        return list;
    }

    private static Token? MapEscape(char x)
    {
        // ">:" / ">;" / ">>" as mid-stream switches. As initial
        // selectors they're peeled off before Tokenize().
        switch (x)
        {
            case ':': return new Token { Kind = TokenKind.SwitchA };
            case ';': return new Token { Kind = TokenKind.SwitchC };
            case '>': return new Token { Kind = TokenKind.SwitchB };
            case '1': return new Token { Kind = TokenKind.Fnc1 };
            case '2': return new Token { Kind = TokenKind.Fnc2 };
            case '3': return new Token { Kind = TokenKind.Fnc3 };
            case '4': return new Token { Kind = TokenKind.Fnc4 };
            // ">5" is the smoking gun on the 15420 label: between
            // 9 digits (rendered in A) and 18 digits (rendered as
            // 9 pairs in C). 23-symbol math (= 759 dots, matching
            // the ^GB0,765,8 ruler) only fits if >5 is a Code C
            // switch. Mapped accordingly.
            case '5': return new Token { Kind = TokenKind.SwitchC };
            // ">6" / ">7" similarly map to Code A / Code B switches
            // per the Zebra docs' symmetric table. Untested on a
            // real label but the table is symmetric and the cost of
            // misreading them as literals (which the legacy regex
            // path did anyway) is the same.
            case '6': return new Token { Kind = TokenKind.SwitchA };
            case '7': return new Token { Kind = TokenKind.SwitchB };
            default: return null;
        }
    }

    private static Subset PickStartSubset(List<Token> tokens)
    {
        int leadingDigits = DigitRunLength(tokens, 0);
        if (leadingDigits >= 4 && (leadingDigits == tokens.Count || leadingDigits >= 4))
            return Subset.C;
        return Subset.B;
    }

    private static int DigitRunLength(List<Token> tokens, int from)
    {
        int n = 0;
        for (int i = from; i < tokens.Count; i++)
        {
            if (tokens[i].Kind != TokenKind.Char) break;
            if (!IsDigit(tokens[i].Ch)) break;
            n++;
        }
        return n;
    }

    private static bool IsDigit(char c) => c >= '0' && c <= '9';

    private static int StartSymbol(Subset s) => s switch
    {
        Subset.A => START_A,
        Subset.C => START_C,
        _ => START_B,
    };

    private static void EnsureNotC(List<int> symbols, ref Subset current)
    {
        if (current == Subset.C)
        {
            symbols.Add(CODE_TO_B);
            current = Subset.B;
        }
    }

    private static int EncodeChar(Subset subset, char ch)
    {
        int code = ch;
        if (subset == Subset.A)
        {
            if (code >= 32 && code <= 95) return code - 32;
            if (code >= 0 && code <= 31) return code + 64;
            // Out-of-subset fall back: treat as space.
            return 0;
        }
        if (subset == Subset.B)
        {
            if (code >= 32 && code <= 127) return code - 32;
            return 0;
        }
        // Subset C is digit-pair-only; single chars shouldn't reach
        // here because the walker pops back to B before calling us.
        return 0;
    }

    private static bool[] Expand(List<int> symbols)
    {
        int total = 0;
        foreach (var s in symbols) total += Patterns[s].Length;
        var bits = new bool[total];
        int idx = 0;
        foreach (var s in symbols)
        {
            var p = Patterns[s];
            for (int k = 0; k < p.Length; k++) bits[idx++] = p[k] == '1';
        }
        return bits;
    }
}
