using System.IO.Compression;
using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CraftingPOS.Persistence.Services;

public class BackupService : IBackupService
{
    private readonly AppDbContext _context;
    private readonly string _dataDirectory;
    private readonly string _dbPath;
    private readonly string _logsDirectory;
    private readonly string _backupDirectory;

    public BackupService(AppDbContext context)
    {
        _context = context;

        _dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CraftingPOS");
        _dbPath = Path.Combine(_dataDirectory, "CraftingPOS.db");
        _logsDirectory = Path.Combine(_dataDirectory, "Logs");

        // FR-BACKUP default location per SRS Part 10 §8.
        _backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CraftingPOS", "Backups");
        Directory.CreateDirectory(_backupDirectory);
    }

    public async Task<OperationResult<BackupInfoDto>> CreateBackupAsync()
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"cpos_backup_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            var snapshotDbPath = Path.Combine(tempDir, "CraftingPOS.db");

            // VACUUM INTO creates a consistent snapshot even while the live DB is open (WAL-safe).
            await using (var conn = new SqliteConnection(_context.Database.GetConnectionString()))
            {
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"VACUUM INTO '{snapshotDbPath.Replace("'", "''")}'";
                await cmd.ExecuteNonQueryAsync();
            }

            if (Directory.Exists(_logsDirectory))
            {
                var logsDest = Path.Combine(tempDir, "Logs");
                CopyDirectory(_logsDirectory, logsDest);
            }

            var fileName = $"CraftingPOS_Backup_{DateTime.Now:yyyy_MM_dd_HHmmss}.zip";
            var zipPath = Path.Combine(_backupDirectory, fileName);

            ZipFile.CreateFromDirectory(tempDir, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            Directory.Delete(tempDir, recursive: true);

            var info = new BackupInfoDto
            {
                FileName = fileName,
                FullPath = zipPath,
                CreatedAt = DateTime.Now,
                SizeBytes = new FileInfo(zipPath).Length
            };

            Log.Information("Backup created: {FileName} ({Size} bytes)", fileName, info.SizeBytes);

            return OperationResult<BackupInfoDto>.Ok(info);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Backup creation failed.");
            return OperationResult<BackupInfoDto>.Fail($"Backup failed: {ex.Message}");
        }
    }

    public Task<List<BackupInfoDto>> ListBackupsAsync()
    {
        if (!Directory.Exists(_backupDirectory))
            return Task.FromResult(new List<BackupInfoDto>());

        var files = Directory.GetFiles(_backupDirectory, "*.zip")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new BackupInfoDto
            {
                FileName = f.Name,
                FullPath = f.FullName,
                CreatedAt = f.CreationTime,
                SizeBytes = f.Length
            })
            .ToList();

        return Task.FromResult(files);
    }

    public async Task<OperationResult> ValidateBackupAsync(string backupZipPath)
    {
        if (!File.Exists(backupZipPath))
            return OperationResult.Fail("Backup file not found.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"cpos_validate_{Guid.NewGuid():N}");

        try
        {
            ZipFile.ExtractToDirectory(backupZipPath, tempDir);

            var extractedDbPath = Path.Combine(tempDir, "CraftingPOS.db");
            if (!File.Exists(extractedDbPath))
                return OperationResult.Fail("Backup does not contain a valid CraftingPOS database file.");

            await using var conn = new SqliteConnection($"Data Source={extractedDbPath}");
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            var result = (string?)await cmd.ExecuteScalarAsync();

            if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                return OperationResult.Fail($"Backup failed integrity check: {result}");

            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail($"Backup validation failed: {ex.Message}");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    public async Task<OperationResult> RestoreBackupAsync(string backupZipPath)
    {
        var validation = await ValidateBackupAsync(backupZipPath);
        if (!validation.Success)
            return validation;

        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"cpos_restore_{Guid.NewGuid():N}");
            ZipFile.ExtractToDirectory(backupZipPath, tempDir);

            var extractedDbPath = Path.Combine(tempDir, "CraftingPOS.db");

            // Release all pooled SQLite connections so the live file can be overwritten.
            await _context.Database.CloseConnectionAsync();
            SqliteConnection.ClearAllPools();

            File.Copy(extractedDbPath, _dbPath, overwrite: true);

            Directory.Delete(tempDir, recursive: true);

            Log.Information("Database restored from backup: {BackupPath}", backupZipPath);

            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Restore failed.");
            return OperationResult.Fail($"Restore failed: {ex.Message}");
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
    }
}