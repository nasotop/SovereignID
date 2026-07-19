using System.Text.Json;

namespace Issuer.Application.ContentAnchor;

public sealed record ContentAnchorResult(
    string ContentHash,
    string IpfsCid,
    string IpfsGatewayUrl);

public interface IContentAnchorService
{
    Task<IssuerResult<ContentAnchorResult>> AnchorAsync(
        JsonElement document,
        CancellationToken cancellationToken);
}
