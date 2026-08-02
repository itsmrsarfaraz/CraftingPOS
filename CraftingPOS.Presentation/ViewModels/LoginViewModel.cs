using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Application.Interfaces;

namespace CraftingPOS.Presentation.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isBusy;

    public event Action? LoginSucceeded;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task AttemptLoginAsync(string password)
    {
        HasError = false;
        ErrorMessage = string.Empty;
        IsBusy = true;

        try
        {
            var result = await _authService.LoginAsync(Username, password);

            if (!result.Success)
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "Login failed.";
                return;
            }

            LoginSucceeded?.Invoke();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Exit()
    {
        System.Windows.Application.Current.Shutdown();
    }
}