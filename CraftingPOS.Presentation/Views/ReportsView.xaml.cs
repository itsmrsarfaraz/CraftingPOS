using System.Windows.Controls;
using CraftingPOS.Presentation.ViewModels;

namespace CraftingPOS.Presentation.Views;

public partial class ReportsView : UserControl
{
    private readonly ReportsViewModel _viewModel;

    public ReportsView(ReportsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ReportsViewModel.ColumnHeaders))
            {
                RebuildColumns();
            }
        };
    }

    private void RebuildColumns()
    {
        ReportGrid.Columns.Clear();

        for (var i = 0; i < _viewModel.ColumnHeaders.Count; i++)
        {
            var index = i;
            ReportGrid.Columns.Add(new DataGridTextColumn
            {
                Header = _viewModel.ColumnHeaders[index],
                Binding = new System.Windows.Data.Binding($"[{index}]")
            });
        }
    }
}