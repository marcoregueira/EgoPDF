using Ego.PDF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Ego.PDF.Data
{
    public class TableRow
    {
        public List<TableCell> Cells { get; set; } = new List<TableCell>();
        public double CellHeight { get; set; } = 6;

        public double MaxHeight;
        public string Border { get; set; }

        public TableRow()
        {

        }

        public TableRow(params double[] widths) : this()
        {
            foreach (var width in widths)
            {
                this.Cells.Add(new TableCell() { Width = width });
            }
        }

        public TableRow NormalizeWidths(double width)
        {
            var sum = Cells.Sum(x => x.Width);
            if (width > 0)
            {
                var correction = width / sum;
                foreach (var tableCell in Cells)
                {
                    tableCell.Width = tableCell.Width / (sum / width);
                }
            }
            return this;
        }

        public TableRow SetBold(bool bold = true)
        {
            foreach (var tableCell in this.Cells)
            {
                tableCell.Bold = bold;
            }
            return this;
        }

        public TableRow SetBorder(string border = "None")
        {
            foreach (var tableCell in this.Cells)
            {
                tableCell.Border = border;
            }
            return this;
        }

        public TableRow SetAlign(AlignEnum align)
        {
            foreach (var tableCell in this.Cells)
            {
                tableCell.Align = align;
            }
            return this;
        }


        public TableRow SetCellHeight(double height)
        {
            this.CellHeight = height;
            return this;
        }

        // ---- Fluent additions for the new Render / Measure path ----

        /// <summary>
        /// Alias for <see cref="NormalizeWidths(double)"/> with a name that
        /// reads better in a build chain: the column proportions are
        /// already on the row, this fixes the total physical width.
        /// </summary>
        public TableRow Width(double totalWidth) => NormalizeWidths(totalWidth);

        /// <summary>
        /// Apply the same set of border flags to every cell. Mostly useful
        /// for the label-on-top / value-below form pattern -- pair two rows with
        /// <c>Borders.All &amp; ~Borders.Bottom</c> on the labels and
        /// <c>Borders.All &amp; ~Borders.Top</c> on the values to get a
        /// merged-looking two-line box.
        /// </summary>
        public TableRow Bordered(Borders b)
        {
            var s = b.ToFpdfBorderString();
            foreach (var c in Cells) c.Border = s;
            return this;
        }

        /// <summary>
        /// Pre-assign a role to each column so positional
        /// <c>Render(pdf, "A", "B", ...)</c> applies it without the caller
        /// supplying lambdas. Without this every column defaults to
        /// <see cref="TableCell.Data(string)"/>.
        /// </summary>
        public TableRow Roles(params CellRole[] roles)
        {
            for (int i = 0; i < Math.Min(roles.Length, Cells.Count); i++)
                Cells[ i ].Role = roles[ i ];
            return this;
        }
    }

    /// <summary>
    /// Cell role used by the positional <c>Render(pdf, params string[])</c>
    /// overload to map a plain string to a styled cell. The lambda /
    /// <c>Cell</c>-descriptor paths set role + tweaks directly and don't
    /// consult this.
    /// </summary>
    public enum CellRole
    {
        Data,
        Header,
        Label,
        Spacer,
    }



    public class TableCell
    {
        /// <summary>
        /// FPDF-style border string ("1", "0", "BTLR" subset). Kept for
        /// back-compat with the legacy <c>PrintRow / PrintRow2</c> paths;
        /// new code should use <see cref="Borders"/> via the
        /// <see cref="Border(Borders)"/> fluent method instead.
        /// </summary>
        public string Border { get; set; } = "1";
        public double Width { get; set; }
        public AlignEnum Align { get; set; } = AlignEnum.Left;
        public Color Background { get; set; } = Color.Transparent;
        public bool Fill => Background != Color.Transparent && Background != Color.TransparentBlack;
        public double? CellHeight { get; set; }
        public StatusEnum Status { get; set; }
        public bool Bold { get; set; }
        /// <summary>
        /// Cell content for the fluent <c>Render</c> path. The legacy
        /// <c>PrintRow2(Func&lt;TableCell, string&gt;)</c> route pulls text
        /// from the lambda's return value; new code passes
        /// <c>Action&lt;TableCell&gt;</c> and the action calls
        /// <see cref="Text(string)"/> / <see cref="Header(string)"/> etc.,
        /// which stash the content here.
        /// </summary>
        public string Content { get; set; } = "";

        /// <summary>
        /// Role assigned by <see cref="TableRow.Roles(CellRole[])"/>. Read
        /// by the positional <c>Render(pdf, params string[])</c> overload
        /// to decide whether the plain string should be wrapped in
        /// <see cref="Header(string)"/> / <see cref="Label(string)"/> etc.
        /// before drawing.
        /// </summary>
        public CellRole Role { get; set; } = CellRole.Data;

        public TableCell()
        {

        }

        public TableCell(double width) : this()
        {
            this.Width = width;
        }

        // ---- Fluent builders (return this so the caller can chain) ----

        /// <summary>Just set the content. No role styling applied.</summary>
        public TableCell Text(string s) { Content = s ?? ""; return this; }

        /// <summary>Bold on (or off when <paramref name="bold"/> = false).</summary>
        public TableCell Bolded(bool bold = true) { Bold = bold; return this; }

        /// <summary>Set the horizontal alignment.</summary>
        public TableCell Aligned(AlignEnum align) { Align = align; return this; }

        /// <summary>Set the border flags. Translates to the legacy <see cref="Border"/> string for the renderer.</summary>
        public TableCell Bordered(Borders b) { Border = b.ToFpdfBorderString(); return this; }

        /// <summary>Set the background fill colour.</summary>
        public TableCell BackgroundColor(Color c) { Background = c; return this; }

        // ---- Role presets (set content + the common style combo) ----

        /// <summary>Bold + centred + content. Default border kept (= "1" / all).</summary>
        public TableCell Header(string text) { Bold = true; Align = AlignEnum.Center; Content = text ?? ""; return this; }

        /// <summary>Bold + left-aligned + content. Default border kept.</summary>
        public TableCell Label(string text)  { Bold = true; Align = AlignEnum.Left;   Content = text ?? ""; return this; }

        /// <summary>Plain content, no style change.</summary>
        public TableCell Data(string value)  { Content = value ?? ""; return this; }

        /// <summary>Empty content, border off — used as a left-margin slot.</summary>
        public TableCell Spacer()            { Border = "0"; Content = ""; return this; }
    }

    public enum StatusEnum
    {
        Measure,
        Draw
    }
}
