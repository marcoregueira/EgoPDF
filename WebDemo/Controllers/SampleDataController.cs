using Ego.PDF.Samples;
using Microsoft.AspNetCore.Mvc;

namespace WebDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class SampleDataController: Controller
    {

        public SampleDataController()
        {
        }

        private string CurrentPath
        {
            get
            {
                var path = Path.Combine(Environment.CurrentDirectory, "App_Data");
                return path;
            }
        }

        [HttpGet()]
        public FileStreamResult GetSample1()
        {
            var buffer = Sample1.GetSample(null);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet()]
        public FileStreamResult GetSample2()
        {
            var buffer = Sample2.GetSample(null, Path.Combine(CurrentPath, "logo.png"));
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet()]
        public FileStreamResult GetSample2b()
        {
            var buffer = Sample2.GetSample(null, Path.Combine(CurrentPath, "3d_down.png"));
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet()]
        public FileStreamResult GetSample2c()
        {
            var buffer = Sample2.GetSample(null, Path.Combine(CurrentPath, "gift.jpg"));
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet()]
        public FileStreamResult GetSample3()
        {
            var buffer = Sample3.GetSample(null, CurrentPath);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet()]
        public FileStreamResult GetSample4()
        {
            var buffer = Sample4.GetSample(null, CurrentPath);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet()]
        public FileStreamResult GetSample5()
        {
            var buffer = Sample5.GetSample(null, CurrentPath);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet()]
        public FileStreamResult GetSample6()
        {
            var buffer = Sample6.GetSample(null, CurrentPath);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet()]
        public FileStreamResult GetSample7()
        {
            var buffer = Sample7.GetSample(null);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }


        [HttpGet()]
        public FileStreamResult GetSample8()
        {
            var buffer = Sample8.GetSample(null, CurrentPath);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet()]
        public FileStreamResult GetSample8b()
        {
            var buffer = Sample8b.GetSample(null, CurrentPath);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet()]
        public FileStreamResult GetSample8c()
        {
            var buffer = Sample8c.GetSample(null, CurrentPath);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet()]
        public FileStreamResult GetSampleMarkdown()
        {
            var buffer = SampleMarkdown.GetSample(null);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet()]
        public FileStreamResult GetSamplePhotovoltaic()
        {
            // Pass null so the sample resolves photo assets against
            // AppDomain.BaseDirectory (= the bin root). The PV pictures
            // are propagated there by the Ego.PDF.Samples project
            // reference, not into App_Data like the invoice assets.
            var buffer = SamplePhotovoltaic.GetSample(null, null);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet()]
        public FileStreamResult GetSampleZebra([FromQuery] int id)
        {
            var buffer =
                id == 1 ? SampleZebra.GetSampleHorizontalShipping(null, AppDomain.CurrentDomain.BaseDirectory) :
                id == 2 ? SampleZebra.GetSampleVertical1(null, AppDomain.CurrentDomain.BaseDirectory) :
                id == 3 ? SampleZebra.GetSampleBarcodes(null, AppDomain.CurrentDomain.BaseDirectory) : null;

            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        /// <summary>
        /// Debug-only: renders the in-repo SampleZebra.GetSampleReverseText
        /// so the ^FR text-reverse fix can be inspected in the browser.
        /// </summary>
        [HttpGet()]
        public FileStreamResult GetSampleZebraReverseText()
        {
            var buffer = SampleZebra.GetSampleReverseText(null, AppDomain.CurrentDomain.BaseDirectory);
            buffer.Seek(0, SeekOrigin.Begin);
            return new FileStreamResult(buffer, "application/pdf");
        }

        /// <summary>
        /// Debug-only: renders an ad-hoc ZPL kept under tools/local-debug/
        /// (gitignored). Walks up from AppContext.BaseDirectory to find
        /// the repo root so it works regardless of where dotnet run is
        /// invoked from. 404 with a friendly message when the file is
        /// missing instead of throwing.
        /// </summary>
        [HttpGet()]
        public IActionResult GetSampleZebraDebugLocal()
        {
            var zplPath = FindLocalDebugFile("seur-debug.zpl");
            if (zplPath == null)
            {
                return NotFound(
                    "No tools/local-debug/seur-debug.zpl was found in the repository root. " +
                    "Drop the ZPL there to enable this debug endpoint.");
            }
            var buffer = SampleZebra.GetSampleFromZplFile(null, zplPath);
            buffer.Seek(0, SeekOrigin.Begin);
            return new FileStreamResult(buffer, "application/pdf");
        }

        private static string FindLocalDebugFile(string fileName)
        {
            // Walk up the directory tree from the bin output looking for
            // a tools/local-debug/<fileName>. Lets the endpoint work
            // whether dotnet run was launched from the repo root or the
            // WebDemo project.
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "tools", "local-debug", fileName);
                if (System.IO.File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            return null;
        }

        [HttpGet()]
        public FileStreamResult GetSample9()
        {
            var buffer = Sample9.GetSample(null, CurrentPath);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }
    }
}