using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Ego.PDF;
using Ego.PDF.Data;
using Ego.PDF.Samples;

namespace Ego.PDF.WebTest.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Modify this template to kick-start your ASP.NET MVC application.";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Samples()
        {
            return View();
        }

        public FileStreamResult GetSample1()
        {
            var pdf = Sample1.GetSample();
            string s = pdf.Output("a.pdf", OutputDevice.ReturnAsString);
            MemoryStream m = new MemoryStream(FPdf.PrivateEnconding.GetBytes(s));
            var result = new FileStreamResult(m, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample2()
        {
            var pdf = Sample2.GetSample(Server.MapPath("../bin/logo.png"));
            string s = pdf.Output("sample1.pdf", OutputDevice.ReturnAsString);
            MemoryStream m = new MemoryStream(FPdf.PrivateEnconding.GetBytes(s));
            var result = new FileStreamResult(m, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample2b()
        {
            var pdf = Sample2.GetSample(Server.MapPath("../bin/3d_down.png"));
            string s = pdf.Output("ap.pdf", OutputDevice.ReturnAsString);
            MemoryStream m = new MemoryStream(FPdf.PrivateEnconding.GetBytes(s));
            var result = new FileStreamResult(m, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample2c()
        {
            var pdf = Sample2.GetSample(Server.MapPath("../bin/gift.jpg"));
            string s = pdf.Output("ap.pdf", OutputDevice.ReturnAsString);
            MemoryStream m = new MemoryStream(FPdf.PrivateEnconding.GetBytes(s));
            var result = new FileStreamResult(m, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample3()
        {
            var pdf = Sample3.GetSample(Server.MapPath("../bin"));
            string s = pdf.Output("ap.pdf", OutputDevice.ReturnAsString);
            MemoryStream m = new MemoryStream(FPdf.PrivateEnconding.GetBytes(s));
            var result = new FileStreamResult(m, "application/pdf");
            return result;
        }

        public FileStreamResult GetSample4()
        {
            var pdf = Sample4.GetSample(Server.MapPath("../bin"));
            string s = pdf.Output("ap.pdf", OutputDevice.ReturnAsString);
            MemoryStream m = new MemoryStream(FPdf.PrivateEnconding.GetBytes(s));
            var result = new FileStreamResult(m, "application/pdf");
            return result;
        }

    }
}
