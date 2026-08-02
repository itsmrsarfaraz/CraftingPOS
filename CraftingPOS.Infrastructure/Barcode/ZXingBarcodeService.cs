using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Enums;
using ZXing;
using ZXing.Common;

namespace CraftingPOS.Infrastructure.Barcode;

public class ZXingBarcodeService : IBarcodeService
{
    public BarcodeImageDto Generate(string content, BarcodeSymbology symbology, int width = 300, int height = 100)
    {
        var format = symbology switch
        {
            BarcodeSymbology.EAN13 => BarcodeFormat.EAN_13,
            _ => BarcodeFormat.CODE_128
        };

        var writer = new BarcodeWriterPixelData
        {
            Format = format,
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 4
            }
        };

        var pixelData = writer.Write(content);

        return new BarcodeImageDto
        {
            PixelDataBgra32 = pixelData.Pixels,
            Width = pixelData.Width,
            Height = pixelData.Height
        };
    }
}