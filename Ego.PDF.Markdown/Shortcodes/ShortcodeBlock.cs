using System;
using System.Collections.Generic;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Ego.PDF.Markdown.Shortcodes;

/// <summary>
/// AST node for a block-level shortcode (<c>[[name key=value ...]]</c>
/// on its own line). Created by <see cref="ShortcodeBlockParser"/> and
/// consumed by <see cref="MarkdownRenderer"/>, which looks up a
/// handler by <see cref="Name"/> in the active theme's
/// <c>Shortcodes</c> registry.
/// </summary>
public sealed class ShortcodeBlock : LeafBlock
{
    public ShortcodeBlock(BlockParser? parser) : base(parser)
    {
    }

    /// <summary>The shortcode name (the first token inside the brackets).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parsed key/value pairs from the shortcode arguments. Values are
    /// stored as plain strings; the handler decides how to interpret
    /// them (parse a number, a colour, a boolean, ...).
    /// </summary>
    public IDictionary<string, string> Options { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Original text between the <c>[[</c> and <c>]]</c> markers, kept
    /// so the renderer can fall back to printing it verbatim when no
    /// handler is registered for <see cref="Name"/>.
    /// </summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>
    /// Accumulator used by <see cref="ShortcodeBlockParser"/> while the
    /// block is still open across multiple source lines (i.e. the
    /// opening <c>[[</c> and the closing <c>]]</c> sit on different
    /// lines). Once the closing marker is found this field is parsed
    /// into <see cref="Name"/> + <see cref="Options"/> and cleared.
    /// </summary>
    internal System.Text.StringBuilder Pending { get; set; }
}
