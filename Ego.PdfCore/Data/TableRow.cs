using Ego.PDF;
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
    }



    public class TableCell
    {
        public string Border { get; set; } = "1";
        public double Width { get; set; }
        public AlignEnum Align { get; set; } = AlignEnum.Left;
        public Color Background { get; set; } = Color.Transparent;
        public bool Fill => Background != Color.Transparent && Background != Color.TransparentBlack;
        public double? CellHeight { get; set; }
        public StatusEnum Status { get; set; }
        public bool Bold { get; set; }

        public TableCell()
        {

        }

        public TableCell(double width) : this()
        {
            this.Width = width;
        }
    }

    public enum StatusEnum
    {
        Measure,
        Draw
    }
}
