using System.Text.Json;

namespace CraftingPOS.Licensing;

public class LicenseManager
{
    private readonly string _licenseFilePath;
    private LicenseValidationResult? _lastResult;
    private DateTime _lastCheckedAt = DateTime.MinValue;
    private static readonly TimeSpan RevalidationThrottle = TimeSpan.FromMinutes(5);

    public LicenseManager(string dataDirectory)
    {
        _licenseFilePath = Path.Combine(dataDirectory, "license.dat");
    }

    public string CurrentMachineFingerprint => MachineFingerprintGenerator.Generate();

    /// <summary>FR-LIC-003/004: full validation — signature, machine match, and expiry.</summary>
    public LicenseValidationResult Validate()
    {
        if (!File.Exists(_licenseFilePath))
        {
            return Cache(LicenseValidationResult.Fail("No license file found. Please activate CraftingPOS."));
        }

        try
        {
            var json = File.ReadAllText(_licenseFilePath);
            var licenseFile = JsonSerializer.Deserialize<LicenseFile>(json);

            if (licenseFile?.Data == null || string.IsNullOrWhiteSpace(licenseFile.SignatureBase64))
            {
                return Cache(LicenseValidationResult.Fail("License file is corrupted or unreadable."));
            }

            var signatureValid = LicenseSigner.Verify(licenseFile.Data, licenseFile.SignatureBase64, LicensePublicKeyProvider.PublicKeyXml);
            if (!signatureValid)
            {
                return Cache(LicenseValidationResult.Fail("License signature is invalid. This license may have been tampered with."));
            }

            var currentFingerprint = CurrentMachineFingerprint;
            if (!string.Equals(licenseFile.Data.MachineFingerprint, currentFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                return Cache(LicenseValidationResult.Fail("This license is not valid for this machine."));
            }

            if (licenseFile.Data.ExpiresAt.HasValue && licenseFile.Data.ExpiresAt.Value < DateTime.UtcNow)
            {
                return Cache(LicenseValidationResult.Fail($"This license expired on {licenseFile.Data.ExpiresAt.Value:dd MMM yyyy}."));
            }

            return Cache(LicenseValidationResult.Ok(licenseFile.Data));
        }
        catch (Exception ex)
        {
            return Cache(LicenseValidationResult.Fail($"Failed to read license file: {ex.Message}"));
        }
    }

    /// <summary>
    /// Cheap re-check for anti-piracy Layer 5 ("validate throughout runtime,
    /// not just at startup"). Throttled so login/sale-completion checks don't
    /// hit WMI/disk on every single call.
    /// </summary>
    public bool QuickCheckIsValid()
    {
        if (_lastResult == null || DateTime.UtcNow - _lastCheckedAt > RevalidationThrottle)
        {
            Validate();
        }

        return _lastResult?.IsValid ?? false;
    }

    public LicenseValidationResult? LastResult => _lastResult;

    /// <summary>FR-LIC-002: imports and activates a license file selected by the user.</summary>
    public LicenseValidationResult ActivateFromFile(string sourceFilePath)
    {
        try
        {
            var json = File.ReadAllText(sourceFilePath);
            var licenseFile = JsonSerializer.Deserialize<LicenseFile>(json);

            if (licenseFile?.Data == null)
                return LicenseValidationResult.Fail("Selected file is not a valid CraftingPOS license.");

            var signatureValid = LicenseSigner.Verify(licenseFile.Data, licenseFile.SignatureBase64, LicensePublicKeyProvider.PublicKeyXml);
            if (!signatureValid)
                return LicenseValidationResult.Fail("License signature is invalid.");

            var currentFingerprint = CurrentMachineFingerprint;
            if (!string.Equals(licenseFile.Data.MachineFingerprint, currentFingerprint, StringComparison.OrdinalIgnoreCase))
                return LicenseValidationResult.Fail("This license was issued for a different machine.");

            if (licenseFile.Data.ExpiresAt.HasValue && licenseFile.Data.ExpiresAt.Value < DateTime.UtcNow)
                return LicenseValidationResult.Fail($"This license expired on {licenseFile.Data.ExpiresAt.Value:dd MMM yyyy}.");

            Directory.CreateDirectory(Path.GetDirectoryName(_licenseFilePath)!);
            File.Copy(sourceFilePath, _licenseFilePath, overwrite: true);

            return Validate();
        }
        catch (Exception ex)
        {
            return LicenseValidationResult.Fail($"Failed to activate license: {ex.Message}");
        }
    }

    private LicenseValidationResult Cache(LicenseValidationResult result)
    {
        _lastResult = result;
        _lastCheckedAt = DateTime.UtcNow;
        return result;
    }
}