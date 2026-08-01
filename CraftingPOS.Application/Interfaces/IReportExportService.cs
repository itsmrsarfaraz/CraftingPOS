namespace CraftingPOS.Application.Interfaces;

/// <summary>
/// Exports a generic tabular report to PDF or Excel. Kept generic (title +
/// column headers + rows of strings) so one implementation serves every
/// report kind without a bespoke exporter per report.
/// </summary>
public interface IReportExportService
{
    void ExportToPdf(string title, string subtitle, List<string> columnHeaders, List<List<string>> rows, string outputFilePath);
    void ExportToExcel(string title, List<string> columnHeaders, List<List<string>> rows, string outputFilePath);
}