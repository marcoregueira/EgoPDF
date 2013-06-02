using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ego.PDF.Data
{
    public enum PageSizeEnum
    {
        Default = 0,
        A3,
        A4,
        A5,
        Letter,
        Legal
    }

    public enum PageOrientation
    {
        Default = 0,
        Portrait,
        Landscape
    }

    public enum OutputDevice
    {
        Default = 0,
        StandardOutput,
        Download,
        SaveToFile,
        ReturnAsString
    }

    public enum UnitEnum
    {
        Default = 0,
        Point,
        Milimeter,
        Centimeter,
        Inch
    }

    public enum AlignEnum
    {
        Default = 0,
        Left,
        Right,
        Center,
        Justified
    }

    public enum ImageTypeEnum
    {
        Default = 0,
        Jpg,
        Png,
        Gif
    }

    public enum FontTypeEnum
    {
        Default = 0,
        Core,
        Type1,
        TrueType
    }

    public enum LayoutEnum
    {
        Default,
        Single,
        Continuous,
        Two
    }

    public enum ZoomEnum
    {
        Default = 0,
        FullPage,
        FullWidth,
        Custom,
        Real
    }
}
