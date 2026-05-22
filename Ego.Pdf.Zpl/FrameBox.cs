namespace Ego.Pdf.Zpl;

internal class FrameBox
{
    public int MaxWidth { get; set; }
    public int MaxLines { get; set; }
    public int LineSpacing { get; set; }
    public string Alignment { get; set; }
    public int HangingIntent { get; set; }

    public FrameBox(int maxWidth, int maxLines, int lineSpacing, string alignment, int hangingIntent)
    {
        this.MaxWidth = maxWidth;
        this.MaxLines = maxLines;
        this.LineSpacing = lineSpacing;
        this.Alignment = alignment;
        this.HangingIntent = hangingIntent;
    }
}