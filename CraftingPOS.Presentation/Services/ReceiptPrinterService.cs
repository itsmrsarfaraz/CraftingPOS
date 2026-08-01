using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;

namespace CraftingPOS.Presentation.Services;

/// <summary>
/// Implemented here (in Presentation) rather than Infrastructure, since WPF
/// printing (FlowDocument/PrintDialog) is a UI-thread, platform-specific
/// concern — consistent with how OpenFileDialog is already used directly
/// in ProductsViewModel. The interface stays in Application for DI/testability.
/// </summary>
public class ReceiptPrinterService : IReceiptPrinterService
{
    public string BuildPreviewText(ReceiptDto receipt, ReceiptPaperWidth width)
    {
        var charWidth = width == ReceiptPaperWidth.Width58mm ? 32 : 48;
        var sb = new StringBuilder();

        AppendCentered(sb, receipt.BusinessName, charWidth);
        if (!string.IsNullOrWhiteSpace(receipt.BusinessAddress))
            AppendCentered(sb, receipt.BusinessAddress, charWidth);
        if (!string.IsNullOrWhiteSpace(receipt.BusinessPhone))
            AppendCentered(sb, receipt.BusinessPhone, charWidth);

        sb.AppendLine(new string('-', charWidth));
        sb.AppendLine($"Invoice: {receipt.InvoiceNumber}");
        sb.AppendLine($"Date:    {receipt.SaleDate:dd MMM yyyy HH:mm}");
        sb.AppendLine($"Cashier: {receipt.CashierName}");
        if (!string.IsNullOrWhiteSpace(receipt.CustomerName))
            sb.AppendLine($"Customer: {receipt.CustomerName}");
        sb.AppendLine(new string('-', charWidth));

        foreach (var item in receipt.Items)
        {
            sb.AppendLine(Truncate(item.ProductName, charWidth));
            var qtyPrice = $"{item.Quantity} x {item.UnitPrice:N0}";
            var lineTotal = item.LineTotal.ToString("N0");
            sb.AppendLine(PadBetween(qtyPrice, lineTotal, charWidth));
        }

        sb.AppendLine(new string('-', charWidth));
        sb.AppendLine(PadBetween("Subtotal", receipt.SubTotal.ToString("N0"), charWidth));
        if (receipt.Discount > 0)
            sb.AppendLine(PadBetween("Discount", receipt.Discount.ToString("N0"), charWidth));
        sb.AppendLine(PadBetween("GRAND TOTAL", receipt.GrandTotal.ToString("N0"), charWidth));
        sb.AppendLine(new string('-', charWidth));
        sb.AppendLine($"Payment: {receipt.PaymentMethod}");

        if (receipt.AmountReceived.HasValue)
            sb.AppendLine(PadBetween("Received", receipt.AmountReceived.Value.ToString("N0"), charWidth));
        if (receipt.ChangeDue.HasValue)
            sb.AppendLine(PadBetween("Change", receipt.ChangeDue.Value.ToString("N0"), charWidth));

        sb.AppendLine(new string('-', charWidth));
        AppendCentered(sb, receipt.FooterMessage, charWidth);

        return sb.ToString();
    }

    public void Print(ReceiptDto receipt, ReceiptPaperWidth width)
    {
        var text = BuildPreviewText(receipt, width);

        var pageWidthPx = width == ReceiptPaperWidth.Width58mm ? 219d : 302d; // mm -> 96dpi px

        var textBlock = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            Width = pageWidthPx,
            Background = Brushes.White,
            Foreground = Brushes.Black,
            TextWrapping = TextWrapping.NoWrap
        };

        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
        {
            printDialog.PrintVisual(textBlock, $"Receipt {receipt.InvoiceNumber}");
        }
    }

    private static void AppendCentered(StringBuilder sb, string text, int width)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var padding = Math.Max(0, (width - text.Length) / 2);
        sb.AppendLine(new string(' ', padding) + text);
    }

    private static string PadBetween(string left, string right, int width)
    {
        var spaces = Math.Max(1, width - left.Length - right.Length);
        return left + new string(' ', spaces) + right;
    }

    private static string Truncate(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}