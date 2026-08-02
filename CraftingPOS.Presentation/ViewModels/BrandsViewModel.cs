using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using Serilog;

namespace CraftingPOS.Presentation.ViewModels;

public partial class BrandsViewModel : ObservableObject
{
    private readonly IBrandService _brandService;

    public ObservableCollection<BrandDto> Brands { get; } = new();

    [ObservableProperty] private BrandDto? selectedBrand;
    [ObservableProperty] private int formId;
    [ObservableProperty] private string formName = string.Empty;
    [ObservableProperty] private string formDescription = string.Empty;
    [ObservableProperty] private string formHeader = "New Brand";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool hasError;

    public BrandsViewModel(IBrandService brandService)
    {
        _brandService = brandService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var items = await _brandService.GetAllAsync();
            Brands.Clear();
            foreach (var item in items) Brands.Add(item);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load brands.");
            SetStatus("Failed to load brands.", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedBrandChanged(BrandDto? value)
    {
        if (value == null)
        {
            NewBrand();
            return;
        }

        FormId = value.Id;
        FormName = value.Name;
        FormDescription = value.Description ?? string.Empty;
        FormHeader = $"Edit Brand — {value.Name}";
        ClearStatus();
    }

    [RelayCommand]
    private void NewBrand()
    {
        SelectedBrand = null;
        FormId = 0;
        FormName = string.Empty;
        FormDescription = string.Empty;
        FormHeader = "New Brand";
        ClearStatus();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ClearStatus();

        var dto = new SaveBrandDto { Id = FormId, Name = FormName, Description = FormDescription };

        IsBusy = true;
        try
        {
            var result = await _brandService.SaveAsync(dto);
            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Failed to save brand.", true);
                return;
            }

            SetStatus(FormId == 0 ? "Brand created successfully." : "Brand updated successfully.", false);
            await LoadAsync();
            NewBrand();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeactivateAsync(BrandDto? brand)
    {
        if (brand == null) return;

        IsBusy = true;
        try
        {
            var result = await _brandService.DeactivateAsync(brand.Id);
            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Failed to deactivate brand.", true);
                return;
            }

            SetStatus($"Brand '{brand.Name}' deactivated.", false);
            await LoadAsync();
            NewBrand();
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