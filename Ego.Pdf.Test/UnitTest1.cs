using Ego.PDF.Samples;
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Ego.Pdf.Test
{
    public class SampleTests
    {
        [Fact]
        public void DoSample1()
        {
            Sample1.GetSample("sample1.pdf");
        }

        [Fact]
        public void DoSample2()
        {
            Sample2.GetSample("sample2.pdf", imagefile: "logo.png");
        }

        [Fact]
        public void DoSample2b()
        {
            Sample2.GetSample("sample2b.pdf", imagefile: "3d_down.png");
        }

        [Fact]
        public void DoSample3()
        {
            Sample3.GetSample("sample3.pdf", GetPath());
        }

        [Fact]
        public void DoSample4()
        {
            Sample4.GetSample("sample4.pdf", GetPath());
        }

        [Fact]
        public void DoSample5()
        {
            Sample5.GetSample("sample5.pdf", GetPath());
        }

        [Fact]
        public void DoSample6()
        {
            Sample6.GetSample("sample6.pdf", GetPath());
        }

        [Fact]
        public void DoSample8()
        {
            Sample8.GetSample("sample8.pdf", GetPath());
        }

        private string GetPath()
        {
            var codeBase = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(codeBase);
        }
    }
}
