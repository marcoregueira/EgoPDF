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
        private static string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [HttpGet("[action]")]
        public IEnumerable<WeatherForecast> WeatherForecasts()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                DateFormatted = DateTime.Now.AddDays(index).ToString("d"),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            });
        }

        public class WeatherForecast
        {
            public string DateFormatted { get; set; }
            public int TemperatureC { get; set; }
            public string Summary { get; set; }

            public int TemperatureF
            {
                get
                {
                    return 32 + (int)(TemperatureC / 0.5556);
                }
            }
        }

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
            var pdf = Sample1.GetSample(null);
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample2()
        {
            var pdf = Sample2.GetSample(null, Path.Combine(CurrentPath, "logo.png"));
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample2b()
        {
            var pdf = Sample2.GetSample(null, Path.Combine(CurrentPath, "3d_down.png"));
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample2c()
        {
            var pdf = Sample2.GetSample(null, Path.Combine(CurrentPath, "gift.jpg"));
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample3()
        {
            var pdf = Sample3.GetSample(null, CurrentPath);
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample4()
        {
            var pdf = Sample4.GetSample(null, CurrentPath);
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample5()
        {
            var pdf = Sample5.GetSample(null, CurrentPath);
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample6()
        {
            var pdf = Sample6.GetSample(null, CurrentPath);
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }

        [HttpGet("[action]")]
        public FileStreamResult GetSample8()
        {
            var pdf = Sample8.GetSample(null, CurrentPath);
            pdf.Buffer.BaseStream.Seek(0, SeekOrigin.Begin);
            var result = new FileStreamResult(pdf.Buffer.BaseStream, "application/pdf");
            return result;
        }
    }
}