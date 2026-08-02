using System.Windows.Controls;
using CraftingPOS.Presentation.ViewModels;

namespace CraftingPOS.Presentation.Views;

public partial class LicenseView : UserControl
{
    public LicenseView(LicenseViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}