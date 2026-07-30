using CraftingPOS.Application.Interfaces;
using Serilog;

namespace CraftingPOS.Infrastructure.Storage;

public class LocalImageStorageService : IImageStorageService
{
    private readonly string _baseDirectory;

    public LocalImageStorageService()
    {
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "CraftingPOS");

        _baseDirectory = Path.Combine(dataDirectory, "ProductImages");
        Directory.CreateDirectory(_baseDirectory);
    }

    public Task<string> SaveProductImageAsync(string sourceFilePath)
    {
        var extension = Path.GetExtension(sourceFilePath);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var destinationPath = Path.Combine(_baseDirectory, fileName);

        File.Copy(sourceFilePath, destinationPath, overwrite: true);

        Log.Information("Product image saved: {FileName}", fileName);

        // Store only the relative filename — full path is resolved at read time.
        return Task.FromResult(fileName);
    }

    public string GetFullPath(string relativePath)
    {
        return Path.Combine(_baseDirectory, relativePath);
    }

    public void DeleteProductImage(string relativePath)
    {
        var fullPath = Path.Combine(_baseDirectory, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            Log.Information("Product image deleted: {FileName}", relativePath);
        }
    }
}