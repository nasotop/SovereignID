using System.Text.Json;
using Issuer.Application;
using Issuer.Application.ContentAnchor;
using Microsoft.Extensions.Options;

namespace Issuer.Infrastructure.ContentAnchor;

internal sealed class CredentialContentAnchorService : IContentAnchorService
{
    private readonly IContentPinningAdapter _pinningAdapter;
    private readonly ContentAnchorOptions _options;

    public CredentialContentAnchorService(
        IContentPinningAdapter pinningAdapter,
        IOptions<IssuerOptions> options)
    {
        _pinningAdapter = pinningAdapter;
        _options = options.Value.ContentAnchor;
    }

    public async Task<IssuerResult<ContentAnchorResult>> AnchorAsync(
        JsonElement document,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled || !_pinningAdapter.IsConfigured)
        {
            return Fail("ipfs_not_configured", 503, "IPFS content anchoring is not configured.");
        }

        if (!CanonicalJsonSerializer.TryCanonicalize(document, out var canonicalBytes, out var canonicalizeError))
        {
            return Fail(
                "invalid_anchor_document",
                400,
                canonicalizeError ?? "document must be a valid JSON object or array.");
        }

        var contentHash = ContentHashComputer.ComputeSha256Hex(canonicalBytes);
        var pinResult = await _pinningAdapter.PinAsync(canonicalBytes, cancellationToken);

        if (!pinResult.Succeeded || string.IsNullOrWhiteSpace(pinResult.Cid))
        {
            return Fail(
                "content_anchor_failed",
                502,
                pinResult.ErrorDetail ?? "Failed to pin content to IPFS.");
        }

        var gatewayBase = _options.GatewayBase.TrimEnd('/');
        var gatewayUrl = $"{gatewayBase}/{pinResult.Cid}";

        return new IssuerSuccess<ContentAnchorResult>(
            new ContentAnchorResult(contentHash, pinResult.Cid, gatewayUrl));
    }

    private static IssuerFailureResult<ContentAnchorResult> Fail(string errorCode, int statusCode, string detail) =>
        new(new IssuerFailure(errorCode, statusCode, detail));
}
