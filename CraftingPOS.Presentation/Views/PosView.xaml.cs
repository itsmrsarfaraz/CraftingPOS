using System.Windows.Controls;
using System.Windows.Input;
using CraftingPOS.Presentation.ViewModels;

namespace CraftingPOS.Presentation.Views;

public partial class PosView : UserControl
{
    private readonly PosViewModel _viewModel;

    public PosView(PosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            await viewModel.LoadCommand.ExecuteAsync(null);
            BarcodeBox.Focus(); // FR-BAR-002: barcode input stays focused during billing
        };
    }

    private async void BarcodeBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await _viewModel.ScanBarcodeCommand.ExecuteAsync(null);
            BarcodeBox.Focus(); // FR-BAR-004: refocus after every scan
        }
    }
}