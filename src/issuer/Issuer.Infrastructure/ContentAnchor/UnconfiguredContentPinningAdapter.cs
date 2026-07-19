using Issuer.Application.ContentAnchor;

namespace Issuer.Infrastructure.ContentAnchor;

internal sealed class UnconfiguredContentPinningAdapter : IContentPinningAdapter
{
    public bool IsConfigured => false;

    public Task<PinResult> PinAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
        Task.FromResult(PinResult.Failure("IPFS pinning is not configured."));
}
