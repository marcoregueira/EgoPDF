using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ego.PDF.Data
{
    public class Page
    {
        public StringBuilder Text { get; set; }
        public PageSize Size { get; set; }
        public List<PageLink> PageLinks { get; set; }

        public Page()
            : base()
        {
            Text = new StringBuilder();
            PageLinks = new List<PageLink>();
        }

        public void Append(string text)
        {
            Text.Append(text);
        }

        public void Replace(string oldValue, string newValue)
        {
            Text.Replace(oldValue, newValue);
        }

        public override string ToString()
        {
            return Text.ToString();
        }
    }
}
