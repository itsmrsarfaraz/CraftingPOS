using System.Windows;
using CraftingPOS.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CraftingPOS.Presentation.Views;

public partial class LoginView : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.LoginSucceeded += OnLoginSucceeded;

        Loaded += (_, _) => UsernameBox.Focus();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.AttemptLoginAsync(PasswordBox.Password);
    }

    private async void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            await _viewModel.AttemptLoginAsync(PasswordBox.Password);
        }
    }

    private void OnLoginSucceeded()
    {
        // Anti-piracy Layer 5: re-validate at login, not just at startup.
        if (!App.LicenseManagerInstance.QuickCheckIsValid())
        {
            var reason = App.LicenseManagerInstance.LastResult?.ErrorMessage ?? "License is no longer valid.";
            MessageBox.Show(
                $"CraftingPOS cannot continue: {reason}\n\nPlease contact support to renew your license.",
                "License Invalid",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            System.Windows.Application.Current.Shutdown();
            return;
        }

        var mainWindow = App.AppHost.Services.GetRequiredService<MainWindow>();
        System.Windows.Application.Current.MainWindow = mainWindow;
        mainWindow.Show();
        Close();
    }
}