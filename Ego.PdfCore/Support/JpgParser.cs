using SkiaSharp;
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
            using (var codec = SKCodec.Create(fs))
            {
                var format = codec.EncodedFormat;
                var dimensions = codec.GetScaledDimensions(1);
                return new Dimensions(dimensions.Width, dimensions.Height);
            }
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
