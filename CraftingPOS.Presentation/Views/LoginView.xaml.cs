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
        var mainWindow = App.AppHost.Services.GetRequiredService<MainWindow>();
        System.Windows.Application.Current.MainWindow = mainWindow; // keep WPF's tracking accurate
        mainWindow.Show();
        Close();
    }
}