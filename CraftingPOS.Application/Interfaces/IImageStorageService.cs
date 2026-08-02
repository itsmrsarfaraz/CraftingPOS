namespace CraftingPOS.Application.Interfaces;

public interface IImageStorageService
{
    /// <summary>
    /// Copies an image file into managed storage and returns the stored relative path.
    /// </summary>
    Task<string> SaveProductImageAsync(string sourceFilePath);

    /// <summary>
    /// Resolves a stored relative path into a full, absolute path for display.
    /// </summary>
    string GetFullPath(string relativePath);

    void DeleteProductImage(string relativePath);
}