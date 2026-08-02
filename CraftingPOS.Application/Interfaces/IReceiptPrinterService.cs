using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface IReceiptPrinterService
{
    /// <summary>Formats the receipt as plain monospace text sized to the given paper width, for on-screen preview.</summary>
    string BuildPreviewText(ReceiptDto receipt, ReceiptPaperWidth width);

    /// <summary>FR-PRINT-001/004: sends the receipt to a thermal printer via the Windows print dialog.</summary>
    void Print(ReceiptDto receipt, ReceiptPaperWidth width);
}