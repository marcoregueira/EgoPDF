
using System;
using System.Collections.Generic;

using Ego.PDF;
using Ego.PDF.Data;
using Ego.PDF.PHP;

namespace Ego.PDF.Font
{
    public class FontDefinition
    {
        public string enc = "cp1252";
        public Dictionary<int, object> uv = new Dictionary<int, object>();
        public FontTypeEnum FontType { get; set; }
        public string Name { get; set; }
        public int up { get; set; }
        public int ut { get; set; }
        public int i { get; set; }

        //file information
        public int n { get; set; }
        public int length1 { get; set; }
        public int length2 { get; set; }

        public string diff { get; set; }
        public int? diffn { get; set; }

        public string FontFile { get; set; }

        public int size1 { get; set; }
        public int size2 { get; set; }
        public int originalsize { get; set; }

        public Dictionary<string, double> Widths { get; set; }
        public Dictionary<string, string> desc { get; set; }
        public bool Subsetted { get; set; }

        public FontDefinition()
        {
            this.Widths = new Dictionary<string, double>();
            this.desc = new Dictionary<string, string>();
        }
    }
}