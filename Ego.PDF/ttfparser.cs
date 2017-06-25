
//<%
//	 /*******************************************************************************
//	* Utility to parse TTF font files                                              *
//	*                                                                              *
//	* Version: 1.0                                                                 *
//	* Date:    2011-06-18                                                          *
//	* Author:  Olivier PLATHEY                                                     *
//	*******************************************************************************/


//%>

using System;
using System.IO;

namespace Ego.PDF
{
    public class TtfParser
    {
        public BinaryReader f;
        public PHP.OrderedMap tables = new PHP.OrderedMap();
        public uint unitsPerEm;
        public double xMin;
        public double yMin;
        public double xMax;
        public double yMax;
        public uint numberOfHMetrics;
        public uint numGlyphs;
        public PHP.OrderedMap widths = new PHP.OrderedMap();
        public PHP.OrderedMap chars = new PHP.OrderedMap();
        public string postScriptName;
        public bool Embeddable;
        public bool Bold;
        public double typoAscender;
        public double typoDescender;
        public double capHeight;
        public double italicAngle;
        public double underlinePosition;
        public double underlineThickness;
        public bool isFixedPitch;

        public virtual void Parse(string file)
        {
            string version;
            uint numTables;
            int i;
            string tag;
            ulong offset;
            //CONVERSION_WARNING: Method 'fopen' was converted to 'PHP.FileSystemSupport.FileOpen' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fopen.htm 
            var stream = PHP.FileSystemSupport.FileOpen(file, "rb");
            this.f = new BinaryReader(stream);
            if (!PHP.TypeSupport.ToBoolean(this.f))
            {
                this.Error("Can\'t open file: " + file);
            }

            version = this.Read(4);
            if (version == "OTTO")
            {
                this.Error("OpenType fonts based on PostScript outlines are not supported");
            }
            if (version != "\x00\x01\x00\x00")
            {
                this.Error("Unrecognized file format");
            }
            numTables = this.ReadUShort();
            this.Skip(3 * 2); // searchRange, entrySelector, rangeShift
            this.tables = new PHP.OrderedMap();
            //CONVERSION_ISSUE: Incrementing/decrementing only supported on numerical types, '++' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
            for (i = 0; i.CompareTo(numTables) < 0; i++)
            {
                tag = this.Read(4);
                this.Skip(4); // checkSum
                offset = this.ReadULong();
                this.Skip(4); // length
                this.tables[tag] = offset;
            }

            this.ParseHead();
            this.ParseHhea();
            this.ParseMaxp();
            this.ParseHmtx();
            this.ParseCmap();
            this.ParseName();
            this.ParseOS2();
            this.ParsePost();

            //CONVERSION_WARNING: Method 'fclose' was converted to 'PHP.FileSystemSupport.Close' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fclose.htm 
            f.BaseStream.Close();
        }

        public virtual void ParseHead()
        {
            double magicNumber;
            this.Seek("head");
            this.Skip(3 * 4); // version, fontRevision, checkSumAdjustment
            magicNumber = this.ReadULong();
            if (magicNumber != 0x5F0F3CF5)
            {
                this.Error("Incorrect magic number");
            }
            this.Skip(2); // flags
            this.unitsPerEm = this.ReadUShort();
            this.Skip(2 * 8); // created, modified
            this.xMin = this.ReadShort();
            this.yMin = this.ReadShort();
            this.xMax = this.ReadShort();
            this.yMax = this.ReadShort();
        }

        public virtual void ParseHhea()
        {
            this.Seek("hhea");
            this.Skip(4 + 15 * 2);
            this.numberOfHMetrics = this.ReadUShort();
        }

        public virtual void ParseMaxp()
        {
            this.Seek("maxp");
            this.Skip(4);
            this.numGlyphs = this.ReadUShort();
        }

        public virtual void ParseHmtx()
        {
            uint advanceWidth;
            object lastWidth;
            this.Seek("hmtx");
            this.widths = new PHP.OrderedMap();
            //CONVERSION_ISSUE: Incrementing/decrementing only supported on numerical types, '++' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
            for (uint i = 0; i < this.numberOfHMetrics; i++)
            {
                advanceWidth = this.ReadUShort();
                this.Skip(2); // lsb
                this.widths[i] = advanceWidth;
            }
            if (this.numberOfHMetrics.CompareTo(this.numGlyphs) < 0)
            {
                lastWidth = this.widths[PHP.TypeSupport.ToDouble(this.numberOfHMetrics) - 1];
                //TODO MARCO Convert.ToInt32(this.numGlyphs) ??
                this.widths = PHP.OrderedMap.Pad(this.widths, Convert.ToInt32(this.numGlyphs), lastWidth);
            }
        }

