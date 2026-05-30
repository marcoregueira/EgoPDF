namespace Ego.PDF.Markdown.Shortcodes;

/// <summary>
/// Built-in <c>[[pagebreak]]</c> shortcode: starts a new page.
/// CommonMark / GFM have no native page-break syntax, so the
/// renderer always registers this handler in every fresh
/// <see cref="ShortcodeRegistry"/>. Callers may override by
/// registering a different handler under the name <c>pagebreak</c>.
/// </summary>
public sealed class PageBreakShortcode : IShortcodeHandler
{
    /// <summary>Shared instance — the handler is stateless.</summary>
    public static readonly PageBreakShortcode Instance = new PageBreakShortcode();

    private PageBreakShortcode() { }

    public void Render(FPdf pdf, ShortcodeBlock block, MarkdownTheme theme)
    {
        pdf.AddPage();
    }
}
