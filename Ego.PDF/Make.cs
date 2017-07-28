using System;
using System.IO;
using Ego.PDF.PHP;

namespace Ego.PDF
{
    public class Make
    {
        public virtual void Message(object txt, bool severity)
        {

            if (severity)
            {
                Console.Write(severity + ": ");
            }
            Console.Write(txt + "\n");
        }

        public virtual void Notice(string txt)
        {
            Message(txt, PHP.TypeSupport.ToBoolean("Notice"));
        }
        public virtual void Warning(object txt)
        {
            Message(txt, PHP.TypeSupport.ToBoolean("Warning"));
        }
        public virtual void Error(string txt)
        {
            Message(txt, PHP.TypeSupport.ToBoolean("Error"));
            throw new Exception(txt);
        }
        public virtual PHP.OrderedMap LoadMap(string enc)
        {
            //TODO INTEGRATE THIS PROPERLY
            var path = "C:\\Users\\MarcoAntonio\\Downloads\\fpdf18\\makefont";
            var file = Path.Combine(path, enc.ToLower() + ".map");
            //CONVERSION_WARNING: Method 'file' was converted to 'PHP.FileSystemSupport.FileToArray' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/file.htm 
            var a = PHP.FileSystemSupport.FileToArray(file);
            if (PHP.VariableSupport.Empty(a))
            {
                Error("Encoding not found: " + enc);
            }
            var map = PHP.OrderedMap.Fill(0, 256, new PHP.OrderedMap(new object[] { "uv", -1 }, new object[] { "name", ".notdef" }));
            foreach (object line in a.Values)
            {
                //CONVERSION_WARNING: Method 'explode' was converted to 'System.String.Split' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/explode.htm 
                var e = new PHP.OrderedMap(PHP.TypeSupport.ToString(line).TrimEnd(new char[] { ' ', '\t', '\n', '\r', '0' }).Split(" ".ToCharArray()));
                //CONVERSION_WARNING: Method 'substr' was converted to 'System.String.Substring' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/substr.htm 
                var c = System.Convert.ToInt32(e[0].ToString().Substring(1), 16);
                //CONVERSION_WARNING: Method 'substr' was converted to 'System.String.Substring' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/substr.htm 
                var uv = System.Convert.ToInt32(e[1].ToString().Substring(2), 16);
                var name = PHP.TypeSupport.ToString(e[2]);
                map[c] = new PHP.OrderedMap(new object[] { "uv", uv }, new object[] { "name", name });
            }

            return map;
        }
        public virtual PHP.OrderedMap GetInfoFromTrueType(string file, bool embed, PHP.OrderedMap map)
        {
            // Return informations from a TrueType font
            TtfParser ttf;
            PHP.OrderedMap info = new PHP.OrderedMap();
            double k;
            PHP.OrderedMap widths = new PHP.OrderedMap();
            int c;
            int uv;
            int w;
            ttf = new TtfParser();
            ttf.Parse(file);
            if (embed)
            {
                if (!ttf.Embeddable)
                {
                    Error("Font license does not allow embedding");
                }
                //CONVERSION_WARNING: Method 'file_get_contents' was converted to 'PHP.FileSystemSupport.ReadContents' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/file_get_contents.htm 
                info["Data"] = PHP.FileSystemSupport.ReadContents(file);
                info["OriginalSize"] = new System.IO.FileInfo(file).Length;
            }
            k = 1000 / PHP.TypeSupport.ToDouble(ttf.unitsPerEm);
            info["FontName"] = ttf.postScriptName;
            info["Bold"] = ttf.Bold;
            info["ItalicAngle"] = ttf.italicAngle;
            info["IsFixedPitch"] = ttf.isFixedPitch;
            info["Ascender"] = System.Math.Round(k * ttf.typoAscender);
            info["Descender"] = System.Math.Round(k * ttf.typoDescender);
            info["UnderlineThickness"] = System.Math.Round(k * ttf.underlineThickness);
            info["UnderlinePosition"] = System.Math.Round(k * ttf.underlinePosition);
            info["FontBBox"] = new PHP.OrderedMap(System.Math.Round(k * ttf.xMin), System.Math.Round(k * ttf.yMin), System.Math.Round(k * ttf.xMax), System.Math.Round(k * ttf.yMax));
            info["CapHeight"] = System.Math.Round(k * ttf.capHeight);
            info["MissingWidth"] = System.Math.Round(k * PHP.TypeSupport.ToDouble(ttf.widths[0]));
            widths = PHP.OrderedMap.Fill(0, 256, info["MissingWidth"]);
            for (c = 0; c <= 255; c++)
            {
                if (PHP.TypeSupport.ToString(map.GetValue(c, "name")) != ".notdef")
                {
                    //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
                    uv = PHP.TypeSupport.ToInt32(map.GetValue(c, "uv"));
                    //CONVERSION_WARNING: Method 'isset' was converted to '!=' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/isset.htm 
                    if (ttf.chars[uv] != null)
                    {
                        //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
                        w = PHP.TypeSupport.ToInt32(ttf.widths[ttf.chars[uv]]);
                        widths[c] = System.Math.Round(k * w);
                    }
                    else
                    {
                        Warning("Character " + PHP.TypeSupport.ToString(map.GetValue(c, "name")) + " is missing");
                    }
                }
            }
            info["Widths"] = widths;
            return info;
        }
        //public virtual PHP.OrderedMap GetInfoFromType1(string file, bool embed, PHP.OrderedMap map)
        //{
        //    // Return informations from a Type1 font
        //    object f;
        //    PHP.OrderedMap a;
        //    int size1;
        //    string data;
        //    int size2;
        //    PHP.OrderedMap info = new PHP.OrderedMap();
        //    string afm;
        //    PHP.OrderedMap e;
        //    string entry;
        //    string w;
        //    string name;
        //    PHP.OrderedMap cw = new PHP.OrderedMap();
        //    PHP.OrderedMap widths = new PHP.OrderedMap();
        //    int c;
        //    if (embed)
        //    {
        //        //CONVERSION_WARNING: Method 'fopen' was converted to 'PHP.FileSystemSupport.FileOpen' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fopen.htm 
        //        f = PHP.FileSystemSupport.FileOpen(file, "rb");
        //        if (!PHP.TypeSupport.ToBoolean(f))
        //        {
        //            Error("Can\'t open font file");
        //        }
        //        // Read first segment
        //        //CONVERSION_ISSUE: Method 'unpack' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
        //        //CONVERSION_WARNING: Method 'fread' was converted to 'PHP.FileSystemSupport.FileOpen' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fread.htm 
        //        a = unpack("Cmarker/Ctype/Vsize", PHP.FileSystemSupport.Read(f, 6));
        //        //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
        //        if (PHP.TypeSupport.ToInt32(a["marker"]) != 128)
        //        {
        //            Error("Font file is not a valid binary Type1");
        //        }
        //        //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
        //        size1 = PHP.TypeSupport.ToInt32(a["size"]);
        //        //CONVERSION_WARNING: Method 'fread' was converted to 'PHP.FileSystemSupport.FileOpen' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fread.htm 
        //        data = PHP.FileSystemSupport.Read(f, size1);
        //        // Read second segment
        //        //CONVERSION_ISSUE: Method 'unpack' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
        //        //CONVERSION_WARNING: Method 'fread' was converted to 'PHP.FileSystemSupport.FileOpen' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fread.htm 
        //        a = unpack("Cmarker/Ctype/Vsize", PHP.FileSystemSupport.Read(f, 6));
        //        //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
        //        if (PHP.TypeSupport.ToInt32(a["marker"]) != 128)
        //        {
        //            Error("Font file is not a valid binary Type1");
        //        }
        //        //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
        //        size2 = PHP.TypeSupport.ToInt32(a["size"]);
        //        //CONVERSION_WARNING: Method 'fread' was converted to 'PHP.FileSystemSupport.FileOpen' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fread.htm 
        //        data += PHP.FileSystemSupport.Read(f, size2);
        //        //CONVERSION_WARNING: Method 'fclose' was converted to 'PHP.FileSystemSupport.Close' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fclose.htm 
        //        PHP.FileSystemSupport.Close(f);
        //        info["Data"] = data;
        //        info["Size1"] = size1;
        //        info["Size2"] = size2;
        //    }

