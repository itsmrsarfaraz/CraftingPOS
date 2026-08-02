using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Licensing;
using Microsoft.Win32;

namespace CraftingPOS.Presentation.ViewModels;

public partial class LicenseViewModel : ObservableObject
{
    private readonly LicenseManager _licenseManager;

    [ObservableProperty] private string machineFingerprint = string.Empty;
    [ObservableProperty] private string businessName = string.Empty;
    [ObservableProperty] private string licenseType = string.Empty;
    [ObservableProperty] private string issuedAt = string.Empty;
    [ObservableProperty] private string expiresAt = string.Empty;
    [ObservableProperty] private bool isValid;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool hasError;

    public LicenseViewModel(LicenseManager licenseManager)
    {
        _licenseManager = licenseManager;
        MachineFingerprint = _licenseManager.CurrentMachineFingerprint;
        Refresh();
    }

    private void Refresh()
    {
        var result = _licenseManager.Validate();
        IsValid = result.IsValid;

        if (result.IsValid && result.Data != null)
        {
            BusinessName = result.Data.BusinessName;
            LicenseType = result.Data.LicenseType.ToString();
            IssuedAt = result.Data.IssuedAt.ToLocalTime().ToString("dd MMM yyyy");
            ExpiresAt = result.Data.ExpiresAt.HasValue
                ? result.Data.ExpiresAt.Value.ToLocalTime().ToString("dd MMM yyyy")
                : "Never (Lifetime)";
            HasError = false;
            StatusMessage = string.Empty;
        }
        else
        {
            BusinessName = "Not Activated";
            LicenseType = "-";
            IssuedAt = "-";
            ExpiresAt = "-";
            StatusMessage = result.ErrorMessage ?? string.Empty;
            HasError = true;
        }
    }

    [RelayCommand]
    private void CopyFingerprint()
    {
        System.Windows.Clipboard.SetText(MachineFingerprint);
        StatusMessage = "Machine ID copied to clipboard.";
        HasError = false;
    }

    [RelayCommand]
    private void ImportLicense()
    {
        var dialog = new OpenFileDialog { Filter = "CraftingPOS License|*.dat;*.json|All Files|*.*" };
        if (dialog.ShowDialog() != true) return;

        var result = _licenseManager.ActivateFromFile(dialog.FileName);
        StatusMessage = result.IsValid ? "License imported successfully." : result.ErrorMessage ?? "Import failed.";
        HasError = !result.IsValid;

        Refresh();
    }
}