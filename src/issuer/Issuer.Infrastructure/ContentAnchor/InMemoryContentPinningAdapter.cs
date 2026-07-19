using System.Collections.Concurrent;
using System.Security.Cryptography;
using Issuer.Application.ContentAnchor;

namespace Issuer.Infrastructure.ContentAnchor;

/// <summary>
/// Test/dev adapter that stores pinned bytes in memory and returns a deterministic CID.
/// </summary>
public sealed class InMemoryContentPinningAdapter : IContentPinningAdapter
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new(StringComparer.Ordinal);

    public bool IsConfigured => true;

    public IReadOnlyDictionary<string, byte[]> Store => _store;

    public Task<PinResult> PinAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = content.ToArray();
        var cid = ComputeDeterministicCid(bytes);
        _store[cid] = bytes;
        return Task.FromResult(PinResult.Success(cid));
    }

    public bool TryGet(string cid, out byte[]? bytes) => _store.TryGetValue(cid, out bytes);

    private static string ComputeDeterministicCid(ReadOnlySpan<byte> bytes)
    {
        var hash = SHA256.HashData(bytes);
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return $"bafybei{hex[..38]}";
    }
}