        //    //CONVERSION_WARNING: Method 'substr' was converted to 'System.String.Substring' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/substr.htm 
        //    afm = file.Substring(0, -3) + "afm";
        //    if (!(System.IO.File.Exists(afm) || System.IO.Directory.Exists(afm)))
        //    {
        //        Error("AFM font file not found: " + afm);
        //    }
        //    //CONVERSION_WARNING: Method 'file' was converted to 'PHP.FileSystemSupport.FileToArray' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/file.htm 
        //    a = PHP.FileSystemSupport.FileToArray(afm);
        //    if (PHP.VariableSupport.Empty(a))
        //    {
        //        Error("AFM file empty or not readable");
        //    }
        //    foreach (object line in a.Values)
        //    {
        //        //CONVERSION_WARNING: Method 'explode' was converted to 'System.String.Split' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/explode.htm 
        //        e = new PHP.OrderedMap(PHP.TypeSupport.ToString(line).TrimEnd(new char[] { ' ', '\t', '\n', '\r', '0' }).Split(" ".ToCharArray()));
        //        if (PHP.OrderedMap.CountElements(e) < 2)
        //        {
        //            continue;
        //        }
        //        entry = PHP.TypeSupport.ToString(e[0]);
        //        if (entry == "C")
        //        {
        //            w = PHP.TypeSupport.ToString(e[4]);
        //            name = PHP.TypeSupport.ToString(e[7]);
        //            cw[name] = w;
        //        }
        //        else if (entry == "FontName")
        //        {
        //            info["FontName"] = e[1];
        //        }
        //        else if (entry == "Weight")
        //        {
        //            info["Weight"] = e[1];
        //        }
        //        else if (entry == "ItalicAngle")
        //        {
        //            //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
        //            info["ItalicAngle"] = PHP.TypeSupport.ToInt32(e[1]);
        //        }
        //        else if (entry == "Ascender")
        //        {
        //            //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
        //            info["Ascender"] = PHP.TypeSupport.ToInt32(e[1]);
        //        }
        //        else if (entry == "Descender")
        //        {
        //            //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
        //            info["Descender"] = PHP.TypeSupport.ToInt32(e[1]);
        //        }
        //        else if (entry == "UnderlineThickness")
        //        {
        //            //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
        //            info["UnderlineThickness"] = PHP.TypeSupport.ToInt32(e[1]);
        //        }
        //        else if (entry == "UnderlinePosition")
        //        {
        //            //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
        //            info["UnderlinePosition"] = PHP.TypeSupport.ToInt32(e[1]);
        //        }
        //        else if (entry == "IsFixedPitch")
        //        {
        //            info["IsFixedPitch"] = (PHP.TypeSupport.ToString(e[1]) == "true");
        //        }
        //        else if (entry == "FontBBox")
        //        {
        //            //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
        //            info["FontBBox"] = new PHP.OrderedMap(PHP.TypeSupport.ToInt32(e[1]), PHP.TypeSupport.ToInt32(e[2]), PHP.TypeSupport.ToInt32(e[3]), PHP.TypeSupport.ToInt32(e[4]));
        //        }
        //        else if (entry == "CapHeight")
        //        {
        //            //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
        //            info["CapHeight"] = PHP.TypeSupport.ToInt32(e[1]);
        //        }
        //        else if (entry == "StdVW")
        //        {
        //            //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
        //            info["StdVW"] = PHP.TypeSupport.ToInt32(e[1]);
        //        }
        //    }


