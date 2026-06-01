using System.Globalization;
using Nethereum.Signer;

namespace Auth.IntegrationTests;

public static class SiweTestHelper
{
    public const string Domain = "sovereignid.test";
    public const string Uri = "https://sovereignid.test";
    public const string Statement = "Sign in to SovereignID Auth tests.";
    public const int SepoliaChainId = 11155111;

    public static (EthECKey Key, string Address) CreateWallet()
    {
        var key = EthECKey.GenerateKey();
        return (key, key.GetPublicAddress());
    }

    public static string BuildMessage(
        string address,
        string nonce,
        DateTimeOffset issuedAt,
        int chainId = SepoliaChainId,
        string? domain = null,
        string? statement = null)
    {
        return string.Join(
            '\n',
            $"{domain ?? Domain} wants you to sign in with your Ethereum account:",
            address,
            string.Empty,
            statement ?? Statement,
            string.Empty,
            $"URI: {Uri}",
            "Version: 1",
            $"Chain ID: {chainId}",
            $"Nonce: {nonce}",
            $"Issued At: {issuedAt.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture)}");
    }

    public static string Sign(string message, EthECKey key)
    {
        var signer = new EthereumMessageSigner();
        return signer.EncodeUTF8AndSign(message, key);
    }
}
