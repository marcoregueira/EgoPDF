using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ego.PdfCore.Support
{
    public class JpgParser
    {

        public static Dimensions GetJpegDimensions(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                return GetJpegDimensions(ms);
            }
        }
        public static Dimensions GetJpegDimensions(string filePath)
        {
            using (var fs = File.OpenRead(filePath))
            {
                return GetJpegDimensions(fs);
            }
        }
        public static Dimensions GetJpegDimensions(Stream fs)
        {
            if (!fs.CanSeek) throw new ArgumentException("Stream must be seekable");
            var buf = new byte[4];
            fs.Read(buf, 0, 4);
            if (buf.SequenceEqual(new byte[] { 0xff, 0xd8, 0xff, 0xe1 }))
            {
                var blockStart = fs.Position;
                fs.Read(buf, 0, 2);
                var blockLength = ((buf[0] << 8) + buf[1]);
                fs.Read(buf, 0, 4);
                if (Encoding.ASCII.GetString(buf, 0, 4).ToUpper() == "EXIF"
                    && fs.ReadByte() == 0)
                {
                    blockStart += blockLength;
                    while (blockStart < fs.Length)
                    {
                        fs.Position = blockStart;
                        fs.Read(buf, 0, 4);
                        blockLength = ((buf[2] << 8) + buf[3]);
                        if (blockLength >= 7 && buf[0] == 0xff && buf[1] == 0xc0)
                        {
                            fs.Position += 1;
                            fs.Read(buf, 0, 4);
                            var height = (buf[0] << 8) + buf[1];
                            var width = (buf[2] << 8) + buf[3];
                            return new Dimensions(width, height);
                        }
                        blockStart += blockLength + 2;
                    }
                }
            }

            if (buf.SequenceEqual(new byte[] { 0xff, 0xd8, 0xff, 0xe0 }))
            {
                var blockStart = fs.Position;
                fs.Read(buf, 0, 2);
                var blockLength = ((buf[0] << 8) + buf[1]);
                fs.Read(buf, 0, 4);
                if (Encoding.ASCII.GetString(buf, 0, 4) == "JFIF"
                    && fs.ReadByte() == 0)
                {
                    blockStart += blockLength;
                    while (blockStart < fs.Length)
                    {
                        fs.Position = blockStart;
                        fs.Read(buf, 0, 4);
                        blockLength = ((buf[2] << 8) + buf[3]);
                        if (blockLength >= 7 && buf[0] == 0xff && buf[1] == 0xc0)
                        {
                            fs.Position += 1;
                            fs.Read(buf, 0, 4);
                            var height = (buf[0] << 8) + buf[1];
                            var width = (buf[2] << 8) + buf[3];
                            return new Dimensions(width, height);
                        }
                        blockStart += blockLength + 2;
                    }
                }
            }
            return null;
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