        //    //CONVERSION_WARNING: Method 'isset' was converted to '!=' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/isset.htm 
        //    if (!(info["FontName"] != null))
        //    {
        //        Error("FontName missing in AFM file");
        //    }
        //    //CONVERSION_WARNING: Method 'isset' was converted to '!=' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/isset.htm 
        //    info["Bold"] = info["Weight"] != null && new System.Text.RegularExpressions.Regex("/bold|black/i").Match(info["Weight"].ToString()).Success;
        //    //CONVERSION_WARNING: Method 'isset' was converted to '!=' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/isset.htm 
        //    if (cw[".notdef"] != null)
        //    {
        //        info["MissingWidth"] = cw[".notdef"];
        //    }
        //    else
        //    {
        //        info["MissingWidth"] = 0;
        //    }
        //    widths = PHP.OrderedMap.Fill(0, 256, info["MissingWidth"]);
        //    for (c = 0; c <= 255; c++)
        //    {
        //        name = PHP.TypeSupport.ToString(map.GetValue(c, "name"));
        //        if (name != ".notdef")
        //        {
        //            //CONVERSION_WARNING: Method 'isset' was converted to '!=' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/isset.htm 
        //            if (cw[name] != null)
        //            {
        //                widths[c] = cw[name];
        //            }
        //            else
        //            {
        //                Warning("Character " + name + " is missing");
        //            }
        //        }
        //    }
        //    info["Widths"] = widths;
        //    return info;
        //}
        public virtual string MakeFontDescriptor(PHP.OrderedMap info)
        {
            // Ascent
            string fd;
            double flags;
            PHP.OrderedMap fbb;
            int stemv;
            fd = "array('Ascent'=>" + PHP.TypeSupport.ToString(info["Ascender"]);
            // Descent
            fd += ",'Descent'=>" + PHP.TypeSupport.ToString(info["Descender"]);
            // CapHeight
            if (!PHP.VariableSupport.Empty(info["CapHeight"]))
            {
                fd += ",'CapHeight'=>" + PHP.TypeSupport.ToString(info["CapHeight"]);
            }
            else
            {
                fd += ",'CapHeight'=>" + PHP.TypeSupport.ToString(info["Ascender"]);
            }
            // Flags
            flags = 0;
            if (PHP.TypeSupport.ToBoolean(info["IsFixedPitch"]))
            {
                flags += 1 << 0;
            }
            flags += 1 << 5;
            //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
            if (PHP.TypeSupport.ToInt32(info["ItalicAngle"]) != 0)
            {
                flags += 1 << 6;
            }
            fd += ",'Flags'=>" + flags.ToString();
            // FontBBox
            fbb = PHP.TypeSupport.ToArray(info["FontBBox"]);
            fd += ",'FontBBox'=>'[" + PHP.TypeSupport.ToString(fbb[0]) + " " + PHP.TypeSupport.ToString(fbb[1]) + " " + PHP.TypeSupport.ToString(fbb[2]) + " " + PHP.TypeSupport.ToString(fbb[3]) + "]'";
            // ItalicAngle
            fd += ",'ItalicAngle'=>" + PHP.TypeSupport.ToString(info["ItalicAngle"]);
            // StemV
            //CONVERSION_WARNING: Method 'isset' was converted to '!=' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/isset.htm 
            if (info["StdVW"] != null)
            {
                //CONVERSION_TODO: The equivalent in .NET for converting to integer may return a different value. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1007.htm 
                stemv = PHP.TypeSupport.ToInt32(info["StdVW"]);
            }
            else if (PHP.TypeSupport.ToBoolean(info["Bold"]))
            {
                stemv = 120;
            }
            else
            {
                stemv = 70;
            }
            fd += ",'StemV'=>" + stemv.ToString();
            // MissingWidth
            fd += ",'MissingWidth'=>" + PHP.TypeSupport.ToString(info["MissingWidth"]) + ")";
            return fd;
        }
        public virtual string MakeWidthArray(PHP.OrderedMap widths)
        {
            string s;
            int c;
            s = "array(\n\t";
            for (c = 0; c <= 255; c++)
            {
                if (System.Convert.ToString((char)c) == "'")
                {
                    s += "'\\''";
                }
                else if (System.Convert.ToString((char)c) == "\\")
                {
                    s += "'\\\\'";
                }
                else if (c >= 32 && c <= 126)
                {
                    s += "'" + System.Convert.ToString((char)c) + "'";
                }
                else
                {
                    s += "chr(" + c + ")";
                }
                s += "=>" + PHP.TypeSupport.ToString(widths[c]);
                if (c < 255)
                {
                    s += ",";
                }
                if ((c + 1) % 22 == 0)
                {
                    s += "\n\t";
                }
            }
            s += ")";
            return s;
        }
        public virtual string MakeFontEncoding(PHP.OrderedMap map)
        {
            // Build differences from reference encoding
            PHP.OrderedMap ref_Renamed;
            string s;
            int last;
            ref_Renamed = LoadMap("cp1252");
            s = "";
            last = 0;
            //CONVERSION_ISSUE: Incrementing/decrementing only supported on numerical types, '++' was not converted. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1000.htm 
            for (var c = 32; c <= 255; c++)
            {
                //CONVERSION_WARNING: Converted Operator might not behave as expected. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/1009.htm 
                if (map.GetValue(c, "name") != ref_Renamed.GetValue(c, "name"))
                {
                    if (PHP.TypeSupport.ToInt32(c) != PHP.TypeSupport.ToDouble(last) + 1)
                    {
                        s += c + " ";
                    }
                    last = c;
                    s += "/" + PHP.TypeSupport.ToString(map.GetValue(c, "name")) + " ";
                }
            }
            return s.TrimEnd(new char[] { ' ', '\t', '\n', '\r', '0' });
        }
        public virtual void SaveToFile(string file, string s, string mode)
        {
            //CONVERSION_WARNING: Method 'fopen' was converted to 'PHP.FileSystemSupport.FileOpen' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fopen.htm 
            var f = PHP.FileSystemSupport.FileOpen(file, "w" + mode);
            if (!PHP.TypeSupport.ToBoolean(f))
            {
                Error("Can\'t write to file " + file);
            }
            //CONVERSION_WARNING: Method 'fwrite' was converted to 'PHPFileSystemSupport.Write' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fwrite.htm 
            PHP.FileSystemSupport.Write(f, s, s.Length);
            //CONVERSION_WARNING: Method 'fclose' was converted to 'PHP.FileSystemSupport.Close' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/fclose.htm 
            PHP.FileSystemSupport.Close(f);
        }
        public virtual void MakeDefinitionFilePhp(object file, string type, string enc, bool embed, object map, PHP.OrderedMap info)
        {
            string s;
            string diff;
            s = "<?php\n";
            s += "$type = \'" + type + "';\n";
            s += "$name = \'" + PHP.TypeSupport.ToString(info["FontName"]) + "';\n";
            s += "$desc = " + MakeFontDescriptor(info) + ";\n";
            s += "$up = " + PHP.TypeSupport.ToString(info["UnderlinePosition"]) + ";\n";
            s += "$ut = " + PHP.TypeSupport.ToString(info["UnderlineThickness"]) + ";\n";
            s += "$cw = " + MakeWidthArray(PHP.TypeSupport.ToArray(info["Widths"])) + ";\n";
            s += "$enc = \'" + enc + "';\n";
            diff = MakeFontEncoding(PHP.TypeSupport.ToArray(map));
            if (PHP.TypeSupport.ToBoolean(diff))
            {
                s += "$diff = \'" + diff + "';\n";
            }
            if (embed)
            {
                s += "$file = \'" + PHP.TypeSupport.ToString(info["File"]) + "';\n";
                if (type == "Type1")
                {
                    s += "$size1 = " + PHP.TypeSupport.ToString(info["Size1"]) + ";\n";
                    s += "$size2 = " + PHP.TypeSupport.ToString(info["Size2"]) + ";\n";
                }
                else
                {
                    s += "$originalsize = " + PHP.TypeSupport.ToString(info["OriginalSize"]) + ";\n";
                }
            }
            s += "?>\n";
            SaveToFile(PHP.TypeSupport.ToString(file), s, "t");
        }

