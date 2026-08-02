using CraftingPOS.Licensing;
using Xunit;

namespace CraftingPOS.Tests.Licensing;

public class MachineFingerprintGeneratorTests
{
    [Fact]
    public void Generate_IsStableAcrossCalls()
    {
        // BR: fingerprint must remain stable across reboots — simulated here
        // by simply calling it twice in the same process and expecting an
        // identical result, since the underlying hardware IDs don't change.
        var first = MachineFingerprintGenerator.Generate();
        var second = MachineFingerprintGenerator.Generate();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Generate_ProducesExpectedFormat()
    {
        var fingerprint = MachineFingerprintGenerator.Generate();

        Assert.StartsWith("CPOS-", fingerprint);
        Assert.Equal(24, fingerprint.Length); // "CPOS-XXXX-XXXX-XXXX-XXXX"
    }
}