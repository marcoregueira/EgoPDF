using System;
using System.Collections.Generic;

namespace Ego.PDF.Markdown.Shortcodes;

/// <summary>
/// Name → handler lookup used by <see cref="MarkdownRenderer"/> when it
/// hits a <see cref="ShortcodeBlock"/>. Lookups are
/// case-insensitive. The registry lives on <see cref="MarkdownTheme"/>
/// so handlers and visual settings travel together.
/// </summary>
public sealed class ShortcodeRegistry
{
    private readonly Dictionary<string, IShortcodeHandler> _handlers =
        new Dictionary<string, IShortcodeHandler>(StringComparer.OrdinalIgnoreCase);

    public ShortcodeRegistry()
    {
        // Built-in shortcodes that don't depend on anything outside
        // EgoPDF.Generator are registered up front. Users can override
        // them by re-registering under the same name.
        Register("pagebreak", PageBreakShortcode.Instance);
        Register("image", ImageShortcode.Instance);
    }

    public ShortcodeRegistry Register(string name, IShortcodeHandler handler)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Shortcode name is required.", nameof(name));
        _handlers[name] = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }

    /// <summary>
    /// Convenience overload for callers that prefer a lambda over a
    /// dedicated class.
    /// </summary>
    public ShortcodeRegistry Register(string name, Action<FPdf, ShortcodeBlock, MarkdownTheme> render)
        => Register(name, new DelegateHandler(render));

    public bool TryGet(string name, out IShortcodeHandler handler)
    {
        if (_handlers.TryGetValue(name, out var found))
        {
            handler = found;
            return true;
        }
        handler = default!;
        return false;
    }

    public bool Contains(string name) => _handlers.ContainsKey(name);

    private sealed class DelegateHandler : IShortcodeHandler
    {
        private readonly Action<FPdf, ShortcodeBlock, MarkdownTheme> _render;
        public DelegateHandler(Action<FPdf, ShortcodeBlock, MarkdownTheme> render)
        {
            _render = render ?? throw new ArgumentNullException(nameof(render));
        }
        public void Render(FPdf pdf, ShortcodeBlock block, MarkdownTheme theme) => _render(pdf, block, theme);
    }
}
