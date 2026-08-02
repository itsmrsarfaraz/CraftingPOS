using System.Windows;
using CraftingPOS.Presentation.ViewModels;

namespace CraftingPOS.Presentation.Views;

public partial class ReceiptPreviewWindow : Window
{
    private readonly ReceiptPreviewViewModel _viewModel;

    public ReceiptPreviewWindow(ReceiptPreviewViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public async Task LoadAsync(int saleId)
    {
        await _viewModel.LoadAsync(saleId);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}