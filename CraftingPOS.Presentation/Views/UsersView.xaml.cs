using System.Windows.Controls;
using CraftingPOS.Presentation.ViewModels;

namespace CraftingPOS.Presentation.Views;

public partial class UsersView : UserControl
{
    public UsersView(UsersViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadCommand.ExecuteAsync(null);
    }
}