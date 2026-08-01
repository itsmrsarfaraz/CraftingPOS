using System.Windows.Controls;
using CraftingPOS.Presentation.ViewModels;

namespace CraftingPOS.Presentation.Views;

public partial class BackupView : UserControl
{
    public BackupView(BackupViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadCommand.ExecuteAsync(null);
    }
}