using System;
using System.IO;
using System.Linq;
using System.Text;
using Ego.PDF.Data;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Ego.PDF.Markdown.Shortcodes;
using Microsoft.Xna.Framework;

namespace Ego.PDF.Markdown;

/// <summary>
/// Renders a CommonMark Markdown document onto an <see cref="FPdf"/>
/// page using the rules in <see cref="MarkdownTheme"/>. The renderer
/// uses the current cursor position (X/Y) as the upper-left corner of
/// the first block, and leaves the cursor below the last block when
/// it returns.
///
/// Phase-1 supported nodes:
/// <list type="bullet">
///   <item>Headings (H1-H6).</item>
///   <item>Paragraphs with inline emphasis (bold, italic, code,
///   links, autolinks, hard/soft line breaks).</item>
///   <item>Bullet and ordered lists (nested).</item>
///   <item>Fenced and indented code blocks (with background).</item>
///   <item>Horizontal rules.</item>
///   <item>Block-level local images (<c>![alt](path.png)</c>).</item>
/// </list>
/// Tables, blockquotes, inline images, footnotes and task lists are
/// silently passed through and rendered as plain text (or skipped) in
/// this preview; they will land in later releases.
/// </summary>
public static class MarkdownRenderer
{
    /// <summary>
    /// CommonMark + GitHub Flavored Markdown + EgoPDF shortcodes
    /// (<c>[[name k=v ...]]</c>). Built once and shared across calls.
    /// </summary>
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseTaskLists()
        .UseAutoLinks()
        .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
        .Use<ShortcodeExtension>()
        .Build();

    /// <summary>
    /// Parses <paramref name="markdown"/> and writes it to <paramref name="pdf"/>
    /// using <paramref name="theme"/> (or <see cref="MarkdownTheme.Default"/>).
    /// Add at least one page to the PDF before calling.
    /// </summary>
    public static void Render(FPdf pdf, string markdown, MarkdownTheme? theme = null)
    {
        if (pdf is null) throw new ArgumentNullException(nameof(pdf));
        if (markdown is null) throw new ArgumentNullException(nameof(markdown));
        theme ??= MarkdownTheme.Default;

        var pipeline = Pipeline;
        var doc = Markdig.Markdown.Parse(markdown, pipeline);
        var ctx = new RenderContext(theme);

        foreach (var block in doc)
        {
            RenderBlock(pdf, block, ctx);
        }
    }

    /// <summary>
    /// Loads <paramref name="path"/> as UTF-8 Markdown and renders it.
    /// Convenience wrapper around <see cref="Render"/>.
    /// </summary>
    public static void RenderFile(FPdf pdf, string path, MarkdownTheme? theme = null)
    {
        if (path is null) throw new ArgumentNullException(nameof(path));
        var text = File.ReadAllText(path, Encoding.UTF8);
        Render(pdf, text, theme);
    }

    // ---- Block dispatch ----------------------------------------------------

    private static void RenderBlock(FPdf pdf, Block block, RenderContext ctx)
    {
        switch (block)
        {
            case HeadingBlock h:        RenderHeading(pdf, h, ctx); break;
            case ParagraphBlock p:      RenderParagraph(pdf, p, ctx); break;
            case ListBlock list:        RenderList(pdf, list, ctx); break;
            case ThematicBreakBlock _:  RenderThematicBreak(pdf, ctx); break;
            case FencedCodeBlock fcb:   RenderCodeBlock(pdf, fcb, ctx); break;
            case CodeBlock cb:          RenderCodeBlock(pdf, cb, ctx); break;
            case QuoteBlock q:          RenderQuote(pdf, q, ctx); break;
            case ShortcodeBlock sc:     RenderShortcode(pdf, sc, ctx); break;
            default:                    /* skip unknown block types in phase 1 */ break;
        }
    }

    // ---- Headings ----------------------------------------------------------

    private static void RenderHeading(FPdf pdf, HeadingBlock h, RenderContext ctx)
    {
        var theme = ctx.Theme;
        var level = Math.Clamp(h.Level, 1, theme.HeadingSizes.Length);
        var size = theme.HeadingSizes[ level - 1 ];

        pdf.Y += theme.HeadingSpacingAbove;
        pdf.SetFont(theme.HeadingFont, "B", size);
        pdf.SetTextColor(theme.HeadingColor);

        var inlineCtx = ctx.InlineForBlock(theme.HeadingFont, "B", size, theme.HeadingColor);
        RenderInlines(pdf, h.Inline, inlineCtx);

        pdf.Ln(LineHeightFor(size, theme));
        pdf.Y += theme.HeadingSpacingBelow;
    }

