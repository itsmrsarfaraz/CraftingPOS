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
        var owner = App.AppHost.Services.GetRequiredService<MainWindow>();

        // Only set Owner if this isn't somehow the same window and the owner is visible.
        if (!ReferenceEquals(window, owner) && owner.IsVisible)
        {
            window.Owner = owner;
        }

        window.LoadLabel(product.Name, product.Barcode, product.SellingPrice);
        window.Show();
    }
}