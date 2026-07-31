using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using Serilog;

namespace CraftingPOS.Presentation.ViewModels;

public partial class CustomersViewModel : ObservableObject
{
    private readonly ICustomerService _customerService;

    public ObservableCollection<CustomerDto> Customers { get; } = new();
    public ObservableCollection<SalesHistoryItemDto> SalesHistory { get; } = new();

    [ObservableProperty]
    private CustomerDto? selectedCustomer;

    [ObservableProperty]
    private string searchTerm = string.Empty;

    // Form fields
    [ObservableProperty] private int formId;
    [ObservableProperty] private string formName = string.Empty;
    [ObservableProperty] private string formPhone = string.Empty;
    [ObservableProperty] private string formEmail = string.Empty;
    [ObservableProperty] private string formAddress = string.Empty;
    [ObservableProperty] private string formNotes = string.Empty;
    [ObservableProperty] private string formHeader = "New Customer";

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool hasError;

    [ObservableProperty] private bool isViewingHistory;
    [ObservableProperty] private string historyHeader = string.Empty;

    public bool HasSalesHistory => SalesHistory.Count > 0;

    public CustomersViewModel(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var items = await _customerService.GetAllAsync();
            Customers.Clear();
            foreach (var item in items) Customers.Add(item);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load customers.");
            SetStatus("Failed to load customers.", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        IsBusy = true;
        try
        {
            var results = await _customerService.SearchAsync(SearchTerm);
            Customers.Clear();
            foreach (var c in results) Customers.Add(c);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedCustomerChanged(CustomerDto? value)
    {
        IsViewingHistory = false;

        if (value == null)
        {
            NewCustomer();
            return;
        }

        FormId = value.Id;
        FormName = value.Name;
        FormPhone = value.Phone ?? string.Empty;
        FormEmail = value.Email ?? string.Empty;
        FormAddress = value.Address ?? string.Empty;
        FormNotes = value.Notes ?? string.Empty;
        FormHeader = $"Edit Customer — {value.Name}";
        ClearStatus();
    }

    [RelayCommand]
    private void NewCustomer()
    {
        SelectedCustomer = null;
        FormId = 0;
        FormName = string.Empty;
        FormPhone = string.Empty;
        FormEmail = string.Empty;
        FormAddress = string.Empty;
        FormNotes = string.Empty;
        FormHeader = "New Customer";
        IsViewingHistory = false;
        ClearStatus();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ClearStatus();

        var dto = new SaveCustomerDto
        {
            Id = FormId,
            Name = FormName,
            Phone = FormPhone,
            Email = FormEmail,
            Address = FormAddress,
            Notes = FormNotes
        };

        IsBusy = true;
        try
        {
            var result = await _customerService.SaveAsync(dto);

            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Failed to save customer.", true);
                return;
            }

            SetStatus(FormId == 0 ? "Customer created successfully." : "Customer updated successfully.", false);
            await LoadAsync();
            NewCustomer();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeactivateAsync(CustomerDto? customer)
    {
        if (customer == null) return;

        IsBusy = true;
        try
        {
            var result = await _customerService.DeactivateAsync(customer.Id);

            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Failed to deactivate customer.", true);
                return;
            }

            SetStatus($"Customer '{customer.Name}' deactivated.", false);
            await LoadAsync();
            NewCustomer();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ViewHistoryAsync(CustomerDto? customer)
    {
        if (customer == null) return;

        IsBusy = true;
        try
        {
            var history = await _customerService.GetSalesHistoryAsync(customer.Id);

            SalesHistory.Clear();
            foreach (var item in history) SalesHistory.Add(item);

            HistoryHeader = $"Purchase History — {customer.Name}";
            IsViewingHistory = true;
            OnPropertyChanged(nameof(HasSalesHistory));
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