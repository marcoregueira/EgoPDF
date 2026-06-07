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

/// <summary>
/// Per-field options for every ZPL 1D barcode the engine renders: the
/// simple symbologies (^B3 Code 39, ^BK Codabar, ^BE EAN-13, ^B8 EAN-8,
/// ^BU UPC-A, ^B9 UPC-E, ^BM MSI) plus ^BC Code 128 and ^B2 Interleaved
/// 2 of 5. Each ^B? command parses its parameter tail into one of these,
/// then the FieldDefinition.Draw dispatch reads it back.
/// </summary>
/// <remarks>
/// FieldDefinition keeps three independent instances of this type
/// (Barcode128Options, Barcode2of5Options, Barcode1DOptions) so a label
/// that mixes ^BC and ^B3 doesn't leak parameters between the two.
/// CheckDigit is ignored by Code 128 and the simple writers that don't
/// expose it.
/// </remarks>
public class Barcode1DOptions
{
    /// <summary>Empty, N, R, I, B.</summary>
    public string Orientation { get; set; } = "";
    /// <summary>
    /// Barcode bar height in dots, or 0 when the ^B? field didn't carry
    /// a height parameter -- in that case the renderer falls back to the
    /// ^BY default (BarcodeOptions.Height) so plain "^BC^FD..." still
    /// inherits the chain's last ^BY,h height.
    /// </summary>
    public int Height { get; set; } = 0;
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