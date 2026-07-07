namespace Verifier.Infrastructure.Evidence;

internal sealed class InMemoryIpfsContentReader : IIpfsContentReader
{
    private readonly Dictionary<string, IpfsReadResult> _responses = new(StringComparer.OrdinalIgnoreCase);

    public void SetResponse(string gatewayUrl, IpfsReadResult result) =>
        _responses[gatewayUrl] = result;

    public Task<IpfsReadResult> ReadAndHashAsync(
        string gatewayUrl,
        string expectedContentHash,
        CancellationToken cancellationToken) =>
        _responses.TryGetValue(gatewayUrl, out var result)
            ? Task.FromResult(result)
            : Task.FromResult(new IpfsReadResult(null));
}
