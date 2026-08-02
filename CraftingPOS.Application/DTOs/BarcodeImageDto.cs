namespace CraftingPOS.Application.DTOs;

/// <summary>
/// Raw BGRA32 pixel buffer for a generated barcode. Kept free of any
/// System.Drawing or WPF dependency so Application/Infrastructure stay
/// platform-agnostic; Presentation converts this into a WriteableBitmap.
/// </summary>
public class BarcodeImageDto
{
    public byte[] PixelDataBgra32 { get; set; } = Array.Empty<byte>();
    public int Width { get; set; }
    public int Height { get; set; }
}