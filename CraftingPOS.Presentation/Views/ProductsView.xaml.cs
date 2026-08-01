using System.Windows.Controls;
using CraftingPOS.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CraftingPOS.Presentation.Views;

public partial class ProductsView : UserControl
{
    private readonly ProductsViewModel _viewModel;

    public ProductsView(ProductsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
        Loaded += async (_, _) => await viewModel.LoadCommand.ExecuteAsync(null);

        _viewModel.PrintLabelRequested += OnPrintLabelRequested;
    }

    private void OnPrintLabelRequested(Application.DTOs.ProductDto product)
    {
        var window = App.AppHost.Services.GetRequiredService<BarcodeLabelWindow>();
        window.Owner = System.Windows.Application.Current.MainWindow;
        window.LoadLabel(product.Name, product.Barcode, product.SellingPrice);
        window.Show();
    }
}