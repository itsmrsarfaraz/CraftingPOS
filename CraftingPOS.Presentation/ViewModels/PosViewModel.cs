using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Enums;
using Serilog;

namespace CraftingPOS.Presentation.ViewModels;

public partial class CartLine : ObservableObject
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal AvailableStock { get; set; }
    public bool HasProductDiscount { get; set; }

    [ObservableProperty] private decimal quantity;
    [ObservableProperty] private decimal unitPrice; // already reflects any active product discount

    public decimal LineTotal => Quantity * UnitPrice;

    partial void OnQuantityChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
}

public partial class PosViewModel : ObservableObject
{
    private readonly ISaleService _saleService;
    private readonly IProductService _productService;
    private readonly IProductVariantService _productVariantService;
    private readonly ICustomerService _customerService;
    private readonly IAuthService _authService;
    private readonly CurrentUserContext _currentUserContext;

    public ObservableCollection<CartLine> Cart { get; } = new();
    public ObservableCollection<ProductDto> SearchResults { get; } = new();
    public ObservableCollection<CustomerDto> Customers { get; } = new();
    public ObservableCollection<ProductVariantDto> VariantPickerOptions { get; } = new();
    public List<PaymentMethod> PaymentMethods { get; } = Enum.GetValues<PaymentMethod>().ToList();
    public List<DiscountType> DiscountTypes { get; } = Enum.GetValues<DiscountType>().ToList();

    [ObservableProperty] private string barcodeInput = string.Empty;
    [ObservableProperty] private string searchTerm = string.Empty;
    [ObservableProperty] private CustomerDto? selectedCustomer;

    [ObservableProperty] private DiscountType cartDiscountType = DiscountType.Flat;
    [ObservableProperty] private decimal cartDiscountValue;
    [ObservableProperty] private PaymentMethod selectedPaymentMethod = PaymentMethod.Cash;
    [ObservableProperty] private decimal amountReceived;
    [ObservableProperty] private string referenceNumber = string.Empty;

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool hasError;

    [ObservableProperty] private string lastReceiptSummary = string.Empty;
    [ObservableProperty] private bool hasCompletedSale;

    [ObservableProperty] private bool isPickingVariant;
    [ObservableProperty] private string variantPickerHeader = string.Empty;

    // Owner authorization panel (discount ceiling exceeded)
    [ObservableProperty] private bool showOwnerAuthPanel;
    [ObservableProperty] private string ownerAuthUsername = string.Empty;
    [ObservableProperty] private string ownerAuthPassword = string.Empty;
    [ObservableProperty] private string ownerAuthMessage = string.Empty;
    private bool _discountOverrideAuthorized;

    public event Action<int>? SaleCompleted;

    public bool IsOwner => _currentUserContext.Session?.RoleName is RoleNames.Owner or RoleNames.SystemAdmin;
    public bool IsCashPayment => SelectedPaymentMethod == PaymentMethod.Cash;
    public bool RequiresReference => SelectedPaymentMethod is PaymentMethod.Card or PaymentMethod.BankTransfer;
    public bool RequiresCustomer => SelectedPaymentMethod == PaymentMethod.Credit;

    public decimal SubTotal => Cart.Sum(c => c.LineTotal);

    public decimal CartDiscountAmount => CartDiscountType == DiscountType.Percentage
        ? SubTotal * CartDiscountValue / 100m
        : CartDiscountValue;

    public decimal GrandTotal => Math.Max(0, SubTotal - CartDiscountAmount);
    public decimal ChangeDue => SelectedPaymentMethod == PaymentMethod.Cash ? Math.Max(0, AmountReceived - GrandTotal) : 0;

    public PosViewModel(
        ISaleService saleService,
        IProductService productService,
        IProductVariantService productVariantService,
        ICustomerService customerService,
        IAuthService authService,
        CurrentUserContext currentUserContext)
    {
        _saleService = saleService;
        _productService = productService;
        _productVariantService = productVariantService;
        _customerService = customerService;
        _authService = authService;
        _currentUserContext = currentUserContext;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var customers = await _customerService.GetAllAsync();
            Customers.Clear();
            foreach (var c in customers) Customers.Add(c);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ScanBarcodeAsync()
    {
        if (string.IsNullOrWhiteSpace(BarcodeInput)) return;

        var code = BarcodeInput.Trim();
        BarcodeInput = string.Empty;

        var lookup = await _saleService.FindByBarcodeAsync(code);
        if (lookup == null)
        {
            SetStatus($"No product found for barcode '{code}'.", true);
            return;
        }

        AddToCart(lookup);
        ClearStatus();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchTerm)) { SearchResults.Clear(); return; }

