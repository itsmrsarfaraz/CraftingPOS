using CraftingPOS.Application.DTOs;
using CraftingPOS.Domain.Enums;

namespace CraftingPOS.Application.Interfaces;

public interface IBarcodeService
{
    /// <summary>FR-BAR-005/006: generate a barcode image for the given content.</summary>
    BarcodeImageDto Generate(string content, BarcodeSymbology symbology, int width = 300, int height = 100);
}