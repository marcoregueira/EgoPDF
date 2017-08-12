using System;
using System.IO;

namespace Ego.PDF.PHP
{
    /// <summary>
    /// Provides static methods related to file system functions.
    /// </summary>
    public class FileSystemSupport
    {
        /// <summary>
        /// Returns the base file name of the specified path.
        /// </summary>
        /// <param name="path">The full file path.</param>
        /// <param name="suffix">The suffix that will be removed from the result if the file ends with it.</param>
        /// <returns>Return the base file name of the specified path.</returns>
        public static string BaseName(string path, string suffix)
        {
            if (path.EndsWith(suffix))
                return System.IO.Path.GetFileNameWithoutExtension(path);
            else
                return System.IO.Path.GetFileName(path);
        }

        ///// <summary>
        ///// Gets the number of available bytes of the specified disk.
        ///// </summary>
        ///// <param name="disk">The disk to obtain the information for.<param>
        ///// <returns>Returns number of available bytes of the specified disk.</returns>
        //public static long GetDiskFreeSpace(string disk)
        //{
        //    long result = 0;
        //    try
        //    {
        //        string root = System.IO.Path.GetPathRoot(System.IO.Path.GetFullPath(disk));
        //        root = root.Replace(@"\", "");
        //        System.Management.ManagementObject theDisk =
        //            new System.Management.ManagementObject("win32_logicaldisk.deviceid=\"" + root + "\"");
        //        theDisk.Get();
        //        result = System.Convert.ToInt64(theDisk["FreeSpace"]);
        //    }
        //    catch
        //    {
        //    }
        //    return result;
        //}

        ///// <summary>
        ///// Gets total size of the specified disk.
        ///// </summary>
        ///// <param name="disk">The disk to obtain the information for.<param>
        ///// <returns>Returns total size of the specified disk.</returns>
        //public static long GetDiskSize(string disk)
        //{
        //    long result = 0;
        //    try
        //    {
        //        string root = System.IO.Path.GetPathRoot(System.IO.Path.GetFullPath(disk));
        //        root = root.Replace(@"\", "");
        //        System.Management.ManagementObject theDisk =
        //            new System.Management.ManagementObject("win32_logicaldisk.deviceid=\"" + root + "\"");
        //        theDisk.Get();
        //        result = System.Convert.ToInt64(theDisk["Size"]);
        //    }
        //    catch
        //    {
        //    }
        //    return result;
        //}

        /// <summary>
        /// Returns an OrderedMap with information about the specified path.
        /// </summary>
        /// <param name="path">The path to retrieve the information from.</param>
        /// <returns>Returns an OrderedMap with information about the specified path.</returns>
        public static OrderedMap PathInfo(string path)
        {
            OrderedMap pathInfo = null;
            try
            {
                pathInfo = new OrderedMap();
                pathInfo["dirname"] = System.IO.Path.GetDirectoryName(path);
                pathInfo["basename"] = System.IO.Path.GetFileName(path);
                pathInfo["extension"] = System.IO.Path.GetExtension(path);
            }
            catch
            {
            }
            return pathInfo;
        }

        /// <summary>
        /// Opens the specified file using the specified file mode and file access options.
        /// </summary>
        /// <param name="fileName">The name of the file to open.</param>
        /// <param name="options">The file mode and file access options.</param>
        /// <returns>Returns an opened System.IO.FileStream.</returns>
        /// <remarks>This function is not intented to work with URLs.</remarks>
        public static System.IO.FileStream FileOpen(string fileName, string options)
        {
            System.IO.FileStream file = null;
            try
            {
                System.IO.FileMode fileMode = System.IO.FileMode.Open;
                System.IO.FileAccess fileAccess = System.IO.FileAccess.Read;

                if (options.EndsWith("b") || options.EndsWith("t"))
                    options = options.Remove(options.Length - 1, 1);

                switch (options)
                {
                    case "r":
                        fileMode = System.IO.FileMode.Open;
                        fileAccess = System.IO.FileAccess.Read;
                        break;
                    case "r+":
                        fileMode = System.IO.FileMode.Open;
                        fileAccess = System.IO.FileAccess.ReadWrite;
                        break;
                    case "w":
                        if (!System.IO.File.Exists(fileName))
                        {
                            file = new System.IO.FileStream(fileName, System.IO.FileMode.CreateNew);
                            //file.Close();
                            file.Dispose();
                        }
                        fileMode = System.IO.FileMode.Truncate;
                        fileAccess = System.IO.FileAccess.Write;
                        break;
                    case "w+":
                        if (!System.IO.File.Exists(fileName))
                        {
                            file = new System.IO.FileStream(fileName, System.IO.FileMode.CreateNew);
                            file.Dispose();
                        }
                        fileMode = System.IO.FileMode.Truncate;
                        fileAccess = System.IO.FileAccess.Write;
                        break;
                    case "a":
                        if (!System.IO.File.Exists(fileName))
                        {
                            file = new System.IO.FileStream(fileName, System.IO.FileMode.CreateNew);
                            file.Dispose();
                        }
                        fileMode = System.IO.FileMode.Append;
                        fileAccess = System.IO.FileAccess.Write;
                        break;
                    case "a+":
                        if (!System.IO.File.Exists(fileName))
                        {
                            file = new System.IO.FileStream(fileName, System.IO.FileMode.CreateNew);
                            file.Dispose();
                        }
                        fileMode = System.IO.FileMode.Append;
                        fileAccess = System.IO.FileAccess.Write;
                        break;
                    default:
                        fileMode = System.IO.FileMode.Open;
                        fileAccess = System.IO.FileAccess.Read;
                        break;
                }
                file = new System.IO.FileStream(fileName, fileMode, fileAccess);
            }
            catch
            {
            }
            return file;
        }

