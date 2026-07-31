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
    private readonly ICustomerLedgerService _customerLedgerService;

    public ObservableCollection<CustomerDto> Customers { get; } = new();
    public ObservableCollection<SalesHistoryItemDto> SalesHistory { get; } = new();
    public ObservableCollection<CustomerLedgerEntryDto> LedgerEntries { get; } = new();

    [ObservableProperty]
    private CustomerDto? selectedCustomer;

    [ObservableProperty]
    private string searchTerm = string.Empty;

    // Product/Edit form fields
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

    // Panel state: exactly one of these three is true at a time.
    [ObservableProperty] private bool isViewingHistory;
    [ObservableProperty] private bool isViewingLedger;
    [ObservableProperty] private string historyHeader = string.Empty;

    // Ledger (Khata) panel state
    [ObservableProperty] private string ledgerHeader = string.Empty;
    [ObservableProperty] private decimal ledgerOutstandingBalance;
    [ObservableProperty] private decimal paymentAmount;
    [ObservableProperty] private string paymentNotes = string.Empty;
    [ObservableProperty] private string ledgerStatusMessage = string.Empty;
    [ObservableProperty] private bool ledgerHasError;
    private int _ledgerCustomerId;

    public bool HasSalesHistory => SalesHistory.Count > 0;
    public bool HasLedgerEntries => LedgerEntries.Count > 0;

    public CustomersViewModel(ICustomerService customerService, ICustomerLedgerService customerLedgerService)
    {
        _customerService = customerService;
        _customerLedgerService = customerLedgerService;
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
        IsViewingLedger = false;

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
        IsViewingLedger = false;
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
            IsViewingLedger = false;
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

    // ---- Khata (Credit Ledger) ----

    [RelayCommand]
    private async Task ViewLedgerAsync(CustomerDto? customer)
    {
        if (customer == null) return;

        IsBusy = true;
        try
        {
            await LoadLedgerAsync(customer.Id, customer.Name);
            IsViewingLedger = true;
            IsViewingHistory = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadLedgerAsync(int customerId, string customerName)
    {
        _ledgerCustomerId = customerId;

        var ledger = await _customerLedgerService.GetLedgerAsync(customerId);

        LedgerEntries.Clear();
        foreach (var entry in ledger.Entries) LedgerEntries.Add(entry);

        LedgerHeader = $"Khata — {customerName}";
        LedgerOutstandingBalance = ledger.OutstandingBalance;
        PaymentAmount = 0;
        PaymentNotes = string.Empty;
        ClearLedgerStatus();
        OnPropertyChanged(nameof(HasLedgerEntries));
    }

    [RelayCommand]
    private void CloseLedger()
    {
        IsViewingLedger = false;
    }

    [RelayCommand]
    private async Task RecordPaymentAsync()
    {
        ClearLedgerStatus();

        if (_ledgerCustomerId <= 0)
        {
            SetLedgerStatus("No customer selected.", true);
            return;
        }

        var dto = new RecordPaymentDto
        {
            CustomerId = _ledgerCustomerId,
            Amount = PaymentAmount,
            Notes = PaymentNotes
        };

        IsBusy = true;
        try
        {
            var result = await _customerLedgerService.RecordPaymentAsync(dto);

            if (!result.Success)
            {
                SetLedgerStatus(result.ErrorMessage ?? "Failed to record payment.", true);
                return;
            }

            SetLedgerStatus($"Payment of Rs. {dto.Amount:N0} recorded.", false);

            var customerName = LedgerHeader.Replace("Khata — ", "");
            await LoadLedgerAsync(_ledgerCustomerId, customerName);

            // Refresh the grid so the customer's OutstandingBalance column updates too.
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
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

    private void SetLedgerStatus(string message, bool isError)
    {
        LedgerStatusMessage = message;
        LedgerHasError = isError;
    }

    private void ClearLedgerStatus()
    {
        LedgerStatusMessage = string.Empty;
        LedgerHasError = false;
    }
}