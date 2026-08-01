using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using Serilog;

namespace CraftingPOS.Presentation.ViewModels;

public partial class UsersViewModel : ObservableObject
{
    private readonly IUserManagementService _userManagementService;
    private readonly IDiscountSettingsService _discountSettingsService;

    public ObservableCollection<UserAccountDto> Users { get; } = new();
    public List<string> AssignableRoles { get; private set; } = new();

    [ObservableProperty] private string newUsername = string.Empty;
    [ObservableProperty] private string newFullName = string.Empty;
    [ObservableProperty] private string newPassword = string.Empty;
    [ObservableProperty] private string? selectedRole;

    [ObservableProperty] private decimal maxCashierDiscountPercent;
    [ObservableProperty] private decimal maxCashierDiscountFlat;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string limitsStatusMessage = string.Empty;
    [ObservableProperty] private bool limitsHasError;

    public UsersViewModel(IUserManagementService userManagementService, IDiscountSettingsService discountSettingsService)
    {
        _userManagementService = userManagementService;
        _discountSettingsService = discountSettingsService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            AssignableRoles = _userManagementService.GetAssignableRoles();
            OnPropertyChanged(nameof(AssignableRoles));

            var users = await _userManagementService.GetAllAsync();
            Users.Clear();
            foreach (var u in users) Users.Add(u);

            var limits = await _discountSettingsService.GetAsync();
            MaxCashierDiscountPercent = limits.MaxCashierDiscountPercent;
            MaxCashierDiscountFlat = limits.MaxCashierDiscountFlat;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load user management data.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateUserAsync()
    {
        ClearStatus();

        if (string.IsNullOrWhiteSpace(SelectedRole))
        {
            SetStatus("Select a role.", true);
            return;
        }

        var dto = new CreateUserAccountDto
        {
            Username = NewUsername,
            FullName = NewFullName,
            Password = NewPassword,
            RoleName = SelectedRole
        };

        IsBusy = true;
        try
        {
            var result = await _userManagementService.CreateAsync(dto);
            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Failed to create user.", true);
                return;
            }

            SetStatus($"Account '{NewUsername}' created.", false);
            NewUsername = string.Empty;
            NewFullName = string.Empty;
            NewPassword = string.Empty;
            SelectedRole = null;

            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeactivateUserAsync(UserAccountDto? user)
    {
        if (user == null) return;

        IsBusy = true;
        try
        {
            var result = await _userManagementService.DeactivateAsync(user.Id);
            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Failed to deactivate user.", true);
                return;
            }

            SetStatus($"User '{user.Username}' deactivated.", false);
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ResetPasswordAsync(UserAccountDto? user)
    {
        if (user == null) return;

        var newPassword = Microsoft.VisualBasic.Interaction.InputBox(
            $"Enter a new password for '{user.Username}':", "Reset Password", "");

        if (string.IsNullOrWhiteSpace(newPassword)) return;

        IsBusy = true;
        try
        {
            var result = await _userManagementService.ResetPasswordAsync(new ResetPasswordDto
            {
                UserId = user.Id,
                NewPassword = newPassword
            });

            SetStatus(result.Success ? $"Password reset for '{user.Username}'." : result.ErrorMessage ?? "Failed.", !result.Success);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveLimitsAsync()
    {
        ClearLimitsStatus();

        IsBusy = true;
        try
        {
            var result = await _discountSettingsService.SaveAsync(new DiscountSettingsDto
            {
                MaxCashierDiscountPercent = MaxCashierDiscountPercent,
                MaxCashierDiscountFlat = MaxCashierDiscountFlat
            });

            SetLimitsStatus(result.Success ? "Discount limits saved." : result.ErrorMessage ?? "Failed.", !result.Success);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetStatus(string message, bool isError) { StatusMessage = message; HasError = isError; }
    private void ClearStatus() { StatusMessage = string.Empty; HasError = false; }
    private void SetLimitsStatus(string message, bool isError) { LimitsStatusMessage = message; LimitsHasError = isError; }
    private void ClearLimitsStatus() { LimitsStatusMessage = string.Empty; LimitsHasError = false; }
}