        public virtual void ParseCmap()
        {
            uint numTables;
            int i;
            long offset31;
            uint platformID;
            uint encodingID;
            long offset;
            var startCount = new PHP.OrderedMap();
            var endCount = new PHP.OrderedMap();
            var idDelta = new PHP.OrderedMap();
            var idRangeOffset = new PHP.OrderedMap();
            uint format;
            string segCount;
            int c1;
            int c2;
            int d;
            int ro;
            int c;
            int gid;
            this.Seek("cmap");
            this.Skip(2); // version
            numTables = this.ReadUShort();
            offset31 = 0;
            //CONVERSION_ISSUE: Incrementing/decrementing only supported on numerical types, '++' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
            for (i = 0; i.CompareTo(numTables) < 0; i++)
            {
                platformID = this.ReadUShort();
                encodingID = this.ReadUShort();
                //TODO MARCO
                //offset = this.ReadULong();
                offset = this.ReadLong();
                if (PHP.TypeSupport.ToInt32(platformID) == 3 && PHP.TypeSupport.ToInt32(encodingID) == 1)
                {
                    offset31 = offset;
                }
            }
            if (offset31 == 0)
            {
                this.Error("No Unicode encoding found");
            }

            startCount = new PHP.OrderedMap();
            endCount = new PHP.OrderedMap();
            idDelta = new PHP.OrderedMap();
            idRangeOffset = new PHP.OrderedMap();
            this.chars = new PHP.OrderedMap();
            //CONVERSION_WARNING: Method 'fseek' was converted to 'System.IO.FileStream.Seek' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fseek.htm 

            f.BaseStream.Seek(Convert.ToInt32(this.tables["cmap"]) + (int)offset31, SeekOrigin.Begin);
            format = this.ReadUShort();
            if (PHP.TypeSupport.ToInt32(format) != 4)
            {
                this.Error("Unexpected subtable format: " + format);
            }
            this.Skip(2 * 2); // length, language
            segCount = (PHP.TypeSupport.ToDouble(this.ReadUShort()) / 2).ToString();
            this.Skip(3 * 2); // searchRange, entrySelector, rangeShift
            //CONVERSION_ISSUE: Incrementing/decrementing only supported on numerical types, '++' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
            for (i = 0; i.CompareTo(segCount) < 0; i++)
                endCount[i] = this.ReadUShort();
            this.Skip(2); // reservedPad
            //CONVERSION_ISSUE: Incrementing/decrementing only supported on numerical types, '++' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
            for (i = 0; i.CompareTo(segCount) < 0; i++)
                startCount[i] = this.ReadUShort();
            //CONVERSION_ISSUE: Incrementing/decrementing only supported on numerical types, '++' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
            for (i = 0; i.CompareTo(segCount) < 0; i++)
                idDelta[i] = this.ReadShort();
            //CONVERSION_WARNING: Method 'ftell' was converted to 'System.IO.FileStream.Position' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/ftell.htm 
            offset = this.f.BaseStream.Position;
            //CONVERSION_ISSUE: Incrementing/decrementing only supported on numerical types, '++' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
            for (i = 0; i.CompareTo(segCount) < 0; i++)
                idRangeOffset[i] = this.ReadUShort();

            //CONVERSION_ISSUE: Incrementing/decrementing only supported on numerical types, '++' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
            for (i = 0; i.CompareTo(segCount) < 0; i++)
            {
                c1 = Convert.ToInt32(startCount[i]);
                c2 = Convert.ToInt32(endCount[i]);
                //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
                d = PHP.TypeSupport.ToInt32(idDelta[i]);
                //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
                ro = PHP.TypeSupport.ToInt32(idRangeOffset[i]);
                if (ro > 0)
                {
                    //CONVERSION_WARNING: Method 'fseek' was converted to 'System.IO.FileStream.Seek' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fseek.htm 
                    this.f.BaseStream.Seek(offset + 2 * i + ro, System.IO.SeekOrigin.Begin);
                }
                for (c = c1; c <= c2; c++)
                {
                    if (c == 0xFFFF)
                    {
                        break;
                    }
                    if (ro > 0)
                    {
                        gid = this.ReadShort();
                        if (gid.CompareTo(0.ToString()) > 0)
                        {
                            gid = gid + d;
                        }
                    }
                    else
                    {
                        gid = (c + d);
                    }

                    if (gid.CompareTo(0.ToString()) > 0)
                    {
                        this.chars[c] = gid;
                    }
                }
            }
        }