        /// <summary>
        /// Reads a block of bytes from the file stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="length">The lenght of the data to be read.</param>
        /// <returns>Returns the string representation of the read block of bytes.</returns>
        public static string Read(System.IO.BinaryReader stream, long length)
        {
            int theLength = (int)length;
            string result = null;
            try
            {
                byte[] bytes = new byte[length];
                int readBytes = stream.Read(bytes, 0, theLength);
                if (readBytes > 0)
                    result = System.Text.Encoding.ASCII.GetString(bytes, 0, readBytes);
            }
            catch
            {
            }
            return result;
        }

        /// <summary>
        /// Reads a block of bytes from the file stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="length">The lenght of the data to be read.</param>
        /// <returns>Returns the string representation of the read block of bytes.</returns>
        public static string Read(MiscUtil.IO.EndianBinaryReader stream, long length)
        {
            int theLength = (int)length;
            string result = null;
            try
            {
                byte[] bytes = new byte[length];
                int readBytes = stream.Read(bytes, 0, theLength);
                if (readBytes > 0)
                    result = System.Text.Encoding.ASCII.GetString(bytes, 0, readBytes);
            }
            catch
            {
            }
            return result;
        }

        /// <summary>
        /// Reads the contents of the specified file.
        /// </summary>
        /// <param name="fileName">The file name of the file to be read.</param>
        /// <returns>Returns the contents of the specified file.</returns>
        public static string ReadContents(string fileName)
        {
            string result = null;
            try
            {
                using (var file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var reader = new StreamReader(file);
                    string contents = reader.ReadToEnd();
                    reader.Dispose();
                    result = contents;
                }
            }
            catch
            {
            }
            return result;
        }

        public static byte[] ReadContentBytes(string fileName)
        {
            using (System.IO.FileStream file = new System.IO.FileStream(fileName, System.IO.FileMode.Open,
                System.IO.FileAccess.Read, System.IO.FileShare.Read))
            {
                System.IO.BinaryReader reader = new System.IO.BinaryReader(file);
                int size = Convert.ToInt32(file.Length);
                var result = reader.ReadBytes(size);
                reader.Dispose();
                return result;
            }
        }

        /// <summary>
        /// Reads a byte from the file stream and advances the read position one byte.
        /// </summary>
        /// <param name="stream">The file stream to read from.</param>
        /// <returns>Returns the string representation of the read byte.</returns>
        public static string ReadByte(System.IO.FileStream stream)
        {
            string result = null;
            try
            {
                result = System.Text.Encoding.ASCII.GetString(new byte[] { (byte)stream.ReadByte() });
            }
            catch
            {
            }
            return result;
        }

        /// <summary>
        /// Reads an entire file into an OrderedMap.
        /// </summary>
        /// <param name="fileName">The name of the file to open and read.</param>
        /// <returns>Returns an OrderedMap containing the data from the file.</returns>
        public static OrderedMap FileToArray(string fileName)
        {
            OrderedMap result = null;
            using (System.IO.FileStream file = new System.IO.FileStream(fileName, System.IO.FileMode.Open,
                System.IO.FileAccess.Read, System.IO.FileShare.Read))
            {
                result = new OrderedMap();
                System.IO.StreamReader reader = new StreamReader(file);
                string line = reader.ReadLine();
                while (line != null)
                {
                    result[line] = line;
                    line = reader.ReadLine();
                }
            }

            return result;
        }

