namespace Verifier.Infrastructure.Blockchain;

internal static class GuidToBytes32
{
    public static string Convert(Guid guid)
    {
        var normalized = guid.ToString("N").ToLowerInvariant();
        return $"0x{normalized.PadLeft(64, '0')}";
    }
}
