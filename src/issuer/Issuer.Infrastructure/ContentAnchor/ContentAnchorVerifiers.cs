using Issuer.Application;
using Issuer.Application.ContentAnchor;
using Microsoft.Extensions.Options;

namespace Issuer.Infrastructure.ContentAnchor;

internal sealed class HttpContentAnchorVerifier : IContentAnchorVerifier
{
    private readonly HttpClient _httpClient;
    private readonly ContentAnchorOptions _options;

    public HttpContentAnchorVerifier(HttpClient httpClient, IOptions<IssuerOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.ContentAnchor;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.IpfsTimeoutSeconds));
    }

    public async Task<ContentAnchorVerificationResult> VerifyAsync(
        ContentAnchorCheck check,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                check.IpfsGatewayUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ContentAnchorVerificationResult.Failure(
                    "content_anchor_invalid",
                    $"IPFS gateway returned {(int)response.StatusCode}.");
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var actualHash = ContentHashComputer.ComputeSha256Hex(bytes);

            if (!ContentHashComputer.HashesEqual(actualHash, check.ContentHash))
            {
                return ContentAnchorVerificationResult.Failure(
                    "content_anchor_invalid",
                    "IPFS content hash does not match contentHash.");
            }

            return ContentAnchorVerificationResult.Success();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ContentAnchorVerificationResult.Failure(
                "content_anchor_invalid",
                "IPFS gateway request timed out.");
        }
        catch (HttpRequestException ex)
        {
            return ContentAnchorVerificationResult.Failure(
                "content_anchor_invalid",
                $"IPFS gateway unreachable: {ex.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ContentAnchorVerificationResult.Failure(
                "content_anchor_invalid",
                "IPFS gateway request timed out.");
        }
    }
}

internal sealed class NullContentAnchorVerifier : IContentAnchorVerifier
{
    public Task<ContentAnchorVerificationResult> VerifyAsync(
        ContentAnchorCheck check,
        CancellationToken cancellationToken) =>
        Task.FromResult(ContentAnchorVerificationResult.Success());
}

internal sealed class ConfigurableContentAnchorVerifier : IContentAnchorVerifier
{
    private readonly IssuerOptions _options;
    private readonly HttpContentAnchorVerifier _httpVerifier;
    private readonly NullContentAnchorVerifier _nullVerifier;

    public ConfigurableContentAnchorVerifier(
        IOptions<IssuerOptions> options,
        HttpContentAnchorVerifier httpVerifier,
        NullContentAnchorVerifier nullVerifier)
    {
        _options = options.Value;
        _httpVerifier = httpVerifier;
        _nullVerifier = nullVerifier;
    }

    public Task<ContentAnchorVerificationResult> VerifyAsync(
        ContentAnchorCheck check,
        CancellationToken cancellationToken) =>
        Resolve().VerifyAsync(check, cancellationToken);

    private IContentAnchorVerifier Resolve() =>
        _options.ContentAnchor.VerifyEnabled ? _httpVerifier : _nullVerifier;
}
