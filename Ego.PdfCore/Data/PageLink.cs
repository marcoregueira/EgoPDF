using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ego.PDF.Data
{
    public enum LynkTypeEnum
    {
        Uri,
        Position
    }

    public class PageLink
    {
        public double P0 { get; set; }
        public double P1 { get; set; }
        public double P2 { get; set; }
        public double P3 { get; set; }
        public int P4 { get; set; }
        public LinkData Link { get; set; }

        public PageLink()
        {
        }

        internal PageLink(double p0, double p1, double p2, double p3, LinkData link)
            : this()
        {
            this.P0 = p0;
            this.P1 = p1;
            this.P2 = p2;
            this.P3 = p3;
            this.Link = link;
        }
    }


    public class LinkData
    {
    }

    public class LinkDataInternal : LinkData
    {
        public int InternalLink { get; set; }
        public int PageIndex { get; set; }
        public double Y { get; set; }

        public LinkDataInternal()
        {
        }

        public LinkDataInternal(int pageNumber, double y)
        {
            this.PageIndex = pageNumber;
            this.Y = y;
        }
    }

    public class LinkDataUri : LinkData
    {
        public string Uri { get; set; }
        public LinkDataUri(string uri)
        {
            this.Uri = uri;
        }
    }
}