        public virtual void MakeDefinitionFile(object file, string type, string enc, bool embed, object map, PHP.OrderedMap info)
        {
            string s;
            string diff;
            s = "<?php\n";
            s += "$type = \'" + type + "';\n";
            s += "$name = \'" + PHP.TypeSupport.ToString(info["FontName"]) + "';\n";
            s += "$desc = " + MakeFontDescriptor(info) + ";\n";
            s += "$up = " + PHP.TypeSupport.ToString(info["UnderlinePosition"]) + ";\n";
            s += "$ut = " + PHP.TypeSupport.ToString(info["UnderlineThickness"]) + ";\n";
            s += "$cw = " + MakeWidthArray(PHP.TypeSupport.ToArray(info["Widths"])) + ";\n";
            s += "$enc = \'" + enc + "';\n";
            diff = MakeFontEncoding(PHP.TypeSupport.ToArray(map));
            if (PHP.TypeSupport.ToBoolean(diff))
            {
                s += "$diff = \'" + diff + "';\n";
            }
            if (embed)
            {
                s += "$file = \'" + PHP.TypeSupport.ToString(info["File"]) + "';\n";
                if (type == "Type1")
                {
                    s += "$size1 = " + PHP.TypeSupport.ToString(info["Size1"]) + ";\n";
                    s += "$size2 = " + PHP.TypeSupport.ToString(info["Size2"]) + ";\n";
                }
                else
                {
                    s += "$originalsize = " + PHP.TypeSupport.ToString(info["OriginalSize"]) + ";\n";
                }
            }
            s += "?>\n";
            SaveToFile(PHP.TypeSupport.ToString(file), s, "t");
        }

