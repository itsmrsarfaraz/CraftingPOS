namespace CraftingPOS.Licensing;

/// <summary>
/// Embeds the PUBLIC key used to verify license files shipped with the app.
///
/// *** ACTION REQUIRED BEFORE FIRST USE ***
/// The placeholder below will NOT verify any real license. Run
/// CraftingPOS.LicenseGenerator with "generate-keys" once, then replace
/// PublicKeyXml below with the generated public key. Keep the matching
/// PRIVATE key file (privatekey.xml) somewhere safe, offline, and OUT of
/// git — anyone with it can issue valid licenses for your product.
/// </summary>
public static class LicensePublicKeyProvider
{
    public const string PublicKeyXml =
        "<RSAKeyValue><Modulus>mfFsu8yJey0NwQwL7Ouw28LOwb3YGP3evgqlIVRQ0P3OYv1H3laQfEgheHHmUYC4ubb1xIew0U15a7V6BAjAIgvMF21BM4rEOfvsQeKlgXD+GubI7jNB0aWEQMlQFwRAguIGcSiAJYckCpJmixPa+vtYIkQWToov9moerV13ikLq81tR8RkdAJAhE9SY7tot7Jg2BBEvVDGAa9679nglSMd9Q9wYuzPMC3HlBzY2drdIAocxmC6qO7+43maYDRhwoFk53BKVutzzrtX8v+a9Dzlh2AJPGLC0zG55E7hZXnV+tD9sOo+8YMEv0oC4i1ysq0Crir5s2iBpY4gFaf/AlQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
}