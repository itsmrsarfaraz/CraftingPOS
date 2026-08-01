using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;

namespace CraftingPOS.Presentation.ViewModels;

public partial class ReceiptPreviewViewModel : ObservableObject
{
    private readonly ISaleService _saleService;
    private readonly IReceiptPrinterService _receiptPrinterService;

    private ReceiptDto? _receipt;

    [ObservableProperty] private string previewText = string.Empty;
    [ObservableProperty] private ReceiptPaperWidth selectedPaperWidth = ReceiptPaperWidth.Width80mm;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool hasPrintedOnce;

    public ReceiptPreviewViewModel(ISaleService saleService, IReceiptPrinterService receiptPrinterService)
    {
        _saleService = saleService;
        _receiptPrinterService = receiptPrinterService;
    }

    public async Task LoadAsync(int saleId)
    {
        IsBusy = true;
        try
        {
            _receipt = await _saleService.GetReceiptAsync(saleId);
            RefreshPreview();
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedPaperWidthChanged(ReceiptPaperWidth value) => RefreshPreview();

    private void RefreshPreview()
    {
        if (_receipt == null) return;
        PreviewText = _receiptPrinterService.BuildPreviewText(_receipt, SelectedPaperWidth);
    }

    [RelayCommand]
    private void Print()
    {
        if (_receipt == null) return;
        _receiptPrinterService.Print(_receipt, SelectedPaperWidth);
        HasPrintedOnce = true;
    }
}