using Ego.PDF.Data;
using Ego.PdfCore.NewFont;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Xunit;
using static Ego.PdfCore.NewFont.FontData;

namespace FontImport
{
    //TODO: MIGRATE TO .NET CORE 3.0
    public class FontGenerators
    {
        [Fact]
        public void GenerateArial()
        {
            var font = LoadFont("file:///C:\\WINDOWS\\Fonts\\cour.ttf");
            var font2 = LoadFont("file:///C:\\WINDOWS\\Fonts\\arial.ttf");
            var fontData = JsonConvert.SerializeObject(font2, Formatting.Indented);
            File.WriteAllText("font.ttf.json",fontData);
        }

        public FontData LoadFont(string path)
        {
            GlyphTypeface font = new GlyphTypeface(new Uri(path));
            var fontData = new FontData
            {
                Name = font.FamilyNames.FirstOrDefault().Value,
            };

            foreach (var advance in font.AdvanceWidths)
            {
                var leftBearing = font.LeftSideBearings[advance.Key];
                var rightBearing = font.RightSideBearings[advance.Key];
                fontData.FontWidths[advance.Key] = new AdvanceWidth
                {
                    Point = advance.Key,
                    LeftBearing = leftBearing,
                    RightBearing = rightBearing,
                    Advance = advance.Value
                };
            }
            return fontData;
        }
    }
}
