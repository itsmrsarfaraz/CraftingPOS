using System.Windows;
using CraftingPOS.Presentation.ViewModels;

namespace CraftingPOS.Presentation.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}