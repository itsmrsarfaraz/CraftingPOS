using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;

namespace CraftingPOS.Presentation.ViewModels;

public partial class AuditLogsViewModel : ObservableObject
{
    private readonly IAuditLogService _auditLogService;

    public ObservableCollection<AuditLogDto> Logs { get; } = new();

    public List<string> Modules { get; } = new()
    {
        "", AuditModules.Auth, AuditModules.Products, AuditModules.Inventory,
        AuditModules.Discounts, AuditModules.Users, AuditModules.Sales, AuditModules.Backup
    };

    [ObservableProperty] private string? selectedModule;
    [ObservableProperty] private string usernameFilter = string.Empty;
    [ObservableProperty] private string keywordFilter = string.Empty;
    [ObservableProperty] private DateTime? fromDate;
    [ObservableProperty] private DateTime? toDate;

    [ObservableProperty] private bool isBusy;

    public AuditLogsViewModel(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await SearchAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        IsBusy = true;
        try
        {
            var results = await _auditLogService.SearchAsync(new AuditLogSearchDto
            {
                Module = string.IsNullOrWhiteSpace(SelectedModule) ? null : SelectedModule,
                Username = string.IsNullOrWhiteSpace(UsernameFilter) ? null : UsernameFilter,
                Keyword = string.IsNullOrWhiteSpace(KeywordFilter) ? null : KeywordFilter,
                FromDate = FromDate,
                ToDate = ToDate
            });

            Logs.Clear();
            foreach (var log in results) Logs.Add(log);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SelectedModule = null;
        UsernameFilter = string.Empty;
        KeywordFilter = string.Empty;
        FromDate = null;
        ToDate = null;
        await SearchAsync();
    }
}