    // ---- Paragraphs --------------------------------------------------------

    private static void RenderParagraph(FPdf pdf, ParagraphBlock p, RenderContext ctx)
    {
        var theme = ctx.Theme;

        // Block-image shorthand: a paragraph whose only child is `![alt](url)`
        // becomes a block-level image. Phase 1 only supports local files.
        if (p.Inline?.FirstChild is LinkInline link &&
            link.NextSibling is null &&
            link.IsImage)
        {
            RenderBlockImage(pdf, link, theme);
            return;
        }

        pdf.SetFont(theme.BodyFont, "", theme.BodyFontSize);
        pdf.SetTextColor(theme.BodyColor);

        var inlineCtx = ctx.InlineForBlock(theme.BodyFont, "", theme.BodyFontSize, theme.BodyColor);
        RenderInlines(pdf, p.Inline, inlineCtx);

        pdf.Ln(theme.LineHeight);
        pdf.Y += theme.ParagraphSpacing;
    }

    // ---- Lists -------------------------------------------------------------

    private static void RenderList(FPdf pdf, ListBlock list, RenderContext ctx)
    {
        var theme = ctx.Theme;
        var savedLeftMargin = pdf.LeftMargin;
        pdf.SetLeftMargin(savedLeftMargin + theme.ListIndent);
        pdf.SetX(pdf.LeftMargin);

        int n = list.OrderedStart != null && int.TryParse(list.OrderedStart, out var start) ? start : 1;

        foreach (var child in list)
        {
            if (child is not ListItemBlock item) continue;

            var marker = list.IsOrdered ? $"{n++}." : theme.BulletGlyph;
            pdf.SetFont(theme.BodyFont, "", theme.BodyFontSize);
            pdf.SetTextColor(theme.MutedColor);
            pdf.SetX(pdf.LeftMargin - theme.ListMarkerWidth);
            pdf.Cell(theme.ListMarkerWidth, theme.LineHeight, marker);

            // Render the item's blocks (paragraph, sub-list, code, ...).
            // First block continues on the same line as the bullet; subsequent
            // blocks naturally start at the new (indented) LeftMargin.
            bool first = true;
            foreach (var itemChild in item)
            {
                if (!first)
                {
                    pdf.SetX(pdf.LeftMargin);
                }
                RenderBlock(pdf, itemChild, ctx);
                first = false;
            }
        }

        pdf.SetLeftMargin(savedLeftMargin);
        pdf.SetX(savedLeftMargin);
    }

    // ---- Thematic break ----------------------------------------------------

    private static void RenderThematicBreak(FPdf pdf, RenderContext ctx)
    {
        var theme = ctx.Theme;
        pdf.Y += theme.ParagraphSpacing;
        pdf.SetDrawColor(theme.RuleColor);
        pdf.SetLineWidth(0.3);
        var y = pdf.Y;
        pdf.Line(pdf.LeftMargin, y, pdf.W - pdf.RightMargin, y);
        pdf.Y += theme.ParagraphSpacing;
        pdf.SetX(pdf.LeftMargin);
    }

    // ---- Code blocks -------------------------------------------------------

    private static void RenderCodeBlock(FPdf pdf, LeafBlock code, RenderContext ctx)
    {
        var theme = ctx.Theme;
        var text = CollectLines(code);
        if (string.IsNullOrEmpty(text)) return;

        var lines = text.Split('\n');
        var padding = theme.CodeBlockPadding;
        var lineHeight = theme.LineHeight - 0.5;
        var blockHeight = lines.Length * lineHeight + 2 * padding;
        var contentWidth = pdf.W - pdf.LeftMargin - pdf.RightMargin;

        var blockX = pdf.LeftMargin;
        var blockY = pdf.Y;

        pdf.SetFillColor(theme.CodeBackground);
        pdf.Rect(blockX, blockY, contentWidth, blockHeight, "F");

        pdf.SetFont(theme.CodeFont, "", theme.CodeFontSize);
        pdf.SetTextColor(theme.CodeColor);
        pdf.SetXY(blockX + padding, blockY + padding);

        foreach (var line in lines)
        {
            pdf.SetX(blockX + padding);
            pdf.Cell(contentWidth - 2 * padding, lineHeight, line);
            pdf.Ln(lineHeight);
        }

        pdf.SetX(pdf.LeftMargin);
        pdf.Y = blockY + blockHeight + theme.ParagraphSpacing;
    }

