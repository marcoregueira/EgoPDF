using Ego.PDF.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ego.PdfCore.NewFont
{
    public class FontData
    {
        public FontTypeEnum FontType { get; set; }
        public string Name { get; set; }
        public int Up { get; set; } = -100;
        public int Ut { get; set; } = 50;
        public Dictionary<ushort, AdvanceWidth> FontWidths { get; set; } = new Dictionary<ushort, AdvanceWidth>();

        public class AdvanceWidth
        {
            public double Advance { get; set; }
            public double RightBearing { get; set; }
            public double LeftBearing { get; set; }
            public ushort Point { get; set; }
        }
    }
}
