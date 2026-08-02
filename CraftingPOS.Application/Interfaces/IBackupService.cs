using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface IBackupService
{
    /// <summary>FR-BACKUP-001/003: creates a zipped backup of the database and logs.</summary>
    Task<OperationResult<BackupInfoDto>> CreateBackupAsync();

    /// <summary>Lists existing backups in the default backup folder (for the Status Area / last backup date).</summary>
    Task<List<BackupInfoDto>> ListBackupsAsync();

    /// <summary>FR-BACKUP-005: validates a backup zip's integrity before restoring.</summary>
    Task<OperationResult> ValidateBackupAsync(string backupZipPath);

    /// <summary>FR-BACKUP-004: restores from a validated backup. Requires the app to restart afterward.</summary>
    Task<OperationResult> RestoreBackupAsync(string backupZipPath);
}