    private static string CollectLines(LeafBlock code)
    {
        var sb = new StringBuilder();
        var slices = code.Lines.Lines;
        for (int i = 0; i < code.Lines.Count; i++)
        {
            var slice = slices[ i ].Slice;
            if (slice.Text is null) continue;
            sb.Append(slice.ToString());
            sb.Append('\n');
        }
        return sb.ToString().TrimEnd('\n', '\r');
    }

    // ---- Shortcode ---------------------------------------------------------

    private static void RenderShortcode(FPdf pdf, ShortcodeBlock sc, RenderContext ctx)
    {
        var theme = ctx.Theme;
        pdf.SetX(pdf.LeftMargin);

        if (theme.Shortcodes.TryGet(sc.Name, out var handler))
        {
            handler.Render(pdf, sc, theme);
            pdf.SetX(pdf.LeftMargin);
            return;
        }

        // No handler — render the raw token as muted italic so the
        // author notices something is off without breaking the layout.
        pdf.SetFont(theme.BodyFont, "I", theme.BodyFontSize);
        pdf.SetTextColor(theme.MutedColor);
        var lh = System.Math.Max(1, (int)System.Math.Round(theme.LineHeight));
        pdf.Write(lh, "[[" + sc.RawText + "]]");
        pdf.Ln(theme.LineHeight);
        pdf.Y += theme.ParagraphSpacing;
        pdf.SetTextColor(theme.BodyColor);
    }

    // ---- Quote -------------------------------------------------------------

    private static void RenderQuote(FPdf pdf, QuoteBlock q, RenderContext ctx)
    {
        var theme = ctx.Theme;
        var savedLeftMargin = pdf.LeftMargin;
        var quoteIndent = theme.ListIndent;
        var ruleX = savedLeftMargin + 1;

        pdf.SetLeftMargin(savedLeftMargin + quoteIndent);
        pdf.SetX(pdf.LeftMargin);

        var startY = pdf.Y;
        var muted = ctx.WithBodyColor(theme.MutedColor);
        foreach (var child in q)
        {
            RenderBlock(pdf, child, muted);
        }
        var endY = pdf.Y;

        // Accent rule on the left, spanning the quote vertically.
        pdf.SetDrawColor(theme.AccentColor);
        pdf.SetLineWidth(0.7);
        pdf.Line(ruleX, startY, ruleX, endY - theme.ParagraphSpacing);

        pdf.SetLeftMargin(savedLeftMargin);
        pdf.SetX(savedLeftMargin);
    }

    // ---- Block image -------------------------------------------------------

    private static void RenderBlockImage(FPdf pdf, LinkInline imgLink, MarkdownTheme theme)
    {
        var url = imgLink.Url ?? "";
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        if (url.StartsWith("http:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https:", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            // Phase 1: skip remote / inline-data images. Drop a muted alt line.
            var altText = "[image: " + (FlattenLiteral(imgLink) ?? url) + "]";
            pdf.SetFont(theme.BodyFont, "I", theme.BodyFontSize);
            pdf.SetTextColor(theme.MutedColor);
            var lh = Math.Max(1, (int)Math.Round(theme.LineHeight));
            pdf.Write(lh, altText);
            pdf.Ln(theme.LineHeight);
            pdf.Y += theme.ParagraphSpacing;
            return;
        }

        var path = url;
        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(path);
        }
        if (!File.Exists(path))
        {
            return;
        }

        var contentWidth = pdf.W - pdf.LeftMargin - pdf.RightMargin;

        // Use the image's natural pixel size translated through ImageDpi
        // so a 64x64 icon renders small and an 1800x1200 photo renders
        // big — instead of every image inflating to fill the column.
        // Falls back to full column width if Skia can't read the size.
        var (pxW, pxH) = TryReadImageDimensions(path);
        double widthMm = pxW > 0
            ? Math.Min(contentWidth, Shortcodes.ImageShortcode.PixelsToMm(pxW, theme.ImageDpi))
            : contentWidth;
        double heightMm = pxW > 0 && pxH > 0
            ? widthMm * pxH / pxW
            : 0; // 0 lets FPdf.Image preserve the aspect ratio itself

        pdf.Image(path, pdf.LeftMargin, pdf.Y, widthMm, heightMm);
        pdf.Y += theme.ParagraphSpacing;
        pdf.SetX(pdf.LeftMargin);
    }

