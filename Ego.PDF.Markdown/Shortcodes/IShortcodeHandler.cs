namespace Ego.PDF.Markdown.Shortcodes;

/// <summary>
/// Renders a single <see cref="ShortcodeBlock"/> into <paramref name="pdf"/>.
/// The handler is responsible for advancing the cursor (typically by
/// adjusting <c>pdf.Y</c>) so the next block starts below the
/// shortcode output.
/// </summary>
public interface IShortcodeHandler
{
    void Render(FPdf pdf, ShortcodeBlock block, MarkdownTheme theme);
}
