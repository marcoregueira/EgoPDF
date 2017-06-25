using System;
using System.Windows.Forms;

using Ego.PDF.Data;
using Ego.PDF.Samples;

namespace Ego.PDF.WindowsFormsTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
           
        }

        public void DoSample1()
        {
            var pdf = Sample1.GetSample( );
            pdf.Output("sample1.pdf", OutputDevice.SaveToFile);
        }

        public void DoSample2()
        {
            {
                var pdf = Sample2.GetSample("logo.png");
                pdf.Output("sample2.a.pdf", OutputDevice.SaveToFile);
            }
            {
//                var pdf = Sample2.GetSample("gift.jpg");
  //              pdf.Output("sample2.b.pdf", OutputDevice.SaveToFile);
            }
        }

       
        public void DoSample3()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var pdf = Sample3.GetSample(path);
            pdf.Output("sample3.pdf", OutputDevice.SaveToFile);
        }

        public void DoSample4()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var pdf = Sample4.GetSample(path);
            pdf.Output("sample4.pdf", OutputDevice.SaveToFile);
        }

        public void DoSample5()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var pdf = Sample5.GetSample(path);
            pdf.Output("sample5.pdf", OutputDevice.SaveToFile);
        }

        public void DoSample6()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var pdf = Sample6.GetSample(path);
            pdf.Output("sample6.pdf", OutputDevice.SaveToFile);
        }

        private void BtnRunSamples_Click(object sender, EventArgs e)
        {
            DoSample1();
            DoSample2();
            DoSample3();
            DoSample4();
            DoSample5();
            DoSample6();
        }

        private void BtnParseFont_Click(object sender, EventArgs e)
        {
            var ttfParser = new TtfParser();    
            ttfParser.Parse("C:\\Users\\MarcoAntonio\\Downloads\\RetroArch (1)\\assets\\zarch\\Roboto-Condensed.ttf");
        }
    }
}
