using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ego.PDF;
using Ego.PDF.Data;
using Ego.PDF.Samples;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public void DoSample1()
        {
            var pdf = Sample1.GetSample("sample1.pdf");
        }

        public void DoSample2()
        {
            {
                var pdf = Sample2.GetSample("sample2.a.pdf","logo.png");
            }
            {
                //                var pdf = Sample2.GetSample("gift.jpg");
                //              pdf.Output("sample2.b.pdf", OutputDevice.SaveToFile);
            }
        }


        public void DoSample3()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var pdf = Sample3.GetSample("sample3.pdf", path);
        }

        public void DoSample4()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var pdf = Sample4.GetSample("sample4.pdf",path);
        }

        public void DoSample5()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var pdf = Sample5.GetSample("sample5.pdf",path);
        }

        public void DoSample6()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var pdf = Sample6.GetSample("sample6.pdf",path);
        }

        public void DoSample8()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var pdf = Sample8.GetSample("sample8.pdf", path);
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

        private void BtnMakeFont_Click(object sender, EventArgs e)
        {
            var fontMaker = new Make();
            fontMaker.MakeFont("C:\\Users\\MarcoAntonio\\Downloads\\RetroArch (1)\\assets\\zarch\\Roboto-Condensed.ttf");
        }
    }
}
