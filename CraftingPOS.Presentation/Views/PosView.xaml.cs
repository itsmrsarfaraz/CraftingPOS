using System.Windows.Controls;
using System.Windows.Input;
using CraftingPOS.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CraftingPOS.Presentation.Views;

public partial class PosView : UserControl
{
    private readonly PosViewModel _viewModel;

    public PosView(PosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.SaleCompleted += OnSaleCompleted;

        Loaded += async (_, _) =>
        {
            await viewModel.LoadCommand.ExecuteAsync(null);
            BarcodeBox.Focus();
        };
    }

    private async void BarcodeBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await _viewModel.ScanBarcodeCommand.ExecuteAsync(null);
            BarcodeBox.Focus();
        }
    }

    private void OwnerAuthPasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        _viewModel.OwnerAuthPassword = OwnerAuthPasswordBox.Password;
    }

    private async void OnSaleCompleted(int saleId)
    {
        var window = App.AppHost.Services.GetRequiredService<ReceiptPreviewWindow>();
        var owner = App.AppHost.Services.GetRequiredService<MainWindow>();

        if (!ReferenceEquals(window, owner) && owner.IsVisible)
            window.Owner = owner;

        await window.LoadAsync(saleId);
        window.Show();

        BarcodeBox.Focus();
    }
}