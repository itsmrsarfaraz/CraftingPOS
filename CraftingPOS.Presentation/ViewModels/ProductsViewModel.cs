using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private readonly Application.Common.CurrentUserContext _currentUserContext;

    public ObservableCollection<ProductDto> Products { get; } = new();
    public ObservableCollection<CategoryDto> Categories { get; } = new();
    public List<ProductType> ProductTypes { get; } = Enum.GetValues<ProductType>().ToList();

    [ObservableProperty]
    private ProductDto? selectedProduct;

    [ObservableProperty]
    private string searchTerm = string.Empty;

    // Form fields
    [ObservableProperty] private int formId;
    [ObservableProperty] private CategoryDto? formCategory;
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

    public bool IsOwner => _currentUserContext.Session?.RoleName == RoleNames.Owner;

    public ProductsViewModel(
        IProductService productService,
        ICategoryService categoryService,
        Application.Common.CurrentUserContext currentUserContext)
    {
        _productService = productService;
        _categoryService = categoryService;
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

    partial void OnSelectedProductChanged(ProductDto? value)
    {
        if (value == null)
        {
            NewProduct();
            return;
        }

        FormId = value.Id;
        FormCategory = Categories.FirstOrDefault(c => c.Id == value.CategoryId);
        FormBarcode = value.Barcode;
        FormSku = value.SKU;
        FormName = value.Name;
        FormDescription = value.Description ?? string.Empty;
        FormProductType = value.ProductType;
        FormCostPrice = value.CostPrice;
        FormSellingPrice = value.SellingPrice;
        FormCurrentStock = value.CurrentStock;
        FormMinimumStock = value.MinimumStock;
        FormImageSourcePath = null; // only set when user picks a NEW image
        FormHeader = $"Edit Product — {value.Name}";
        ClearStatus();
    }

    [RelayCommand]
    private void NewProduct()
    {
        SelectedProduct = null;
        FormId = 0;
        FormCategory = null;
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

            SetStatus(FormId == 0 ? "Product created successfully." : "Product updated successfully.", false);
            await LoadAsync();
            NewProduct();
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