        public virtual void MakeFont(string fontfile, string enc = "cp1252", bool embed = false)
        {
            // Generate a font definition file
            string ext;
            string type = null;
            object map;
            PHP.OrderedMap info = new PHP.OrderedMap();
            string basename;

            if (!(System.IO.File.Exists(fontfile) || System.IO.Directory.Exists(fontfile)))
            {
                Error("Font file not found: " + fontfile);
            }
            //CONVERSION_WARNING: Method 'substr' was converted to 'System.String.Substring' which has a different behavior. Copy this link in your browser for more info: ms-its:C:\Program Files\Microsoft Corporation\PHP to ASP.NET Migration Assistant\PHPToAspNet.chm::/substr.htm 
            ext = Path.GetExtension(fontfile).Replace(".", "");
            if (ext == "ttf" || ext == "otf")
            {
                type = "TrueType";
            }
            else if (ext == "pfb")
            {
                type = "Type1";
            }
            else
            {
                Error("Unrecognized font file extension: " + ext);
            }

            map = LoadMap(enc);

            if (type == "TrueType")
            {
                info = GetInfoFromTrueType(fontfile, embed, PHP.TypeSupport.ToArray(map));
            }
            else
            {
                //TODO UNSUPPORTED BY NOW
                Error("UNSUPPORTED BY NOW: " + ext);
                //info = GetInfoFromType1(fontfile, embed, PHP.TypeSupport.ToArray(map));
            }

            basename = Path.GetFileNameWithoutExtension(fontfile);

            //TODO PRIORITARY, EMBED IS MISSING, IMPLEMENT USING GZIP STREAM
            if (embed)
            {
                throw new NotImplementedException();
                //file = basename + ".z";
                //SaveToFile(file, gzcompress(PHP.TypeSupport.ToString(info["Data"])), "b");
                //info["File"] = file;
                //Message("Font file compressed: " + file, PHP.TypeSupport.ToBoolean(""));
            }

            MakeDefinitionFile(basename + ".php", type, enc, embed, map, info);
            Message("Font definition file generated: " + basename + ".php", PHP.TypeSupport.ToBoolean(""));
        }
    }
}
