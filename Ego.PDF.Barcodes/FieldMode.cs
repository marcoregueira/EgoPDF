namespace Ego.PDF.Barcodes.Zpl;

public class BarcodeOptions
{
    /// <summary>
    /// The default bar code height, in dots. 
    /// Any positive number may be used. 
    /// The default value is the previously configured value, or 10 if no value has been set.
    /// </summary>
    public int Height { get; set; } = 10;

    /// <summary>
    /// widthRatio: The default ratio between wide bars and narrow bars. 
    /// Any decimal number between 2 and 3 may be used. 
    /// The number must be a multiple of 0.1 (i.e. 2.0, 2.1, 2.2, 2.3, ... , 2.9, 3.0). 
    /// Larger numbers generally result in fewer bar code scan failures. 
    /// The default value is the previously configured value, or 3 if no value has been set.
    /// </summary>

    public decimal WidthRatio { get; set; } = 3;
    /// <summary>
    /// The default bar code module width, in dots. 
    /// Any number between 1 and 100 may be used. 
    /// The default value is the previously configured value, or 2 if no value has been set.
    /// </summary>
    public int Width { get; set; } = 2;
}

public class Barcode128Options
{
    /*
        orientation: The bar code orientation to use. Valid values are N (no rotation), R (rotate 90° clockwise), I (rotate 180° clockwise), and B (rotate 270° clockwise). The default value is the orientation configured via the ^FW command, which itself defaults to N (no rotation).
        height: The bar code height, in dots. Any number between 1 and 32,000 may be used. The default value is the bar code height configured via the ^BY command, which itself defaults to 10.
        line: Whether or not to include human-readable text with the bar code. Valid values are Y and N. The default value is Y (include human-readable text).
        lineAbove: Whether or not to place the human-readable text above the bar code. Valid values are Y and N. The default value is N (if printed, text is placed below the bar code), except for mode U where the default is Y (if printed, text is placed above the bar code).
        checkDigit: Whether or not to calculate a GS1 (UCC) Mod 10 check digit. Valid values are Y and N. The default value is N (GS1 check digit is not calculated).
        mode: The mode to use to encode the bar code data. Valid values are N (no mode, subsets are specified explicitly as part of the field data), U (UCC case mode, field data must contain 19 digits), A (automatic mode, the ZPL engine automatically determines the subsets that are used to encode the data), and D (UCC/EAN mode, field data must contain GS1 numbers). The default value is N (no mode, subsets are specified explicitly as part of the field data).
    */

    /// <summary>
    /// Empty, N, R, I, B
    /// </summary>
    public string Orientation { get; set; } = "";
    public int Height { get; set; } = 10;
    public bool Line { get; set; } = true;
    public bool LineAbove { get; set; } = false;
}

/// <summary>Options for ^B2 Interleaved 2 of 5.</summary>
public class Barcode2of5Options
{
    /// <summary>Empty, N, R, I, B.</summary>
    public string Orientation { get; set; } = "";
    public int Height { get; set; } = 10;
    public bool Line { get; set; } = true;
    public bool LineAbove { get; set; } = false;
    public bool CheckDigit { get; set; } = false;
}

/// <summary>
/// Shared options for the simple 1D ZPL barcodes (^B3 Code 39, ^BK
/// Codabar, ^BE EAN-13, ^B8 EAN-8, ^BU UPC-A, ^B9 UPC-E, ^BM MSI).
/// </summary>
public class Barcode1DOptions
{
    /// <summary>Empty, N, R, I, B.</summary>
    public string Orientation { get; set; } = "";
    public int Height { get; set; } = 10;
    public bool Line { get; set; } = true;
    public bool LineAbove { get; set; } = false;
    public bool CheckDigit { get; set; } = false;
}

/// <summary>
/// Shared options for ZPL 2D barcodes (^BQ QR Code, ^BX Data Matrix,
/// ^B7 PDF417, ^BO Aztec). Magnification is in dots-per-module and
/// scales the rendered matrix proportionally.
/// </summary>
public class Barcode2DOptions
{
    /// <summary>Empty, N, R, I, B.</summary>
    public string Orientation { get; set; } = "";
    public int Magnification { get; set; } = 4;
}

public enum FieldMode
{

    Text = 0,
    Barcode,
}

public enum BarcodeMode
{
    Code128,
    Code39,
    Interleaved2of5,
    Codabar,
    EAN13,
    EAN8,
    UPC_A,
    UPC_E,
    MSI,
    QrCode,
    DataMatrix,
    PDF417,
    Aztec,
}