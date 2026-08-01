using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Enums;
using Microsoft.Win32;
using Serilog;

namespace CraftingPOS.Presentation.ViewModels;

public partial class ProductsViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IBrandService _brandService;
    private readonly IProductVariantService _productVariantService;
    private readonly CurrentUserContext _currentUserContext;

    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<CategoryDto> Categories { get; } = new();
    public ObservableCollection<BrandDto> Brands { get; } = new();
    public ObservableCollection<ProductVariantDto> Variants { get; } = new();
    public List<ProductType> ProductTypes { get; } = Enum.GetValues<ProductType>().ToList();

    [ObservableProperty] private ProductDto? selectedProduct;
    [ObservableProperty] private string searchTerm = string.Empty;

    [ObservableProperty] private int formId;
    [ObservableProperty] private CategoryDto? formCategory;
    [ObservableProperty] private BrandDto? formBrand;
    [ObservableProperty] private string formBarcode = string.Empty;
    [ObservableProperty] private string formSku = string.Empty;
    [ObservableProperty] private string formName = string.Empty;
    [ObservableProperty] private string formDescription = string.Empty;
    [ObservableProperty] private ProductType formProductType = ProductType.Standard;
    [ObservableProperty] private decimal formCostPrice;
    [ObservableProperty] private decimal formSellingPrice;
    [ObservableProperty] private decimal formCurrentStock;
    [ObservableProperty] private decimal formMinimumStock;
    [ObservableProperty] private string? formImageSourcePath;
    [ObservableProperty] private string formHeader = "New Product";

    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool hasError;

    [ObservableProperty] private ProductVariantDto? selectedVariant;
    [ObservableProperty] private int variantFormId;
    [ObservableProperty] private string variantName = string.Empty;
    [ObservableProperty] private string variantBarcode = string.Empty;
    [ObservableProperty] private string variantSku = string.Empty;
    [ObservableProperty] private decimal variantCostPrice;
    [ObservableProperty] private decimal variantSellingPrice;
    [ObservableProperty] private decimal variantCurrentStock;
    [ObservableProperty] private decimal variantMinimumStock;
    [ObservableProperty] private string variantFormHeader = "New Variant";
    [ObservableProperty] private string variantStatusMessage = string.Empty;
    [ObservableProperty] private bool variantHasError;
    private readonly IProductDiscountService _productDiscountService;

    [ObservableProperty] private CraftingPOS.Domain.Enums.DiscountType discountType = CraftingPOS.Domain.Enums.DiscountType.Percentage;
    [ObservableProperty] private decimal discountValue;
    [ObservableProperty] private bool hasActiveDiscount;

    public List<CraftingPOS.Domain.Enums.DiscountType> DiscountTypes { get; } =
        Enum.GetValues<CraftingPOS.Domain.Enums.DiscountType>().ToList();

    public bool IsOwner => _currentUserContext.Session?.RoleName == RoleNames.Owner;
    public bool IsVariableProductType => FormProductType == ProductType.Variable;
    public bool ShowVariantsPanel => FormProductType == ProductType.Variable && FormId > 0;
    public bool ShowVariantsSaveFirstMessage => FormProductType == ProductType.Variable && FormId == 0;

    public ProductsViewModel(
        IProductService productService,
        ICategoryService categoryService,
        IBrandService brandService,
        IProductVariantService productVariantService,
        CurrentUserContext currentUserContext)
    {
        _productService = productService;
        _categoryService = categoryService;
        _brandService = brandService;
        _productVariantService = productVariantService;
        _currentUserContext = currentUserContext;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var categories = await _categoryService.GetAllAsync();
            Categories.Clear();
            foreach (var c in categories) Categories.Add(c);

            var brands = await _brandService.GetAllAsync();
            Brands.Clear();
            foreach (var b in brands) Brands.Add(b);

            var products = await _productService.GetAllAsync();
            Products.Clear();
            foreach (var p in products) Products.Add(p);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load products.");
            SetStatus("Failed to load products.", true);
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
            var results = await _productService.SearchAsync(SearchTerm);
            Products.Clear();
            foreach (var p in results) Products.Add(p);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial async void OnSelectedProductChanged(ProductDto? value)
    {
        if (value == null)
        {
            NewProduct();
            return;
        }

        var discount = await _productDiscountService.GetForProductAsync(value.Id);
        HasActiveDiscount = discount.DiscountType.HasValue;
        DiscountType = discount.DiscountType ?? CraftingPOS.Domain.Enums.DiscountType.Percentage;
        DiscountValue = discount.DiscountValue ?? 0;

        FormCategory = Categories.FirstOrDefault(c => c.Id == value.CategoryId);
        FormBrand = value.BrandId.HasValue ? Brands.FirstOrDefault(b => b.Id == value.BrandId.Value) : null;
        FormBarcode = value.Barcode;
        FormSku = value.SKU;
        FormName = value.Name;
        FormDescription = value.Description ?? string.Empty;
        FormProductType = value.ProductType;
        FormCostPrice = value.CostPrice;
        FormSellingPrice = value.SellingPrice;
        FormCurrentStock = value.CurrentStock;
        FormMinimumStock = value.MinimumStock;
        FormImageSourcePath = null;
        FormId = value.Id;
        FormHeader = $"Edit Product — {value.Name}";
        ClearStatus();
        NewVariant();
    }

    partial void OnFormProductTypeChanged(ProductType value)
    {
        OnPropertyChanged(nameof(IsVariableProductType));
        OnPropertyChanged(nameof(ShowVariantsPanel));
        OnPropertyChanged(nameof(ShowVariantsSaveFirstMessage));
    }

    partial void OnFormIdChanged(int value)
    {
        OnPropertyChanged(nameof(ShowVariantsPanel));
        OnPropertyChanged(nameof(ShowVariantsSaveFirstMessage));

        if (value > 0 && FormProductType == ProductType.Variable)
        {
            _ = LoadVariantsAsync(value);
        }
        else
        {
            Variants.Clear();
        }
    }

    [RelayCommand]
    private void NewProduct()
    {
        SelectedProduct = null;
        FormId = 0;
        FormCategory = null;
        FormBrand = null;
        FormBarcode = string.Empty;
        FormSku = string.Empty;
        FormName = string.Empty;
        FormDescription = string.Empty;
        FormProductType = ProductType.Standard;
        FormCostPrice = 0;
        FormSellingPrice = 0;
        FormCurrentStock = 0;
        FormMinimumStock = 0;
        FormImageSourcePath = null;
        FormHeader = "New Product";
        ClearStatus();
        NewVariant();
    }

    [RelayCommand]
    private async Task SaveDiscountAsync()
    {
        if (FormId <= 0)
        {
            SetStatus("Save the product first before setting a discount.", true);
            return;
        }

        var result = await _productDiscountService.SetDiscountAsync(new SaveProductDiscountDto
        {
            ProductId = FormId,
            DiscountType = DiscountType,
            DiscountValue = DiscountValue
        });

        SetStatus(result.Success ? "Discount saved." : result.ErrorMessage ?? "Failed to save discount.", !result.Success);
        if (result.Success) HasActiveDiscount = true;
    }

    [RelayCommand]
    private async Task RemoveDiscountAsync()
    {
        if (FormId <= 0) return;

        var result = await _productDiscountService.RemoveDiscountAsync(FormId);
        SetStatus(result.Success ? "Discount removed." : result.ErrorMessage ?? "Failed to remove discount.", !result.Success);
        if (result.Success) { HasActiveDiscount = false; DiscountValue = 0; }
    }

    [RelayCommand]
    private void BrowseImage()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
            Title = "Select Product Image"
        };

        if (dialog.ShowDialog() == true)
        {
            FormImageSourcePath = dialog.FileName;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ClearStatus();

        if (FormCategory == null)
        {
            SetStatus("Please select a category.", true);
            return;
        }

        var dto = new SaveProductDto
        {
            Id = FormId,
            CategoryId = FormCategory.Id,
            BrandId = FormBrand?.Id,
            Barcode = FormBarcode,
            SKU = FormSku,
            Name = FormName,
            Description = FormDescription,
            ProductType = FormProductType,
            CostPrice = FormCostPrice,
            SellingPrice = FormSellingPrice,
            CurrentStock = FormCurrentStock,
            MinimumStock = FormMinimumStock,
            NewImageSourcePath = FormImageSourcePath,
            AllowPriceOverride = IsOwner
        };

        IsBusy = true;
        try
        {
            var result = await _productService.SaveAsync(dto);

            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Failed to save product.", true);
                return;
            }

            var wasNew = FormId == 0;
            SetStatus(wasNew ? "Product created successfully." : "Product updated successfully.", false);

            await LoadAsync();

            if (FormProductType == ProductType.Variable)
            {
                FormId = result.Data;
                FormHeader = $"Edit Product — {FormName} (Variable)";
            }
            else
            {
                NewProduct();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeactivateAsync(ProductDto? product)
    {
        if (product == null) return;

        IsBusy = true;
        try
        {
            var result = await _productService.DeactivateAsync(product.Id);

            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Failed to deactivate product.", true);
                return;
            }

            SetStatus($"Product '{product.Name}' deactivated.", false);
            await LoadAsync();
            NewProduct();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event Action<ProductDto>? PrintLabelRequested;

    [RelayCommand]
    private void PrintLabel(ProductDto? product)
    {
        if (product == null) return;
        PrintLabelRequested?.Invoke(product);
    }

    private async Task LoadVariantsAsync(int productId)
    {
        try
        {
            var variants = await _productVariantService.GetByProductIdAsync(productId);
            Variants.Clear();
            foreach (var v in variants) Variants.Add(v);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load variants for product {ProductId}.", productId);
        }
    }

    partial void OnSelectedVariantChanged(ProductVariantDto? value)
    {
        if (value == null)
        {
            NewVariantForm();
            return;
        }

        VariantFormId = value.Id;
        VariantName = value.VariantName;
        VariantBarcode = value.Barcode;
        VariantSku = value.SKU;
        VariantCostPrice = value.CostPrice;
        VariantSellingPrice = value.SellingPrice;
        VariantCurrentStock = value.CurrentStock;
        VariantMinimumStock = value.MinimumStock;
        VariantFormHeader = $"Edit Variant — {value.VariantName}";
        ClearVariantStatus();
    }

    [RelayCommand]
    private void NewVariant()
    {
        SelectedVariant = null;
        NewVariantForm();
    }

    private void NewVariantForm()
    {
        VariantFormId = 0;
        VariantName = string.Empty;
        VariantBarcode = string.Empty;
        VariantSku = string.Empty;
        VariantCostPrice = 0;
        VariantSellingPrice = 0;
        VariantCurrentStock = 0;
        VariantMinimumStock = 0;
        VariantFormHeader = "New Variant";
        ClearVariantStatus();
    }

    [RelayCommand]
    private async Task SaveVariantAsync()
    {
        ClearVariantStatus();

        if (FormId <= 0)
        {
            SetVariantStatus("Save the product first before adding variants.", true);
            return;
        }

        var dto = new SaveProductVariantDto
        {
            Id = VariantFormId,
            ProductId = FormId,
            VariantName = VariantName,
            Barcode = VariantBarcode,
            SKU = VariantSku,
            CostPrice = VariantCostPrice,
            SellingPrice = VariantSellingPrice,
            CurrentStock = VariantCurrentStock,
            MinimumStock = VariantMinimumStock
        };

        IsBusy = true;
        try
        {
            var result = await _productVariantService.SaveAsync(dto);

            if (!result.Success)
            {
                SetVariantStatus(result.ErrorMessage ?? "Failed to save variant.", true);
                return;
            }

            SetVariantStatus(VariantFormId == 0 ? "Variant added." : "Variant updated.", false);
            await LoadVariantsAsync(FormId);
            NewVariant();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeactivateVariantAsync(ProductVariantDto? variant)
    {
        if (variant == null) return;

        IsBusy = true;
        try
        {
            var result = await _productVariantService.DeactivateAsync(variant.Id);

            if (!result.Success)
            {
                SetVariantStatus(result.ErrorMessage ?? "Failed to remove variant.", true);
                return;
            }

            SetVariantStatus($"Variant '{variant.VariantName}' removed.", false);
            await LoadVariantsAsync(FormId);
            NewVariant();
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

    private void SetVariantStatus(string message, bool isError)
    {
        VariantStatusMessage = message;
        VariantHasError = isError;
    }

    private void ClearVariantStatus()
    {
        VariantStatusMessage = string.Empty;
        VariantHasError = false;
    }
}