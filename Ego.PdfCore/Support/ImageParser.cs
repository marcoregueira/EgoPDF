using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Ego.PDF.Data;
using SkiaSharp;

namespace Ego.PDF.Support
{
    /// <summary>
    /// Decodes raster images into the <see cref="ImageInfo"/> shape consumed by
    /// <c>FPdf.PutImage</c>. JPEG is passed straight through with /DCTDecode;
    /// PNG and GIF are decoded with SkiaSharp into raw RGB (plus an optional
    /// 8-bit alpha SMask) and re-compressed with zlib so the resulting PDF
    /// /FlateDecode stream is RFC 1950 compliant.
    /// </summary>
    internal static class ImageParser
    {
        public static ImageInfo ParseJpg(string file)
        {
            var dimensions = JpgParser.GetJpegDimensions(file);
            return new ImageInfo
            {
                w = dimensions.Width,
                h = dimensions.Height,
                cs = "DeviceRGB",
                bpc = 8,
                f = "DCTDecode",
                data = new List<byte[]> { File.ReadAllBytes(file) },
            };
        }

        public static ImageInfo ParsePng(string file) => DecodeBitmap(file);

        public static ImageInfo ParseGif(string file) => DecodeBitmap(file);

        private static ImageInfo DecodeBitmap(string file)
        {
            using var bitmap = SKBitmap.Decode(file)
                ?? throw new InvalidOperationException("Unable to decode image: " + file);

            var width = bitmap.Width;
            var height = bitmap.Height;
            var pixelCount = width * height;

            var rgb = new byte[pixelCount * 3];
            byte[] alpha = null;

            var pixels = bitmap.Pixels;
            for (int i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                rgb[i * 3 + 0] = c.Red;
                rgb[i * 3 + 1] = c.Green;
                rgb[i * 3 + 2] = c.Blue;
                if (c.Alpha != 0xFF)
                {
                    alpha ??= NewOpaqueAlpha(pixelCount);
                    alpha[i] = c.Alpha;
                }
            }

            var info = new ImageInfo
            {
                w = width,
                h = height,
                cs = "DeviceRGB",
                bpc = 8,
                f = "FlateDecode",
                data = new List<byte[]> { ZlibCompress(rgb) },
            };
            if (alpha != null)
            {
                info.smask = ZlibCompress(alpha);
            }
            return info;
        }

        private static byte[] NewOpaqueAlpha(int length)
        {
            var buffer = new byte[length];
            for (int i = 0; i < length; i++) buffer[i] = 0xFF;
            return buffer;
        }

        private static byte[] ZlibCompress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
    }
}
