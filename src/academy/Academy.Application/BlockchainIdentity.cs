using System.Text.RegularExpressions;

namespace Academy.Application;

public static partial class BlockchainIdentity
{
    public static string? NormalizeWalletAddress(string? walletAddress)
    {
        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            return null;
        }

        var trimmed = walletAddress.Trim();
        return WalletRegex().IsMatch(trimmed) ? trimmed.ToLowerInvariant() : null;
    }

    public static string CreateDid(string walletAddress) => $"did:ethr:sepolia:{walletAddress}";

    [GeneratedRegex("^0x[a-fA-F0-9]{40}$")]
    private static partial Regex WalletRegex();
}

