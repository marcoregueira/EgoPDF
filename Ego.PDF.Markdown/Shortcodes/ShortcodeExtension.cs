using Markdig;
using Markdig.Renderers;

namespace Ego.PDF.Markdown.Shortcodes;

/// <summary>
/// Markdig pipeline extension that plugs in
/// <see cref="ShortcodeBlockParser"/>. Registered automatically by
/// <see cref="MarkdownRenderer"/>; client code never references this
/// directly.
/// </summary>
internal sealed class ShortcodeExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.BlockParsers.Contains<ShortcodeBlockParser>())
        {
            // Insert ahead of the default parsers so [[ is intercepted
            // before any other parser that might consume a '[' at the
            // start of the line.
            pipeline.BlockParsers.Insert(0, new ShortcodeBlockParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        // No-op: rendering is handled by MarkdownRenderer (our own
        // FPdf-based renderer), not by a Markdig IMarkdownRenderer.
    }
}
