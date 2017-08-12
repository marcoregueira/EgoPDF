using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ego.PDF.Data
{
    public class DrawingPoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        public DrawingPoint(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}
