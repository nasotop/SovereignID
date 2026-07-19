namespace Issuer.Application.ContentAnchor;

public sealed record PinResult(bool Succeeded, string? Cid, string? ErrorDetail)
{
    public static PinResult Success(string cid) => new(true, cid, null);

    public static PinResult Failure(string detail) => new(false, null, detail);
}

public interface IContentPinningAdapter
{
    bool IsConfigured { get; }

    Task<PinResult> PinAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken);
}