        public static OrderedMap FileToArray(StreamReader reader)
        {
            var result = new OrderedMap();
            var line = reader.ReadLine();
            while (line != null)
            {
                result[line] = line;
                line = reader.ReadLine();
            }
            return result;
        }

        /// <summary>
        /// Closes the specified FileStream object.
        /// </summary>
        /// <param name="stream">The FileStream object to close.</param>
        /// <returns>Returns a boolean value that indicates whether the stream was successfully closed (true) or not (false).</returns>
        public static bool Close(System.IO.FileStream stream)
        {
            bool result;
            try
            {
                if (stream != null) stream.Dispose();
                result = true;
            }
            catch
            {
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Writes the specified data in the specified FileStream object.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="data">The data to be written to the stream.</param>
        /// <param name="length">The lenght of the data to be written.</param>
        /// <returns>Returns the number of bytes written.</returns>
        public static int Write(System.IO.FileStream stream, string data, int length)
        {
            int resultLength = -1;
            try
            {
                if (length > 0 && length < data.Length) data = data.Substring(0, length);
                System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                byte[] bytes = encoding.GetBytes(data);
                stream.Write(bytes, 0, bytes.Length);
                resultLength = (int)bytes.Length;
            }
            catch
            {
            }
            return resultLength;
        }

        /// <summary>
        /// Rewinds the positions of this stream to the beginning.
        /// </summary>
        /// <param name="stream">The stream to be rewinded.</param>
        /// <returns>Returns a boolean value that indicates whether the stream was successfully rewindde (true) or not (false).</returns>
        public static bool Rewind(System.IO.FileStream stream)
        {
            bool result = false;
            try
            {
                if (stream.CanSeek)
                {
                    stream.Seek(0, System.IO.SeekOrigin.Begin);
                    result = true;
                }
            }
            catch
            {
            }
            return result;
        }

        /// <summary>
        /// Returns a booolean value that indicates whether the specified file or directory exists and is writable.
        /// </summary>
        /// <param name="fileName">The file or directory name to be checked.</param>
        /// <returns>Returns a booolean value that indicates whether the specified file or directory exists and is writable.</returns>
        public static bool IsWritable(string fileName)
        {
            bool result = false;
            try
            {
                if (System.IO.File.Exists(fileName) || System.IO.Directory.Exists(fileName))
                {
                    System.IO.FileAttributes attributes = System.IO.File.GetAttributes(fileName);
                    result = !((attributes & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly);
                }
            }
            catch
            {
            }
            return result;
        }

        ///// <summary>
        ///// Reads and outputs the contents of the specified file.
        ///// </summary>
        ///// <param name="fileName">The file name of the file to be read.</param>
        ///// <returns>Returns the length of the data read.</returns>
        //public static int OutputFile(string fileName)
        //{
        //    int length = -1;
        //    try
        //    {
        //        string contents = ReadContents(fileName);
        //        System.Web.HttpContext.Current.Response.Write(contents);
        //        length = (int)contents.Length;
        //    }
        //    catch
        //    {
        //    }
        //    return length;
        //}

        ///// <summary>
        ///// Returns an OrderedMap with the pathnames that match the specified pattern.
        ///// </summary>
        ///// <param name="pattern">The search pattern.</param>
        ///// <returns>Returns an OrderedMap with the pathnames that match the specified pattern.</returns>
        //public static OrderedMap Glob(string pattern)
        //{
        //    OrderedMap newOrderedMap = null;
        //    try
        //    {
        //        string path =
        //            System.Web.HttpContext.Current.Request.MapPath(
        //                System.Web.HttpContext.Current.Request.ApplicationPath);
        //        System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(path));
        //        System.IO.FileSystemInfo[] fileInfos = dirInfo.GetFiles(pattern);
        //        if (fileInfos.Length > 0)
        //        {
        //            newOrderedMap = new OrderedMap();
        //            for (int index = 0; index < fileInfos.Length; index++)
        //                newOrderedMap[index] = fileInfos[index].Name;
        //        }
        //    }
        //    catch
        //    {
        //    }
        //    return newOrderedMap;
        //}

        ///// <summary>
        ///// Reads the specified INI file and returns the contents in an OrderedMap.
        ///// </summary>
        ///// <param name="fileName">The INI file to read.</param>
        ///// <returns>Returns the contents of the specified INI file.</returns>
        //public static OrderedMap ParseINI(string fileName)
        //{
        //    OrderedMap newOrderedMap = null;
        //    try
        //    {
        //        using (System.IO.StreamReader stream = new System.IO.StreamReader(fileName))
        //        {
        //            newOrderedMap = new OrderedMap();
        //            string line;
        //            while ((line = stream.ReadLine()) != null)
        //            {
        //                line = line.Trim();
        //                if (line != "" && !line.StartsWith(";") && !line.StartsWith("["))
        //                {
        //                    string[] lineContents = line.Split('=');
        //                    newOrderedMap[lineContents[0].Trim()] = lineContents[1].Trim();
        //                }
        //            }
        //        }
        //    }
        //    catch
        //    {
        //    }
        //    return newOrderedMap;
        //}

        /// <summary>
        /// Reads a line from the specified stream. 
        /// Reading ends when one of the following conditions is met:
        /// <list type="bullet">
        /// <item>Length - 1 bytes have been read.</item>
        /// <item>On a newline.</item>
        /// <item>On EOF.</item>
        /// </list>
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="length">The maximum length of the line to be read.</param>
        /// <returns>Returns a string value that represents a line.</returns>
        public static string ReadLine(System.IO.FileStream stream, int length)
        {
            string line = null;
            try
            {
                int count = 0;
                bool endOfLine = false;
                while (!endOfLine)
                {
                    if (stream.Position < stream.Length && count < length)
                    {
                        //A line is defined as a sequence of characters followed by:
                        //	a carriage return (hexadecimal 0x000d)
                        //	a line feed (hexadecimal 0x000a)
                        //	or carriage return + line feed (hexadecimal 0x000d 0x000a)
                        byte theByte = (byte)stream.ReadByte();
                        if (theByte == (byte)0x0d || theByte == (byte)0x0a)
                        {
                            byte nextByte = (byte)stream.ReadByte();
                            if (nextByte != (byte)0x0a)
                                stream.Position--; //if line ends with 0x000d 0x000a, then consume 0x000a.
                            endOfLine = true;
                        }
                        else
                            line += System.Text.Encoding.ASCII.GetString(new byte[] { theByte });
                    }
                    else
                        endOfLine = true;
                }
            }
            catch
            {
            }
            return line;
        }

        /// <summary>
        /// Reads a line from the specified stream and parses it for CSV fields.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="length">The maximum length of the line to be read</param>
        /// <param name="delimiter">The delimiter which separates the CSV fields.</param>
        /// <returns>Returns an OrderedMap that contains the CSV fields of the read line.</returns>
        public static OrderedMap ReadCSV(System.IO.FileStream stream, int length, string delimiter)
        {
            OrderedMap newOrderedMap = null;
            try
            {
                string line = ReadLine(stream, length);
                if (line != null)
                {
                    if (delimiter == null || delimiter == string.Empty) delimiter = ",";
                    string[] fields = line.Split(delimiter[0]);
                    for (int index = 0; index < fields.Length; index++)
                        fields[index] = fields[index].Trim();

                    newOrderedMap = new OrderedMap(fields, false);
                }
            }
            catch
            {
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Returns a string value that indicates whether the specified path is a file (file) or a directory ("dir").
        /// </summary>
        /// <param name="fileName">The path to be checked.</param>
        /// <returns>Returns a string value that indicates whether the specified path is a file ("file") or a directory ("dir").</returns>
        public static string FileType(string path)
        {
            string result = "";
            if (System.IO.File.Exists(path))
                result = "file";
            else if (System.IO.Directory.Exists(path))
                result = "dir";
            else
                result = "unkown";

            return result;
        }

        ///// <summary>
        ///// Outputs all remaining data in a file stream to the current HTTP output content stream. It also closes the stream.
        ///// </summary>
        ///// <param name="stream">The stream to read the data from.</param>
        ///// <returns>Returns the number of bytes written.</returns>
        //public static int OutputContents(System.IO.BinaryReader stream)
        //{
        //    int length = -1;
        //    try
        //    {
        //        var result = stream.ReadBytes(Convert.ToInt32(stream.BaseStream.Length - stream.BaseStream.Position));
        //        stream.Close();
        //        System.Web.HttpContext.Current.Response.Write(result);
        //        length = (int)result.Length;
        //    }
        //    catch
        //    {
        //    }
        //    return length;
        //}

        /// <summary>
        /// Creates a temporal file and opens it.
        /// </summary>
        /// <returns>Returns a System.IO.FileStream object that represents the temporal file.</returns>
        public static System.IO.FileStream TempFile()
        {
            var fileName = System.IO.Path.GetTempFileName();
            var stream = FileOpen(fileName, "w+");
            return stream;
        }
    }
}