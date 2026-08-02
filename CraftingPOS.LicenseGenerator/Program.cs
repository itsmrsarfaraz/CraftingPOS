using CraftingPOS.Licensing;

if (args.Length == 0)
{
    PrintUsage();
    return;
}

switch (args[0].ToLowerInvariant())
{
    case "generate-keys":
        GenerateKeys();
        break;
    case "issue":
        IssueLicense(args);
        break;
    default:
        PrintUsage();
        break;
}

static void PrintUsage()
{
    Console.WriteLine("CraftingPOS License Generator — INTERNAL TOOL, do not ship with the app.");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  generate-keys");
    Console.WriteLine("      Generates a new RSA-2048 key pair (publickey.xml / privatekey.xml).");
    Console.WriteLine("      Paste publickey.xml's contents into LicensePublicKeyProvider.PublicKeyXml.");
    Console.WriteLine("      Keep privatekey.xml safe, offline, and OUT of git.");
    Console.WriteLine();
    Console.WriteLine("  issue <privateKeyPath> <businessName> <machineFingerprint> <Lifetime|Monthly|Yearly> <outputPath> [expiryDate:yyyy-MM-dd]");
    Console.WriteLine("      Issues a signed license.dat for a client.");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  CraftingPOS.LicenseGenerator generate-keys");
    Console.WriteLine("  CraftingPOS.LicenseGenerator issue privatekey.xml \"Ali Traders\" CPOS-AB12-CD34-EF56-7890 Lifetime license.dat");
    Console.WriteLine("  CraftingPOS.LicenseGenerator issue privatekey.xml \"Ali Traders\" CPOS-AB12-CD34-EF56-7890 Yearly license.dat 2027-08-02");
}

static void GenerateKeys()
{
    var (publicKey, privateKey) = LicenseSigner.GenerateKeyPair();

    File.WriteAllText("publickey.xml", publicKey);
    File.WriteAllText("privatekey.xml", privateKey);

    Console.WriteLine("Key pair generated in the current folder:");
    Console.WriteLine("  publickey.xml  -> paste into LicensePublicKeyProvider.PublicKeyXml, then rebuild the app.");
    Console.WriteLine("  privatekey.xml -> KEEP THIS SAFE. Never commit or share it.");
}

static void IssueLicense(string[] a)
{
    if (a.Length < 6)
    {
        PrintUsage();
        return;
    }

    var privateKeyPath = a[1];
    var businessName = a[2];
    var fingerprint = a[3];
    var licenseTypeArg = a[4];
    var outputPath = a[5];
    var expiryArg = a.Length > 6 ? a[6] : null;

    if (!Enum.TryParse<LicenseType>(licenseTypeArg, true, out var licenseType))
    {
        Console.WriteLine($"Invalid license type '{licenseTypeArg}'. Use Lifetime, Monthly, or Yearly.");
        return;
    }

    if (!File.Exists(privateKeyPath))
    {
        Console.WriteLine($"Private key file not found: {privateKeyPath}");
        return;
    }

    DateTime? expiresAt = null;
    if (licenseType != LicenseType.Lifetime)
    {
        expiresAt = expiryArg != null && DateTime.TryParse(expiryArg, out var parsedExpiry)
            ? parsedExpiry
            : licenseType == LicenseType.Monthly ? DateTime.UtcNow.AddMonths(1) : DateTime.UtcNow.AddYears(1);
    }

    var data = new LicenseData
    {
        BusinessName = businessName,
        MachineFingerprint = fingerprint,
        LicenseType = licenseType,
        IssuedAt = DateTime.UtcNow,
        ExpiresAt = expiresAt
    };

    var privateKeyXml = File.ReadAllText(privateKeyPath);
    var signature = LicenseSigner.Sign(data, privateKeyXml);

    var licenseFile = new LicenseFile { Data = data, SignatureBase64 = signature };
    var json = System.Text.Json.JsonSerializer.Serialize(licenseFile, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

    File.WriteAllText(outputPath, json);

    Console.WriteLine($"License issued: {outputPath}");
    Console.WriteLine($"  Business: {businessName}");
    Console.WriteLine($"  Machine:  {fingerprint}");
    Console.WriteLine($"  Type:     {licenseType}");
    Console.WriteLine($"  Expires:  {(expiresAt.HasValue ? expiresAt.Value.ToString("dd MMM yyyy") : "Never (Lifetime)")}");
}