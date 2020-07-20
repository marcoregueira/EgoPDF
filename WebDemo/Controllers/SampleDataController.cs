using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ego.PDF.Samples;
using Microsoft.AspNetCore.Mvc;

namespace WebDemo.Controllers
{
    [Route("api/[controller]")]
    public class SampleDataController : Controller
    {
        private string CurrentPath
        {
            get
            {
                return AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
            }
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample1()
        {
            var buffer =  Sample1.GetSample(null);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample2()
        {
            var buffer =  Sample2.GetSample(null, Path.Combine(CurrentPath, "logo.png"));
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample2b()
        {
            var buffer =  Sample2.GetSample(null, Path.Combine(CurrentPath, "3d_down.png"));
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample2c()
        {
            var buffer =  Sample2.GetSample(null, Path.Combine(CurrentPath, "gift.jpg"));
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample3()
        {
            var buffer =  Sample3.GetSample(null, CurrentPath);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample4()
        {
            var buffer =  Sample4.GetSample(null, CurrentPath);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample5()
        {
            var buffer = Sample5.GetSample(null, CurrentPath);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample6()
        {
            var buffer =  Sample6.GetSample(null, CurrentPath);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample8()
        {
            var buffer =  Sample8.GetSample(null, CurrentPath);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }
    }
}