using CraftingPOS.Licensing;
using Xunit;

namespace CraftingPOS.Tests.Licensing;

public class LicenseSignerTests
{
    [Fact]
    public void Sign_ThenVerify_WithCorrectKey_Succeeds()
    {
        var (publicKey, privateKey) = LicenseSigner.GenerateKeyPair();

        var data = new LicenseData
        {
            BusinessName = "Test Shop",
            MachineFingerprint = "CPOS-TEST-0000-0000-0000",
            LicenseType = LicenseType.Lifetime,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = null
        };

        var signature = LicenseSigner.Sign(data, privateKey);
        var isValid = LicenseSigner.Verify(data, signature, publicKey);

        Assert.True(isValid);
    }

    [Fact]
    public void Verify_WithTamperedData_Fails()
    {
        // Anti-Piracy Layer 2: modified license files become invalid.
        var (publicKey, privateKey) = LicenseSigner.GenerateKeyPair();

        var data = new LicenseData
        {
            BusinessName = "Test Shop",
            MachineFingerprint = "CPOS-TEST-0000-0000-0000",
            LicenseType = LicenseType.Lifetime,
            IssuedAt = DateTime.UtcNow
        };

        var signature = LicenseSigner.Sign(data, privateKey);

        // Tamper: attacker edits the business name after signing.
        data.BusinessName = "Pirated Shop";

        var isValid = LicenseSigner.Verify(data, signature, publicKey);
        Assert.False(isValid);
    }

    [Fact]
    public void Verify_WithWrongPublicKey_Fails()
    {
        var (_, privateKey) = LicenseSigner.GenerateKeyPair();
        var (otherPublicKey, _) = LicenseSigner.GenerateKeyPair(); // a different, unrelated key pair

        var data = new LicenseData
        {
            BusinessName = "Test Shop",
            MachineFingerprint = "CPOS-TEST-0000-0000-0000",
            LicenseType = LicenseType.Lifetime,
            IssuedAt = DateTime.UtcNow
        };

        var signature = LicenseSigner.Sign(data, privateKey);
        var isValid = LicenseSigner.Verify(data, signature, otherPublicKey);

        Assert.False(isValid);
    }

    [Fact]
    public void Verify_ExpiredLicenseData_SignatureStillValid_ExpiryCheckedSeparately()
    {
        // Signature validity and expiry are independent checks — this confirms
        // LicenseManager (not LicenseSigner) is responsible for the expiry gate.
        var (publicKey, privateKey) = LicenseSigner.GenerateKeyPair();

        var data = new LicenseData
        {
            BusinessName = "Test Shop",
            MachineFingerprint = "CPOS-TEST-0000-0000-0000",
            LicenseType = LicenseType.Yearly,
            IssuedAt = DateTime.UtcNow.AddYears(-2),
            ExpiresAt = DateTime.UtcNow.AddYears(-1) // already expired
        };

        var signature = LicenseSigner.Sign(data, privateKey);
        var signatureValid = LicenseSigner.Verify(data, signature, publicKey);

        Assert.True(signatureValid); // signature itself is fine
        Assert.True(data.ExpiresAt < DateTime.UtcNow); // but LicenseManager.Validate() must reject this
    }
}