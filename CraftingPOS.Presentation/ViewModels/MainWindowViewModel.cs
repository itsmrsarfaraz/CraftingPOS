using CommunityToolkit.Mvvm.ComponentModel;
using CraftingPOS.Application.Common;

namespace CraftingPOS.Presentation.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string welcomeMessage = string.Empty;

    public MainWindowViewModel(CurrentUserContext currentUserContext)
    {
        var session = currentUserContext.Session;

        WelcomeMessage = session != null
            ? $"Welcome, {session.FullName} ({session.RoleName})"
            : "Welcome";
    }
}