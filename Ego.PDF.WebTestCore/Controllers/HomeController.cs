using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var pdf = Sample1.GetSample();
            string s = pdf.Output("a.pdf", OutputDevice.ReturnAsString);
            MemoryStream m = new MemoryStream(FPdf.PrivateEncoding.GetBytes(s));
            var result = new FileStreamResult(m, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample2()
        {
            var pdf = Sample2.GetSample(Server.MapPath("../bin/logo.png"));
            string s = pdf.Output("sample1.pdf", OutputDevice.ReturnAsString);
            MemoryStream m = new MemoryStream(FPdf.PrivateEncoding.GetBytes(s));
            var result = new FileStreamResult(m, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample2b()
        {
            var pdf = Sample2.GetSample(Server.MapPath("../bin/3d_down.png"));
            string s = pdf.Output("ap.pdf", OutputDevice.ReturnAsString);
            MemoryStream m = new MemoryStream(FPdf.PrivateEncoding.GetBytes(s));
            var result = new FileStreamResult(m, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample2c()
        {
            var pdf = Sample2.GetSample(Server.MapPath("../bin/gift.jpg"));
            string s = pdf.Output("ap.pdf", OutputDevice.ReturnAsString);
            MemoryStream m = new MemoryStream(FPdf.PrivateEncoding.GetBytes(s));
            var result = new FileStreamResult(m, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample3()
        {
            var pdf = Sample3.GetSample(Server.MapPath("../bin"));
            string s = pdf.Output("ap.pdf", OutputDevice.ReturnAsString);
            MemoryStream m = new MemoryStream(FPdf.PrivateEncoding.GetBytes(s));
            var result = new FileStreamResult(m, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample4()
        {
            var pdf = Sample4.GetSample(Server.MapPath("../bin"));
            string s = pdf.Output("ap.pdf", OutputDevice.ReturnAsString);
            MemoryStream m = new MemoryStream(FPdf.PrivateEncoding.GetBytes(s));
            var result = new FileStreamResult(m, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample5()
        {
            var pdf = Sample5.GetSample(Server.MapPath("../bin"));
            var s = pdf.Output("ap.pdf", OutputDevice.ReturnAsString);
            var m = new MemoryStream(FPdf.PrivateEncoding.GetBytes(s));
            var result = new FileStreamResult(m, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample6()
        {
            var pdf = Sample6.GetSample(Server.MapPath("../bin"));
            var s = pdf.Output("ap.pdf", OutputDevice.ReturnAsString);
            var m = new MemoryStream(FPdf.PrivateEncoding.GetBytes(s));
            var result = new FileStreamResult(m, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample8()
        {
            var pdf = Sample8.GetSample(Server.MapPath("../bin"));
            var s = pdf.Output("ap.pdf", OutputDevice.ReturnAsString);
            var m = new MemoryStream(FPdf.PrivateEncoding.GetBytes(s));
            var result = new FileStreamResult(m, "application/pdf");
            return result;
        }



        public IActionResult Error()
        {
            return View();
        }
    }
}