        var results = await _productService.SearchAsync(SearchTerm);
        SearchResults.Clear();
        foreach (var r in results) SearchResults.Add(r);
    }

    [RelayCommand]
    private async Task AddSearchResultToCartAsync(ProductDto? product)
    {
        if (product == null) return;

        if (product.ProductType == CraftingPOS.Domain.Enums.ProductType.Variable)
        {
            IsBusy = true;
            try
            {
                var variants = await _productVariantService.GetByProductIdAsync(product.Id);
                VariantPickerOptions.Clear();
                foreach (var v in variants) VariantPickerOptions.Add(v);

                if (VariantPickerOptions.Count == 0)
                {
                    SetStatus($"'{product.Name}' has no variants configured yet.", true);
                    return;
                }

                VariantPickerHeader = $"Select a variant — {product.Name}";
                IsPickingVariant = true;
                ClearStatus();
            }
            finally { IsBusy = false; }
            return;
        }

        AddToCart(new CartItemLookupDto
        {
            ProductId = product.Id,
            ProductVariantId = null,
            DisplayName = product.Name,
            OriginalUnitPrice = product.SellingPrice,
            EffectiveUnitPrice = product.EffectiveSellingPrice,
            UnitCost = product.CostPrice,
            AvailableStock = product.CurrentStock
        });

        ClearStatus();
    }

    [RelayCommand]
    private void SelectVariantForCart(ProductVariantDto? variant)
    {
        if (variant == null) return;

        AddToCart(new CartItemLookupDto
        {
            ProductId = variant.ProductId,
            ProductVariantId = variant.Id,
            DisplayName = $"{variant.ProductName} — {variant.VariantName}",
            OriginalUnitPrice = variant.SellingPrice,
            EffectiveUnitPrice = variant.SellingPrice, // variants don't carry product-level discounts in V1
            UnitCost = variant.CostPrice,
            AvailableStock = variant.CurrentStock
        });

        IsPickingVariant = false;
        VariantPickerOptions.Clear();
        ClearStatus();
    }

    [RelayCommand]
    private void CancelVariantPicker()
    {
        IsPickingVariant = false;
        VariantPickerOptions.Clear();
    }

    private void AddToCart(CartItemLookupDto lookup)
    {
        var existing = Cart.FirstOrDefault(c => c.ProductId == lookup.ProductId && c.ProductVariantId == lookup.ProductVariantId);

        if (existing != null)
        {
            if (existing.Quantity + 1 > lookup.AvailableStock)
            {
                SetStatus($"Only {lookup.AvailableStock} unit(s) of '{lookup.DisplayName}' available.", true);
                return;
            }
            existing.Quantity += 1;
        }
        else
        {
            if (lookup.AvailableStock <= 0)
            {
                SetStatus($"'{lookup.DisplayName}' is out of stock.", true);
                return;
            }

            Cart.Add(new CartLine
            {
                ProductId = lookup.ProductId,
                ProductVariantId = lookup.ProductVariantId,
                DisplayName = lookup.DisplayName,
                UnitCost = lookup.UnitCost,
                AvailableStock = lookup.AvailableStock,
                HasProductDiscount = lookup.EffectiveUnitPrice < lookup.OriginalUnitPrice,
                Quantity = 1,
                UnitPrice = lookup.EffectiveUnitPrice
            });
        }

        RefreshTotals();
    }

    [RelayCommand]
    private void IncreaseQuantity(CartLine? line)
    {
        if (line == null) return;
        if (line.Quantity + 1 > line.AvailableStock)
        {
            SetStatus($"Only {line.AvailableStock} unit(s) of '{line.DisplayName}' available.", true);
            return;
        }
        line.Quantity += 1;
        RefreshTotals();
    }

    [RelayCommand]
    private void DecreaseQuantity(CartLine? line)
    {
        if (line == null) return;
        if (line.Quantity <= 1) Cart.Remove(line);
        else line.Quantity -= 1;
        RefreshTotals();
    }

    [RelayCommand]
    private void RemoveFromCart(CartLine? line)
    {
        if (line == null) return;
        Cart.Remove(line);
        RefreshTotals();
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(CartDiscountAmount));
        OnPropertyChanged(nameof(GrandTotal));
        OnPropertyChanged(nameof(ChangeDue));
    }

    partial void OnCartDiscountTypeChanged(DiscountType value) { RefreshTotals(); _discountOverrideAuthorized = false; ShowOwnerAuthPanel = false; }
    partial void OnCartDiscountValueChanged(decimal value) { RefreshTotals(); _discountOverrideAuthorized = false; ShowOwnerAuthPanel = false; }
    partial void OnAmountReceivedChanged(decimal value) => OnPropertyChanged(nameof(ChangeDue));

    partial void OnSelectedPaymentMethodChanged(PaymentMethod value)
    {
        OnPropertyChanged(nameof(IsCashPayment));
        OnPropertyChanged(nameof(RequiresReference));
        OnPropertyChanged(nameof(RequiresCustomer));
        OnPropertyChanged(nameof(ChangeDue));
    }

    [RelayCommand]
    private async Task AuthorizeOwnerOverrideAsync()
    {
        var verified = await _authService.VerifyManagerCredentialsAsync(OwnerAuthUsername, OwnerAuthPassword);

        if (!verified)
        {
            OwnerAuthMessage = "Invalid Owner credentials.";
            return;
        }

        _discountOverrideAuthorized = true;
        ShowOwnerAuthPanel = false;
        OwnerAuthUsername = string.Empty;
        OwnerAuthPassword = string.Empty;
        OwnerAuthMessage = string.Empty;

        SetStatus("Owner authorization confirmed. Click Complete Sale again to finish.", false);
    }

    [RelayCommand]
    private void CancelOwnerAuth()
    {
        ShowOwnerAuthPanel = false;
        OwnerAuthUsername = string.Empty;
        OwnerAuthPassword = string.Empty;
        OwnerAuthMessage = string.Empty;
    }

    [RelayCommand]
    private async Task CompleteSaleAsync()
    {
        ClearStatus();
        HasCompletedSale = false;

        if (Cart.Count == 0)
        {
            SetStatus("Add at least one item to the cart before checking out.", true);
            return;
        }

        if (RequiresCustomer && SelectedCustomer == null)
        {
            SetStatus("Select a customer for a credit sale.", true);
            return;
        }

        if (IsCashPayment && AmountReceived < GrandTotal)
        {
            SetStatus($"Amount received (Rs. {AmountReceived:N0}) is less than the total due (Rs. {GrandTotal:N0}).", true);
            return;
        }

        var belowCostItems = Cart.Where(c => c.UnitPrice < c.UnitCost).ToList();
        var belowCostConfirmed = false;

        if (belowCostItems.Count > 0)
        {
            if (!IsOwner)
            {
                SetStatus("A cashier cannot sell a product below its purchase price. Ask an Owner to complete this sale.", true);
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"{belowCostItems.Count} item(s) are priced below their purchase cost. Sell anyway?",
                "Confirm Below-Cost Sale",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes)
            {
                SetStatus("Sale cancelled.", true);
                return;
            }

            belowCostConfirmed = true;
        }

        var dto = new CompleteSaleDto
        {
            CustomerId = SelectedCustomer?.Id,
            Items = Cart.Select(c => new CompleteSaleItemDto
            {
                ProductId = c.ProductId,
                ProductVariantId = c.ProductVariantId,
                Quantity = c.Quantity,
                UnitPrice = c.UnitPrice,
                UnitCost = c.UnitCost
            }).ToList(),
            CartDiscountType = CartDiscountType,
            CartDiscountValue = CartDiscountValue,
            DiscountOverrideAuthorized = IsOwner || _discountOverrideAuthorized,
            BelowCostConfirmed = belowCostConfirmed,
            PaymentMethod = SelectedPaymentMethod,
            AmountReceived = IsCashPayment ? AmountReceived : GrandTotal,
            ReferenceNumber = RequiresReference ? ReferenceNumber : null,
            Notes = null
        };

        IsBusy = true;
        try
        {
            var result = await _saleService.CompleteSaleAsync(dto);

            if (!result.Success)
            {
                var message = result.ErrorMessage ?? "Failed to complete sale.";

                if (message.StartsWith("OWNER_AUTH_REQUIRED:"))
                {
                    ShowOwnerAuthPanel = true;
                    OwnerAuthMessage = message.Replace("OWNER_AUTH_REQUIRED:", "").Trim();
                    SetStatus("Owner authorization required for this discount.", true);
                    return;
                }

                SetStatus(message, true);
                return;
            }

            var sale = result.Data!;
            LastReceiptSummary = sale.ChangeDue.HasValue
                ? $"Invoice {sale.InvoiceNumber} — Total: Rs. {sale.GrandTotal:N0} — Change Due: Rs. {sale.ChangeDue:N0}"
                : $"Invoice {sale.InvoiceNumber} — Total: Rs. {sale.GrandTotal:N0} — Payment: {SelectedPaymentMethod}";

            HasCompletedSale = true;
            SetStatus("Sale completed successfully.", false);

            SaleCompleted?.Invoke(sale.SaleId);

            ResetCartForNextSale();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetCartForNextSale()
    {
        Cart.Clear();
        SearchResults.Clear();
        VariantPickerOptions.Clear();
        IsPickingVariant = false;
        SelectedCustomer = null;
        CartDiscountType = DiscountType.Flat;
        CartDiscountValue = 0;
        AmountReceived = 0;
        ReferenceNumber = string.Empty;
        SearchTerm = string.Empty;
        _discountOverrideAuthorized = false;
        ShowOwnerAuthPanel = false;
        RefreshTotals();
    }

    private void SetStatus(string message, bool isError) { StatusMessage = message; HasError = isError; }
    private void ClearStatus() { StatusMessage = string.Empty; HasError = false; }
}