using System.Collections.Generic;
using System.Text;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Ego.PDF.Markdown.Shortcodes;

/// <summary>
/// Markdig block parser that recognises a shortcode of the form
/// <c>[[name k1=v1 k2="v with spaces" k3='v']]</c> sitting on its own
/// line, OR spread across several lines for readability:
///
/// <code>
/// [[imagepair
///     src1="left.png" caption1="Left"
///     src2="right.png" caption2="Right"
///     borders=true
/// ]]
/// </code>
///
/// Inline occurrences inside a paragraph are intentionally NOT picked
/// up; the shortcode must be block-level so the renderer can lay it
/// out vertically.
/// </summary>
public sealed class ShortcodeBlockParser : BlockParser
{
    public ShortcodeBlockParser()
    {
        OpeningCharacters = new[] { '[' };
    }

    public override BlockState TryOpen(BlockProcessor processor)
    {
        if (processor.IsCodeIndent) return BlockState.None;

        var slice = processor.Line;
        var text = slice.Text;
        int start = slice.Start;
        int end = slice.End;

        // Require a literal "[[" at the very start of the line.
        if (start + 1 > end) return BlockState.None;
        if (text[start] != '[' || text[start + 1] != '[') return BlockState.None;

        // Look for "]]" within the same line. If found, the whole
        // shortcode is single-line: parse and close immediately.
        int closeIdx = FindCloseMarker(text, start + 2, end);
        if (closeIdx >= 0)
        {
            // The rest of the line must be whitespace — this is a block
            // shortcode, not an inline one.
            int after = closeIdx + 2;
            while (after <= end && (text[after] == ' ' || text[after] == '\t')) after++;
            if (after <= end) return BlockState.None;

            var content = text.Substring(start + 2, closeIdx - (start + 2)).Trim();
            if (content.Length == 0) return BlockState.None;

            if (!TryParse(content, out var name, out var options))
                return BlockState.None;

            processor.NewBlocks.Push(BuildBlock(processor, start, end, name, content, options));
            return BlockState.BreakDiscard;
        }

        // No "]]" on this line -- multi-line invocation. Stash whatever
        // comes after "[[" into a Pending buffer on the block and ask
        // Markdig to keep feeding us subsequent lines via TryContinue.
        var firstLine = text.Substring(start + 2, end + 1 - (start + 2));
        var block = new ShortcodeBlock(this)
        {
            Span = { Start = start, End = end },
            Line = processor.LineIndex,
            Column = processor.Column,
            Pending = new StringBuilder().Append(firstLine).Append(' '),
        };
        processor.NewBlocks.Push(block);
        return BlockState.ContinueDiscard;
    }

    public override BlockState TryContinue(BlockProcessor processor, Block block)
    {
        var sc = (ShortcodeBlock)block;
        if (sc.Pending is null) return BlockState.Break;

        var slice = processor.Line;
        var text = slice.Text;
        int start = slice.Start;
        int end = slice.End;

        // A blank line inside an open shortcode aborts it -- treat as
        // a paragraph break and let the partial text fall through.
        if (start > end)
        {
            sc.Pending = null;
            return BlockState.None;
        }

        int closeIdx = FindCloseMarker(text, start, end);
        if (closeIdx < 0)
        {
            // Still inside the brackets: append the whole line.
            sc.Pending.Append(text, start, end + 1 - start).Append(' ');
            return BlockState.ContinueDiscard;
        }

        // Found "]]" on this line. The rest must be whitespace, same
        // rule as single-line.
        int after = closeIdx + 2;
        while (after <= end && (text[after] == ' ' || text[after] == '\t')) after++;
        if (after <= end)
        {
            sc.Pending = null;
            return BlockState.None;
        }

        sc.Pending.Append(text, start, closeIdx - start);
        var content = sc.Pending.ToString().Trim();
        sc.Pending = null;

        if (content.Length == 0 || !TryParse(content, out var name, out var options))
        {
            // Couldn't make sense of the accumulated text -- bail. The
            // partial block is discarded; subsequent parsers may pick
            // the lines up as a paragraph.
            return BlockState.None;
        }

        sc.Name = name;
        sc.RawText = content;
        foreach (var pair in options)
            sc.Options[pair.Key] = pair.Value;
        sc.Span = new SourceSpan(sc.Span.Start, end);
        return BlockState.BreakDiscard;
    }

