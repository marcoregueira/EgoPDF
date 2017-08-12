using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ego.PDF.Data
{
    public class PageSize
    {
        public string Name { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public PageSize(string name, double width, double height)
            : this()
        {
            this.Name = name;
            this.Width = width;
            this.Height = height;
        }

        public PageSize()
            : base()
        {
        }

        public Dimensions GetDimensions()
        {
            return new Dimensions() { Width = this.Width, Heigth = this.Height };
        }

        public Dimensions GetDimensions(double k)
        {
            return new Dimensions() { Width = this.Width /k , Heigth = this.Height / k };
        }
    }

    public class Dimensions
    {
        public double Width { get; set; }
        public double Heigth { get; set; }
    }
}