        public virtual void ParseName()
        {
            long tableOffset;
            uint count;
            uint stringOffset;
            int i;
            uint nameID;
            int length;
            uint offset;
            string s;
            this.Seek("name");
            //CONVERSION_WARNING: Method 'ftell' was converted to 'System.IO.FileStream.Position' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/ftell.htm 
            tableOffset = this.f.BaseStream.Position;
            this.postScriptName = "";
            this.Skip(2); // format
            count = this.ReadUShort();
            stringOffset = this.ReadUShort();
            //CONVERSION_ISSUE: Incrementing/decrementing only supported on numerical types, '++' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
            for (i = 0; i.CompareTo(count) < 0; i++)
            {
                this.Skip(3 * 2); // platformID, encodingID, languageID
                nameID = this.ReadUShort();
                length = this.ReadShort();
                //length = this.ReadUShort();
                offset = this.ReadUShort();
                if (PHP.TypeSupport.ToInt32(nameID) == 6)
                {
                    // PostScript name
                    //CONVERSION_WARNING: Method 'fseek' was converted to 'System.IO.FileStream.Seek' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fseek.htm 
                    this.f.BaseStream.Seek(tableOffset + stringOffset + offset, System.IO.SeekOrigin.Begin);
                    s = this.Read(length);
                    //CONVERSION_WARNING: Method 'str_replace' was converted to 'PHP.StringSupport.StringReplace' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/str_replace.htm 

                    //TODO: ??????????????????????!!!!!!!!!!!!
                    s = PHP.TypeSupport.ToString(s.Replace(System.Convert.ToString((char)0), ""));
                    //CONVERSION_TODO: The equivalent in .NET for preg_replace may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
                    //CONVERSION_TODO: Regular expression should be reviewed in order to make it .NET compliant. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1022.htm 
                    s = new System.Text.RegularExpressions.Regex("|[ \\[\\](){}<>/%]|").Replace(s, "");
                    this.postScriptName = s;
                    break;
                }
            }
            if (this.postScriptName == "")
            {
                this.Error("PostScript name not found");
            }
        }

        public virtual void ParseOS2()
        {
            uint version;
            uint fsType;
            uint fsSelection;
            this.Seek("OS/2");
            version = this.ReadUShort();
            this.Skip(3 * 2); // xAvgCharWidth, usWeightClass, usWidthClass
            fsType = this.ReadUShort();
            //CONVERSION_ISSUE: Bitwise Operator on string '&' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
            this.Embeddable = (PHP.TypeSupport.ToInt32(fsType) != 2) && (fsType & 0x200) == 0;
            this.Skip(11 * 2 + 10 + 4 * 4 + 4);
            fsSelection = this.ReadUShort();
            //CONVERSION_ISSUE: Bitwise Operator on string '&' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
            this.Bold = (fsSelection & 32) != 0;
            this.Skip(2 * 2); // usFirstCharIndex, usLastCharIndex
            this.typoAscender = this.ReadShort();
            this.typoDescender = this.ReadShort();
            if (version.CompareTo(2.ToString()) >= 0)
            {
                this.Skip(3 * 2 + 2 * 4 + 2);
                this.capHeight = this.ReadShort();
            }
            else
            {
                this.capHeight = 0;
            }
        }

        public virtual void ParsePost()
        {
            this.Seek("post");
            this.Skip(4); // version
            this.italicAngle = this.ReadShort();
            this.Skip(2); // Skip decimal part
            this.underlinePosition = this.ReadShort();
            this.underlineThickness = this.ReadShort();
            this.isFixedPitch = (this.ReadULong() != 0);
        }

        public virtual void Error(string msg)
        {
            throw new InvalidOperationException(msg);
        }

        public virtual void Seek(string tag)
        {
            //CONVERSION_WARNING: Method 'isset' was converted to '!=' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/isset.htm 
            if (!(this.tables[tag] != null))
            {
                this.Error("Table not found: " + tag);
            }
            //CONVERSION_WARNING: Method 'fseek' was converted to 'System.IO.FileStream.Seek' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fseek.htm 
            this.f.BaseStream.Seek(Convert.ToInt32(this.tables[tag]), System.IO.SeekOrigin.Begin);
        }

        public virtual void Skip(int n)
        {
            //CONVERSION_WARNING: Method 'fseek' was converted to 'System.IO.FileStream.Seek' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fseek.htm 
            this.f.BaseStream.Seek(n, System.IO.SeekOrigin.Current);
        }

        public virtual string Read(int n)
        {
            //CONVERSION_WARNING: Method 'fread' was converted to 'PHP.FileSystemSupport.FileOpen' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fread.htm 
            return PHP.FileSystemSupport.Read(this.f, n);
        }

        public virtual uint ReadUShort()
        {
            return f.ReadUInt32();
        }

        public virtual int ReadShort()
        {
            var v = f.ReadInt32();
            return v;
        }

        public virtual long ReadLong()
        {
            return f.ReadInt64();
        }
        public virtual ulong ReadULong()
        {
            return f.ReadUInt64();
        }
    }
}