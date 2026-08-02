using System.Security.Cryptography;
using System.Text;

namespace CraftingPOS.Licensing;

/// <summary>
/// RSA-2048 sign/verify per SRS Part 8 §12. Shared between the main app
/// (verify only, using the public key) and CraftingPOS.LicenseGenerator
/// (sign, using the private key — never shipped with the app).
/// </summary>
public static class LicenseSigner
{
    public static string Sign(LicenseData data, string privateKeyXml)
    {
        using var rsa = RSA.Create();
        rsa.FromXmlString(privateKeyXml);

        var bytes = Encoding.UTF8.GetBytes(data.ToCanonicalString());
        var signature = rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signature);
    }

    public static bool Verify(LicenseData data, string signatureBase64, string publicKeyXml)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.FromXmlString(publicKeyXml);

            var bytes = Encoding.UTF8.GetBytes(data.ToCanonicalString());
            var signature = Convert.FromBase64String(signatureBase64);
            return rsa.VerifyData(bytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    public static (string publicKeyXml, string privateKeyXml) GenerateKeyPair()
    {
        using var rsa = RSA.Create(2048);
        return (rsa.ToXmlString(false), rsa.ToXmlString(true));
    }
}