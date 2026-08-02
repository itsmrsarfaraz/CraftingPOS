using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using Serilog;

namespace CraftingPOS.Presentation.ViewModels;

public partial class SuppliersViewModel : ObservableObject
{
    private readonly ISupplierService _supplierService;

    public ObservableCollection<SupplierDto> Suppliers { get; } = new();
    public ObservableCollection<PurchaseHistoryItemDto> PurchaseHistory { get; } = new();

    [ObservableProperty]
    private SupplierDto? selectedSupplier;

    // Form fields
    [ObservableProperty] private int formId;
    [ObservableProperty] private string formName = string.Empty;
    [ObservableProperty] private string formContactPerson = string.Empty;
    [ObservableProperty] private string formPhone = string.Empty;
    [ObservableProperty] private string formEmail = string.Empty;
    [ObservableProperty] private string formAddress = string.Empty;
    [ObservableProperty] private string formNotes = string.Empty;
    [ObservableProperty] private string formHeader = "New Supplier";

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool hasError;

    [ObservableProperty] private bool isViewingHistory;
    [ObservableProperty] private string historyHeader = string.Empty;

    public bool HasPurchaseHistory => PurchaseHistory.Count > 0;

    public SuppliersViewModel(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var items = await _supplierService.GetAllAsync();
            Suppliers.Clear();
            foreach (var item in items) Suppliers.Add(item);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load suppliers.");
            SetStatus("Failed to load suppliers.", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedSupplierChanged(SupplierDto? value)
    {
        IsViewingHistory = false;

        if (value == null)
        {
            NewSupplier();
            return;
        }

        FormId = value.Id;
        FormName = value.Name;
        FormContactPerson = value.ContactPerson ?? string.Empty;
        FormPhone = value.Phone ?? string.Empty;
        FormEmail = value.Email ?? string.Empty;
        FormAddress = value.Address ?? string.Empty;
        FormNotes = value.Notes ?? string.Empty;
        FormHeader = $"Edit Supplier — {value.Name}";
        ClearStatus();
    }

    [RelayCommand]
    private void NewSupplier()
    {
        SelectedSupplier = null;
        FormId = 0;
        FormName = string.Empty;
        FormContactPerson = string.Empty;
        FormPhone = string.Empty;
        FormEmail = string.Empty;
        FormAddress = string.Empty;
        FormNotes = string.Empty;
        FormHeader = "New Supplier";
        IsViewingHistory = false;
        ClearStatus();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ClearStatus();

        var dto = new SaveSupplierDto
        {
            Id = FormId,
            Name = FormName,
            ContactPerson = FormContactPerson,
            Phone = FormPhone,
            Email = FormEmail,
            Address = FormAddress,
            Notes = FormNotes
        };

        IsBusy = true;
        try
        {
            var result = await _supplierService.SaveAsync(dto);

            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Failed to save supplier.", true);
                return;
            }

            SetStatus(FormId == 0 ? "Supplier created successfully." : "Supplier updated successfully.", false);
            await LoadAsync();
            NewSupplier();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeactivateAsync(SupplierDto? supplier)
    {
        if (supplier == null) return;

        IsBusy = true;
        try
        {
            var result = await _supplierService.DeactivateAsync(supplier.Id);

            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Failed to deactivate supplier.", true);
                return;
            }

            SetStatus($"Supplier '{supplier.Name}' deactivated.", false);
            await LoadAsync();
            NewSupplier();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ViewHistoryAsync(SupplierDto? supplier)
    {
        if (supplier == null) return;

        IsBusy = true;
        try
        {
            var history = await _supplierService.GetPurchaseHistoryAsync(supplier.Id);

            PurchaseHistory.Clear();
            foreach (var item in history) PurchaseHistory.Add(item);

            HistoryHeader = $"Purchase History — {supplier.Name}";
            IsViewingHistory = true;
            OnPropertyChanged(nameof(HasPurchaseHistory));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CloseHistory()
    {
        IsViewingHistory = false;
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