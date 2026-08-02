using System.Windows;
using CraftingPOS.Presentation.ViewModels;

namespace CraftingPOS.Presentation.Views;

public partial class ActivationWindow : Window
{
    public ActivationWindow(ActivationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}