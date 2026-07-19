using System.Security.Cryptography;

namespace Issuer.Application.ContentAnchor;

public static class ContentHashComputer
{
    public static string ComputeSha256Hex(ReadOnlySpan<byte> bytes)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(bytes, hash);
        return $"0x{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    public static string NormalizeHash(string hash) =>
        hash.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? hash.ToLowerInvariant()
            : $"0x{hash.ToLowerInvariant()}";

    public static bool HashesEqual(string left, string right) =>
        string.Equals(NormalizeHash(left), NormalizeHash(right), StringComparison.Ordinal);
}
