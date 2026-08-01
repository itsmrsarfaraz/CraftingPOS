using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using Microsoft.Win32;
using Serilog;

namespace CraftingPOS.Presentation.ViewModels;

public partial class BackupViewModel : ObservableObject
{
    private readonly IBackupService _backupService;

    public ObservableCollection<BackupInfoDto> Backups { get; } = new();

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool hasError;

    public BackupInfoDto? LatestBackup => Backups.FirstOrDefault();

    public BackupViewModel(IBackupService backupService)
    {
        _backupService = backupService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var backups = await _backupService.ListBackupsAsync();
            Backups.Clear();
            foreach (var b in backups) Backups.Add(b);
            OnPropertyChanged(nameof(LatestBackup));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        ClearStatus();
        IsBusy = true;
        try
        {
            var result = await _backupService.CreateBackupAsync();
            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Backup failed.", true);
                return;
            }

            SetStatus($"Backup created: {result.Data!.FileName}", false);
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        ClearStatus();

        var dialog = new OpenFileDialog { Filter = "CraftingPOS Backup|*.zip" };
        if (dialog.ShowDialog() != true) return;

        var confirm = System.Windows.MessageBox.Show(
            "Restoring will replace the current database with the selected backup. " +
            "The application will restart. Continue?",
            "Confirm Restore",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            var result = await _backupService.RestoreBackupAsync(dialog.FileName);

            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Restore failed.", true);
                return;
            }

            SetStatus("Restore successful. Restarting application...", false);
            RestartApplication();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static void RestartApplication()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (exePath != null)
        {
            Process.Start(exePath);
        }
        System.Windows.Application.Current.Shutdown();
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        HasError = isError;
    }

    private void ClearStatus()
    {
        StatusMessage = string.Empty;
        HasError = false;
    }
}