    /// <summary>
    /// Scan <paramref name="text"/> from <paramref name="from"/> for the
    /// nearest <c>]]</c> sequence that is NOT inside a quoted value.
    /// A quoted run starts on <c>"</c> or <c>'</c> and ends on the same
    /// quote character (single-line; <c>\</c> escapes the next char).
    /// Without this, captions like <c>caption="text with ]] in it"</c>
    /// would short-circuit the outer shortcode close.
    /// </summary>
    private static int FindCloseMarker(string text, int from, int end)
    {
        int i = from;
        while (i < end)
        {
            char c = text[i];
            if (c == '"' || c == '\'')
            {
                char quote = c;
                i++;
                while (i < end && text[i] != quote)
                {
                    if (text[i] == '\\' && i + 1 < end) i++;
                    i++;
                }
                if (i < end) i++; // step past the closing quote
                continue;
            }
            if (c == ']' && text[i + 1] == ']')
                return i;
            i++;
        }
        return -1;
    }

    private ShortcodeBlock BuildBlock(BlockProcessor processor, int start, int end,
        string name, string rawText, IList<KeyValuePair<string, string>> options)
    {
        var block = new ShortcodeBlock(this)
        {
            Span = { Start = start, End = end },
            Line = processor.LineIndex,
            Column = processor.Column,
            Name = name,
            RawText = rawText,
        };
        foreach (var pair in options)
            block.Options[pair.Key] = pair.Value;
        return block;
    }

    /// <summary>
    /// Parses <c>name k1=v1 k2="v 2" k3='v3'</c>. Returns false if the
    /// content doesn't look like a shortcode (e.g. starts with '='),
    /// in which case the parser yields control so other block parsers
    /// can have a go.
    /// </summary>
    internal static bool TryParse(string content, out string name, out IList<KeyValuePair<string, string>> options)
    {
        name = string.Empty;
        options = new List<KeyValuePair<string, string>>();

        int i = 0;
        int n = content.Length;

        // Name = first run of non-whitespace, non-equals chars.
        while (i < n && IsWhitespace(content[i])) i++;
        int nameStart = i;
        while (i < n && !IsWhitespace(content[i]) && content[i] != '=') i++;
        if (i == nameStart) return false;
        name = content.Substring(nameStart, i - nameStart);

        // Key=value pairs separated by whitespace.
        while (i < n)
        {
            while (i < n && IsWhitespace(content[i])) i++;
            if (i >= n) break;

            int keyStart = i;
            while (i < n && !IsWhitespace(content[i]) && content[i] != '=') i++;
            if (i == keyStart) break;
            var key = content.Substring(keyStart, i - keyStart);

            // A bare flag (no '=' after) is treated as key with empty value.
            if (i >= n || content[i] != '=')
            {
                options.Add(new KeyValuePair<string, string>(key, string.Empty));
                continue;
            }
            i++; // skip '='

            string value;
            if (i < n && (content[i] == '"' || content[i] == '\''))
            {
                char quote = content[i++];
                var sb = new StringBuilder();
                while (i < n && content[i] != quote)
                {
                    // Allow simple backslash escapes inside quoted values.
                    if (content[i] == '\\' && i + 1 < n)
                    {
                        sb.Append(content[i + 1]);
                        i += 2;
                    }
                    else
                    {
                        sb.Append(content[i++]);
                    }
                }
                if (i < n) i++; // skip closing quote
                value = sb.ToString();
            }
            else
            {
                int valStart = i;
                while (i < n && !IsWhitespace(content[i])) i++;
                value = content.Substring(valStart, i - valStart);
            }
            options.Add(new KeyValuePair<string, string>(key, value));
        }

        return true;
    }

    private static bool IsWhitespace(char c) => c == ' ' || c == '\t';
}
