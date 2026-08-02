using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Licensing;
using Microsoft.Win32;

namespace CraftingPOS.Presentation.ViewModels;

public partial class ActivationViewModel : ObservableObject
{
    private readonly LicenseManager _licenseManager;

    [ObservableProperty] private string machineFingerprint = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool hasError;

    public event Action? ActivationSucceeded;

    public ActivationViewModel(LicenseManager licenseManager)
    {
        _licenseManager = licenseManager;
        MachineFingerprint = _licenseManager.CurrentMachineFingerprint;
    }

    [RelayCommand]
    private void CopyFingerprint()
    {
        System.Windows.Clipboard.SetText(MachineFingerprint);
        StatusMessage = "Machine ID copied. Send this to CraftingPOS support to receive your license file.";
        HasError = false;
    }

    [RelayCommand]
    private void ImportLicense()
    {
        var dialog = new OpenFileDialog { Filter = "CraftingPOS License|*.dat;*.json|All Files|*.*" };
        if (dialog.ShowDialog() != true) return;

        var result = _licenseManager.ActivateFromFile(dialog.FileName);

        if (!result.IsValid)
        {
            StatusMessage = result.ErrorMessage ?? "Activation failed.";
            HasError = true;
            return;
        }

        StatusMessage = $"Activated successfully for '{result.Data!.BusinessName}'.";
        HasError = false;

        ActivationSucceeded?.Invoke();
    }

    [RelayCommand]
    private void Exit()
    {
        System.Windows.Application.Current.Shutdown();
    }
}