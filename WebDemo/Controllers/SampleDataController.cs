using Ego.PDF.Samples;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

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
        public FileStreamResult GetSampleZebra()
        {
            
            var buffer = SampleZebra.GetSample(null, AppDomain.CurrentDomain.BaseDirectory);
            buffer.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(buffer, "application/pdf");
            return result;
        }
    }
}