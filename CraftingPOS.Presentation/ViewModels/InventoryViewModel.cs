using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Enums;
using Serilog;

namespace CraftingPOS.Presentation.ViewModels;

public enum InventoryFilter
{
    All,
    LowStock,
    OutOfStock
}

public partial class InventoryViewModel : ObservableObject
{
    private readonly IInventoryService _inventoryService;

    private List<InventoryItemDto> _allItems = new();

    public ObservableCollection<InventoryItemDto> Items { get; } = new();
    public ObservableCollection<InventoryTransactionDto> HistoryEntries { get; } = new();
    public List<InventoryFilter> Filters { get; } = Enum.GetValues<InventoryFilter>().ToList();

    [ObservableProperty] private InventoryFilter selectedFilter = InventoryFilter.All;
    [ObservableProperty] private InventoryItemDto? selectedItem;

    [ObservableProperty] private bool isViewingHistory;
    [ObservableProperty] private bool isViewingAdjustment;
    [ObservableProperty] private string detailHeader = string.Empty;

    // Adjustment form
    [ObservableProperty] private InventoryTransactionType adjustmentType = InventoryTransactionType.Adjustment;
    [ObservableProperty] private decimal adjustmentQuantity;
    [ObservableProperty] private bool adjustmentIsIncrease = true;
    [ObservableProperty] private string adjustmentNotes = string.Empty;
    [ObservableProperty] private string adjustmentStatusMessage = string.Empty;
    [ObservableProperty] private bool adjustmentHasError;

    public List<InventoryTransactionType> AdjustmentTypes { get; } = new()
    {
        InventoryTransactionType.Adjustment,
        InventoryTransactionType.Damage
    };

    [ObservableProperty] private bool isBusy;

    public bool HasHistoryEntries => HistoryEntries.Count > 0;

    public InventoryViewModel(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            _allItems = await _inventoryService.GetAllAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load inventory.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedFilterChanged(InventoryFilter value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<InventoryItemDto> filtered = SelectedFilter switch
        {
            InventoryFilter.LowStock => _allItems.Where(i => i.Status == StockStatus.Low || i.Status == StockStatus.OutOfStock),
            InventoryFilter.OutOfStock => _allItems.Where(i => i.Status == StockStatus.OutOfStock),
            _ => _allItems
        };

        Items.Clear();
        foreach (var item in filtered) Items.Add(item);
    }

    [RelayCommand]
    private async Task ViewHistoryAsync(InventoryItemDto? item)
    {
        if (item == null) return;

        SelectedItem = item;
        IsBusy = true;
        try
        {
            var history = item.ProductVariantId.HasValue
                ? await _inventoryService.GetHistoryForVariantAsync(item.ProductVariantId.Value)
                : await _inventoryService.GetHistoryForProductAsync(item.ProductId);

            HistoryEntries.Clear();
            foreach (var h in history) HistoryEntries.Add(h);

            DetailHeader = $"Transaction History — {item.DisplayName}";
            IsViewingHistory = true;
            IsViewingAdjustment = false;
            OnPropertyChanged(nameof(HasHistoryEntries));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenAdjustment(InventoryItemDto? item)
    {
        if (item == null) return;

        SelectedItem = item;
        AdjustmentType = InventoryTransactionType.Adjustment;
        AdjustmentQuantity = 0;
        AdjustmentIsIncrease = true;
        AdjustmentNotes = string.Empty;
        ClearAdjustmentStatus();

        DetailHeader = $"Adjust Stock — {item.DisplayName}";
        IsViewingAdjustment = true;
        IsViewingHistory = false;
    }

    [RelayCommand]
    private async Task SaveAdjustmentAsync()
    {
        ClearAdjustmentStatus();

        if (SelectedItem == null)
        {
            SetAdjustmentStatus("No item selected.", true);
            return;
        }

        var dto = new AdjustStockDto
        {
            ProductId = SelectedItem.ProductId,
            ProductVariantId = SelectedItem.ProductVariantId,
            TransactionType = AdjustmentType,
            Quantity = AdjustmentQuantity,
            IsIncrease = AdjustmentIsIncrease,
            Notes = AdjustmentNotes
        };

        IsBusy = true;
        try
        {
            var result = await _inventoryService.AdjustStockAsync(dto);

            if (!result.Success)
            {
                SetAdjustmentStatus(result.ErrorMessage ?? "Failed to adjust stock.", true);
                return;
            }

            SetAdjustmentStatus("Stock adjusted successfully.", false);
            await LoadAsync();
            IsViewingAdjustment = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CloseDetail()
    {
        IsViewingHistory = false;
        IsViewingAdjustment = false;
    }

    private void SetAdjustmentStatus(string message, bool isError)
    {
        AdjustmentStatusMessage = message;
        AdjustmentHasError = isError;
    }

    private void ClearAdjustmentStatus()
    {
        AdjustmentStatusMessage = string.Empty;
        AdjustmentHasError = false;
    }
}