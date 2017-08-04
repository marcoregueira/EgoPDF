using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ego.PDF.Data;
using Ego.PDF.Samples;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Ego.PDF.WebTestCore.Controllers
{
    public class HomeController : Controller
    {
        private IHostingEnvironment _hostingEnvironment;

        private string CurrentPath
        {
            get
            {
                var path = this.GetType().GetTypeInfo().Assembly.Location;
                return System.IO.Path.GetDirectoryName(path);
            }
        }

        public HomeController(IHostingEnvironment hostingEnvironment)
        {
            this._hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }


        public FileStreamResult GetSample1()
        {
            var pdf = Sample1.GetSample(null);
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample2()
        {
            var pdf = Sample2.GetSample(null, Path.Combine(CurrentPath, "logo.png"));
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample2b()
        {
            var pdf = Sample2.GetSample(null, Path.Combine(CurrentPath, "3d_down.png"));
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample2c()
        {
            var pdf = Sample2.GetSample(null, Path.Combine(CurrentPath, "gift.jpg"));
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample3()
        {
            var pdf = Sample3.GetSample(null, CurrentPath);
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample4()
        {
            var pdf = Sample4.GetSample(null, CurrentPath);
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample5()
        {
            var pdf = Sample5.GetSample(null, CurrentPath);
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample6()
        {
            var pdf = Sample6.GetSample(null, CurrentPath);
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample8()
        {
            var pdf = Sample8.GetSample(null, CurrentPath);
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