    private static (int width, int height) TryReadImageDimensions(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var codec = SkiaSharp.SKCodec.Create(stream);
            if (codec is null) return (0, 0);
            return (codec.Info.Width, codec.Info.Height);
        }
        catch
        {
            return (0, 0);
        }
    }

    // ---- Inlines -----------------------------------------------------------

    private static void RenderInlines(FPdf pdf, ContainerInline? container, InlineContext ctx)
    {
        if (container is null) return;

        // Collapse runs of LiteralInline + soft LineBreakInline into a single
        // Write call. Multiple consecutive Writes produce multiple Tj
        // operations in the PDF stream; each one carries CMargin padding
        // inside its Cell that accumulates visually into "double space"
        // artefacts between words across emit boundaries. Concatenating
        // them into one string lets FPdf.Write emit a single tight Tj.
        var buffer = new StringBuilder();
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline lit:
                    buffer.Append(lit.Content.ToString());
                    break;
                case LineBreakInline { IsHard: false }:
                    buffer.Append(' ');
                    break;
                case LineBreakInline { IsHard: true }:
                    FlushBuffer(pdf, buffer, ctx);
                    pdf.Ln(ctx.LineHeight);
                    break;
                default:
                    FlushBuffer(pdf, buffer, ctx);
                    RenderInline(pdf, inline, ctx);
                    break;
            }
        }
        FlushBuffer(pdf, buffer, ctx);
    }

    private static void FlushBuffer(FPdf pdf, StringBuilder buffer, InlineContext ctx)
    {
        if (buffer.Length == 0) return;
        pdf.Write(ctx.LineHeight, buffer.ToString());
        buffer.Clear();
    }

    private static void RenderInline(FPdf pdf, Inline inline, InlineContext ctx)
    {
        switch (inline)
        {
            case EmphasisInline em:
            {
                // Markdig encodes ** / __ as DelimiterCount == 2 (strong),
                // single * / _ as DelimiterCount == 1 (em).
                var addStyle = em.DelimiterCount >= 2 ? "B" : "I";
                var saved = ctx.Style;
                ctx.Style = CombineStyle(saved, addStyle);
                pdf.SetFont(ctx.Family, ctx.Style, ctx.Size);
                RenderInlines(pdf, em, ctx);
                ctx.Style = saved;
                pdf.SetFont(ctx.Family, ctx.Style, ctx.Size);
                break;
            }

            case CodeInline code:
            {
                var savedFamily = ctx.Family;
                var savedSize = ctx.Size;
                var savedColor = ctx.Color;
                ctx.Family = ctx.Theme.CodeFont;
                ctx.Size = ctx.Theme.CodeFontSize;
                ctx.Color = ctx.Theme.CodeColor;
                pdf.SetFont(ctx.Family, ctx.Style, ctx.Size);
                pdf.SetTextColor(ctx.Color);
                pdf.Write(ctx.LineHeight, code.Content);
                ctx.Family = savedFamily;
                ctx.Size = savedSize;
                ctx.Color = savedColor;
                pdf.SetFont(ctx.Family, ctx.Style, ctx.Size);
                pdf.SetTextColor(ctx.Color);
                break;
            }

            case LinkInline link when !link.IsImage:
            {
                var savedColor = ctx.Color;
                var savedStyle = ctx.Style;
                ctx.Color = ctx.Theme.AccentColor;
                ctx.Style = CombineStyle(savedStyle, "U");
                pdf.SetFont(ctx.Family, ctx.Style, ctx.Size);
                pdf.SetTextColor(ctx.Color);
                RenderInlines(pdf, link, ctx);
                ctx.Color = savedColor;
                ctx.Style = savedStyle;
                pdf.SetFont(ctx.Family, ctx.Style, ctx.Size);
                pdf.SetTextColor(ctx.Color);
                break;
            }

            case LinkInline imgInline when imgInline.IsImage:
            {
                // Inline image inside a paragraph — render alt text as a
                // fallback so the document still flows. Block-level images
                // are caught earlier in RenderParagraph.
                var alt = FlattenLiteral(imgInline) ?? "image";
                pdf.SetTextColor(ctx.Theme.MutedColor);
                pdf.Write(ctx.LineHeight, $"[{alt}]");
                pdf.SetTextColor(ctx.Color);
                break;
            }

            case AutolinkInline auto:
            {
                var savedColor = ctx.Color;
                var savedStyle = ctx.Style;
                ctx.Color = ctx.Theme.AccentColor;
                ctx.Style = CombineStyle(savedStyle, "U");
                pdf.SetFont(ctx.Family, ctx.Style, ctx.Size);
                pdf.SetTextColor(ctx.Color);
                pdf.Write(ctx.LineHeight, auto.Url);
                ctx.Color = savedColor;
                ctx.Style = savedStyle;
                pdf.SetFont(ctx.Family, ctx.Style, ctx.Size);
                pdf.SetTextColor(ctx.Color);
                break;
            }

            case ContainerInline container:
                RenderInlines(pdf, container, ctx);
                break;

            // HtmlInline, HtmlEntityInline, etc. are silently dropped in Phase 1.
        }
    }

    // ---- Helpers -----------------------------------------------------------

    private static string CombineStyle(string current, string add)
    {
        if (string.IsNullOrEmpty(current)) return add;
        foreach (var c in add)
        {
            if (!current.Contains(c)) current += c;
        }
        return current;
    }

    private static double LineHeightFor(double fontSizePoints, MarkdownTheme theme)
    {
        // Roughly 1.2x the visual ascent. For 11pt body @ default LineHeight 5.5
        // this returns 5.5; for a 20pt H2 it returns ~10.
        return theme.LineHeight * (fontSizePoints / theme.BodyFontSize);
    }

    private static string? FlattenLiteral(ContainerInline container)
    {
        var sb = new StringBuilder();
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline lit: sb.Append(lit.Content.ToString()); break;
                case ContainerInline c: sb.Append(FlattenLiteral(c)); break;
            }
        }
        return sb.Length == 0 ? null : sb.ToString();
    }

    // ---- Contexts ----------------------------------------------------------

    private sealed class RenderContext
    {
        public MarkdownTheme Theme { get; }

        public RenderContext(MarkdownTheme theme) { Theme = theme; }

        public InlineContext InlineForBlock(string family, string style, double size, Color color)
            => new(Theme)
            {
                Family = family,
                Style = style,
                Size = size,
                Color = color,
                // FPdf.Write expects an int line-height. Round so paragraphs
                // and headings get the closest whole-mm spacing.
                LineHeight = Math.Max(1, (int)Math.Round(LineHeightFor(size, Theme))),
            };

        public RenderContext WithBodyColor(Color color)
        {
            // The "muted body" override is only used by quote blocks for now.
            var clone = new MarkdownTheme
            {
                BodyFont = Theme.BodyFont,
                HeadingFont = Theme.HeadingFont,
                CodeFont = Theme.CodeFont,
                BodyFontSize = Theme.BodyFontSize,
                HeadingSizes = Theme.HeadingSizes,
                CodeFontSize = Theme.CodeFontSize,
                LineHeight = Theme.LineHeight,
                ParagraphSpacing = Theme.ParagraphSpacing,
                HeadingSpacingAbove = Theme.HeadingSpacingAbove,
                HeadingSpacingBelow = Theme.HeadingSpacingBelow,
                ListIndent = Theme.ListIndent,
                ListMarkerWidth = Theme.ListMarkerWidth,
                CodeBlockPadding = Theme.CodeBlockPadding,
                BodyColor = color,
                HeadingColor = color,
                AccentColor = Theme.AccentColor,
                CodeColor = Theme.CodeColor,
                CodeBackground = Theme.CodeBackground,
                RuleColor = Theme.RuleColor,
                MutedColor = Theme.MutedColor,
                BulletGlyph = Theme.BulletGlyph,
            };
            return new RenderContext(clone);
        }
    }

    private sealed class InlineContext
    {
        public MarkdownTheme Theme { get; }
        public string Family { get; set; } = "";
        public string Style { get; set; } = "";
        public double Size { get; set; }
        public Color Color { get; set; }

        /// <summary>Line height in user units. <see cref="FPdf.Write"/> takes int.</summary>
        public int LineHeight { get; set; }

        public InlineContext(MarkdownTheme theme) { Theme = theme; }
    }
}
