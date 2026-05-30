using System.Collections.Generic;
using System.Text;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Ego.PDF.Markdown.Shortcodes;

/// <summary>
/// Markdig block parser that recognises a single-line shortcode of the
/// form <c>[[name k1=v1 k2="v with spaces" k3='v']]</c> sitting on its
/// own line (allowing trailing whitespace). Inline occurrences inside a
/// paragraph are intentionally NOT picked up; the shortcode must be
/// block-level so the renderer can lay it out vertically.
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

        // Find the matching "]]" within the same line.
        int closeIdx = -1;
        for (int i = start + 2; i < end; i++)
        {
            if (text[i] == ']' && text[i + 1] == ']')
            {
                closeIdx = i;
                break;
            }
        }
        if (closeIdx < 0) return BlockState.None;

        // The rest of the line must be whitespace — this is a block
        // shortcode, not an inline one.
        int after = closeIdx + 2;
        while (after <= end && (text[after] == ' ' || text[after] == '\t')) after++;
        if (after <= end) return BlockState.None;

        var content = text.Substring(start + 2, closeIdx - (start + 2)).Trim();
        if (content.Length == 0) return BlockState.None;

        if (!TryParse(content, out var name, out var options))
        {
            return BlockState.None;
        }

        var block = new ShortcodeBlock(this)
        {
            Span = { Start = start, End = end },
            Line = processor.LineIndex,
            Column = processor.Column,
            Name = name,
            RawText = content,
        };
        foreach (var pair in options)
        {
            block.Options[pair.Key] = pair.Value;
        }

        processor.NewBlocks.Push(block);
        return BlockState.BreakDiscard;
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
