using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ego.PDF.Data;
using Ego.PDF.PHP;

namespace Ego.PDF
{
    public static class FPdfExtensions
    {
        public static string Output(this FPdf document, string name, OutputDevice destination)
        {
            // Output PDF to some destination
            if (document.State < 3)
            {
                document.Close();
            }
            if (destination == OutputDevice.Default)
            {
                if (string.IsNullOrEmpty(name))
                {
                    name = "doc.pdf";
                    destination = OutputDevice.StandardOutput;
                }
                else
                {
                    destination = OutputDevice.SaveToFile;
                }
            }
            switch (destination)
            {
                //case OutputDevice.StandardOutput:
                //    HttpContext.Current.Response.AppendHeader("Content-Type: application/pdf", "");
                //    HttpContext.Current.Response.AppendHeader("Content-Disposition: inline; filename=\"" + name + "\"",
                //                                              "");
                //    HttpContext.Current.Response.AppendHeader("Cache-Control: private, max-age=0, must-revalidate", "");
                //    HttpContext.Current.Response.AppendHeader("Pragma: public", "");
                //    HttpContext.Current.Response.Write(document.Buffer);
                //    break;

                //case OutputDevice.Download:
                //    // Download file
                //    HttpContext.Current.Response.AppendHeader("Content-Type: application/x-download", "");
                //    HttpContext.Current.Response.AppendHeader(
                //        "Content-Disposition: attachment; filename=\"" + name + "\"", "");
                //    HttpContext.Current.Response.AppendHeader("Cache-Control: private, max-age=0, must-revalidate", "");
                //    HttpContext.Current.Response.AppendHeader("Pragma: public", "");
                //    HttpContext.Current.Response.Write(document.Buffer);
                //    break;

                case OutputDevice.SaveToFile:
                    // Save to local file
                    FileStream f = FileSystemSupport.FileOpen(name, "wb");
                    if (!TypeSupport.ToBoolean(f))
                    {
                        throw new InvalidOperationException("Unable to create output file: " + name);
                    }
                    var writer = new StreamWriter(f, FPdf.PrivateEncoding);
                    writer.Write(document.Buffer);
                    writer.Dispose();
                    break;

                case OutputDevice.ReturnAsString:
                    return "";
                //return document.Buffer;

                default:
                    throw new InvalidOperationException("Incorrect output destination: " + destination);
                    break;
            }
            return string.Empty;
        }
    }
}
