using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using Serilog;

namespace CraftingPOS.Presentation.ViewModels;

public partial class PurchaseLineItem : ObservableObject
{
    public ProductDto? Product { get; set; }
    public ProductVariantDto? Variant { get; set; }

    [ObservableProperty] private decimal quantity = 1;
    [ObservableProperty] private decimal unitCost;

    public string DisplayName => Variant != null
        ? $"{Product?.Name} — {Variant.VariantName}"
        : Product?.Name ?? string.Empty;

    public decimal LineTotal => Quantity * UnitCost;

    partial void OnQuantityChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnUnitCostChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
}

public partial class PurchasesViewModel : ObservableObject
{
    private readonly IPurchaseService _purchaseService;
    private readonly ISupplierService _supplierService;
    private readonly IProductService _productService;
    private readonly IProductVariantService _productVariantService;

    public ObservableCollection<PurchaseDto> Purchases { get; } = new();
    public ObservableCollection<SupplierDto> Suppliers { get; } = new();
    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<ProductVariantDto> AvailableVariants { get; } = new();
    public ObservableCollection<PurchaseLineItem> Items { get; } = new();

    [ObservableProperty] private SupplierDto? formSupplier;
    [ObservableProperty] private string formInvoiceNumber = string.Empty;
    [ObservableProperty] private DateTime formPurchaseDate = DateTime.Now;
    [ObservableProperty] private string formNotes = string.Empty;
    [ObservableProperty] private decimal formDiscountAmount;

    [ObservableProperty] private ProductDto? selectedProductToAdd;
    [ObservableProperty] private ProductVariantDto? selectedVariantToAdd;
    [ObservableProperty] private decimal newItemQuantity = 1;
    [ObservableProperty] private decimal newItemUnitCost;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool hasError;

    public decimal SubTotal => Items.Sum(i => i.LineTotal);
    public decimal GrandTotal => SubTotal - FormDiscountAmount;

    public bool SelectedProductIsVariable => SelectedProductToAdd?.ProductType == Domain.Enums.ProductType.Variable;

    public PurchasesViewModel(
        IPurchaseService purchaseService,
        ISupplierService supplierService,
        IProductService productService,
        IProductVariantService productVariantService)
    {
        _purchaseService = purchaseService;
        _supplierService = supplierService;
        _productService = productService;
        _productVariantService = productVariantService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var suppliers = await _supplierService.GetAllAsync();
            Suppliers.Clear();
            foreach (var s in suppliers) Suppliers.Add(s);

            var products = await _productService.GetAllAsync();
            Products.Clear();
            foreach (var p in products) Products.Add(p);

            var purchases = await _purchaseService.GetAllAsync();
            Purchases.Clear();
            foreach (var p in purchases) Purchases.Add(p);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load purchases data.");
            SetStatus("Failed to load data.", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedProductToAddChanged(ProductDto? value)
    {
        OnPropertyChanged(nameof(SelectedProductIsVariable));
        AvailableVariants.Clear();
        SelectedVariantToAdd = null;

        if (value == null) return;

        NewItemUnitCost = value.CostPrice;

        if (value.ProductType == Domain.Enums.ProductType.Variable)
        {
            _ = LoadVariantsForProductAsync(value.Id);
        }
    }

    private async Task LoadVariantsForProductAsync(int productId)
    {
        var variants = await _productVariantService.GetByProductIdAsync(productId);
        AvailableVariants.Clear();
        foreach (var v in variants) AvailableVariants.Add(v);
    }

    partial void OnSelectedVariantToAddChanged(ProductVariantDto? value)
    {
        if (value != null)
        {
            NewItemUnitCost = value.CostPrice;
        }
    }

    [RelayCommand]
    private void AddItem()
    {
        if (SelectedProductToAdd == null)
        {
            SetStatus("Select a product to add.", true);
            return;
        }

        if (SelectedProductIsVariable && SelectedVariantToAdd == null)
        {
            SetStatus("Select a variant for this variable product.", true);
            return;
        }

        if (NewItemQuantity <= 0)
        {
            SetStatus("Quantity must be greater than zero.", true);
            return;
        }

        Items.Add(new PurchaseLineItem
        {
            Product = SelectedProductToAdd,
            Variant = SelectedProductIsVariable ? SelectedVariantToAdd : null,
            Quantity = NewItemQuantity,
            UnitCost = NewItemUnitCost
        });

        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(GrandTotal));

        SelectedProductToAdd = null;
        SelectedVariantToAdd = null;
        NewItemQuantity = 1;
        NewItemUnitCost = 0;
        ClearStatus();
    }

    [RelayCommand]
    private void RemoveItem(PurchaseLineItem? item)
    {
        if (item == null) return;
        Items.Remove(item);
        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(GrandTotal));
    }

    partial void OnFormDiscountAmountChanged(decimal value) => OnPropertyChanged(nameof(GrandTotal));

    [RelayCommand]
    private async Task SavePurchaseAsync()
    {
        ClearStatus();

        if (FormSupplier == null)
        {
            SetStatus("Please select a supplier.", true);
            return;
        }

        if (string.IsNullOrWhiteSpace(FormInvoiceNumber))
        {
            SetStatus("Invoice number is required.", true);
            return;
        }

        if (Items.Count == 0)
        {
            SetStatus("Add at least one item to the purchase.", true);
            return;
        }

        var dto = new SavePurchaseDto
        {
            SupplierId = FormSupplier.Id,
            InvoiceNumber = FormInvoiceNumber,
            PurchaseDate = FormPurchaseDate,
            DiscountAmount = FormDiscountAmount,
            Notes = FormNotes,
            Items = Items.Select(i => new SavePurchaseItemDto
            {
                ProductId = i.Product!.Id,
                ProductVariantId = i.Variant?.Id,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost
            }).ToList()
        };

        IsBusy = true;
        try
        {
            var result = await _purchaseService.SaveAsync(dto);

            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Failed to save purchase.", true);
                return;
            }

            SetStatus($"Purchase '{FormInvoiceNumber}' recorded and inventory updated.", false);

            FormSupplier = null;
            FormInvoiceNumber = string.Empty;
            FormPurchaseDate = DateTime.Now;
            FormNotes = string.Empty;
            FormDiscountAmount = 0;
            Items.Clear();
            OnPropertyChanged(nameof(SubTotal));
            OnPropertyChanged(nameof(GrandTotal));

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
}