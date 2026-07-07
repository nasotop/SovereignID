namespace Verifier.Infrastructure.Evidence;

internal sealed record IpfsReadResult(bool? HashMatches);

internal interface IIpfsContentReader
{
    Task<IpfsReadResult> ReadAndHashAsync(string gatewayUrl, string expectedContentHash, CancellationToken cancellationToken);
}
