using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Enums;

namespace CraftingPOS.Presentation.Views;

public partial class BarcodeLabelWindow : Window
{
    private readonly IBarcodeService _barcodeService;

    public BarcodeLabelWindow(IBarcodeService barcodeService)
    {
        InitializeComponent();
        _barcodeService = barcodeService;
    }

    public void LoadLabel(string productName, string barcodeValue, decimal price)
    {
        ProductNameText.Text = productName;
        BarcodeValueText.Text = barcodeValue;
        PriceText.Text = $"Rs. {price:N0}";

        // FR-BAR-006: Code128 by default; EAN13 requires exactly 12-13 numeric digits,
        // so we fall back to Code128 for any non-numeric or wrong-length barcode.
        var symbology = (barcodeValue.Length is 12 or 13 && barcodeValue.All(char.IsDigit))
            ? BarcodeSymbology.EAN13
            : BarcodeSymbology.Code128;

        var image = _barcodeService.Generate(barcodeValue, symbology, width: 280, height: 90);

        var bitmap = new WriteableBitmap(image.Width, image.Height, 96, 96,
            System.Windows.Media.PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, image.Width, image.Height),
            image.PixelDataBgra32, image.Width * 4, 0);

        BarcodeImage.Source = bitmap;
    }

    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        // FR-BAR-007: barcode label printing.
        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
        {
            printDialog.PrintVisual(LabelPanel, "CraftingPOS Barcode Label");
        }
    }
}