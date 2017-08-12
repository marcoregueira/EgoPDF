using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ego.PDF.Data
{

    public class ImageInfo
    {
        public int n { get; set; }
        public int w { get; set; }
        public int h { get; set; }
        /// <summary>
        /// Indexed, DeviceCMYK
        /// </summary>
        public string cs { get; set; }

        /// <summary>
        /// Bits per component
        /// </summary>
        public int bpc { get; set; }
        /// <summary>
        /// Filter: DCTDecode, FlateDecode
        /// </summary>
        public string f { get; set; }
        /// <summary>
        /// predictor
        /// </summary>
        public string dp { get; set; }

        public byte[] pal { get; set; }

        public int[] trns { get; set; }

        /// <summary>
        /// gzipped strema o algo así
        /// </summary>
        public byte[] smask { get; set; }

        public List<byte[]> data { get; set; }

        public int i { get; set; }

        public ImageInfo()
        {
            this.trns = new int[] { };
            this.data = new List<byte[]>();
        }
    }
}
