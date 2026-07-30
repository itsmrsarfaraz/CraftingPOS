using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using Serilog;

namespace CraftingPOS.Presentation.ViewModels;

public partial class CategoriesViewModel : ObservableObject
{
    private readonly ICategoryService _categoryService;

    public ObservableCollection<CategoryDto> Categories { get; } = new();

    [ObservableProperty]
    private CategoryDto? selectedCategory;

    // Form fields
    [ObservableProperty]
    private int formId;

    [ObservableProperty]
    private string formName = string.Empty;

    [ObservableProperty]
    private string formDescription = string.Empty;

    [ObservableProperty]
    private string formHeader = "New Category";

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    public CategoriesViewModel(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;

        try
        {
            var items = await _categoryService.GetAllAsync();

            Categories.Clear();
            foreach (var item in items)
                Categories.Add(item);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load categories.");
            SetStatus("Failed to load categories.", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedCategoryChanged(CategoryDto? value)
    {
        if (value == null)
        {
            NewCategory();
            return;
        }

        FormId = value.Id;
        FormName = value.Name;
        FormDescription = value.Description ?? string.Empty;
        FormHeader = $"Edit Category — {value.Name}";
        ClearStatus();
    }

    [RelayCommand]
    private void NewCategory()
    {
        SelectedCategory = null;
        FormId = 0;
        FormName = string.Empty;
        FormDescription = string.Empty;
        FormHeader = "New Category";
        ClearStatus();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ClearStatus();

        var dto = new SaveCategoryDto
        {
            Id = FormId,
            Name = FormName,
            Description = FormDescription
        };

        IsBusy = true;

        try
        {
            var result = await _categoryService.SaveAsync(dto);

            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Failed to save category.", isError: true);
                return;
            }

            SetStatus(FormId == 0 ? "Category created successfully." : "Category updated successfully.", isError: false);
            await LoadAsync();
            NewCategory();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeactivateAsync(CategoryDto? category)
    {
        if (category == null) return;

        IsBusy = true;

        try
        {
            var result = await _categoryService.DeactivateAsync(category.Id);

            if (!result.Success)
            {
                SetStatus(result.ErrorMessage ?? "Failed to deactivate category.", isError: true);
                return;
            }

            SetStatus($"Category '{category.Name}' deactivated.", isError: false);
            await LoadAsync();
            NewCategory();
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