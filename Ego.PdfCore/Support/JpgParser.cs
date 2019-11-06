using SixLabors.ImageSharp;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Ego.PDF.Support
{
    public class JpgParser
    {
        public static Dimensions GetJpegDimensions(string filePath)
        {
            using (var fs = File.OpenRead(filePath))
            {
                return GetJpegDimensions(fs);
            }
        }

        public static Dimensions GetJpegDimensions(Stream fs)
        {
            var info = Image.Identify(fs);
            return new Dimensions(info.Width, info.Height);
        }

        public class Dimensions
        {
            public Dimensions(int width, int height)
            {
                this.Width = width;
                this.Height = height;
            }
            public int Width { get; }

            public int Height { get; }

            public override string ToString()
            {
                return string.Format("width:{0}, height:{1}", Width, Height);
            }
        }
    }

}
