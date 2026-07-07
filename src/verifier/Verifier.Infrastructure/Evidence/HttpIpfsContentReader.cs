using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Verifier.Application;

namespace Verifier.Infrastructure.Evidence;

internal sealed class HttpIpfsContentReader : IIpfsContentReader
{
    private readonly HttpClient _httpClient;
    private readonly EvidenceVerificationOptions _options;

    public HttpIpfsContentReader(HttpClient httpClient, IOptions<VerifierOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.Evidence;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.IpfsTimeoutSeconds));
    }

    public async Task<IpfsReadResult> ReadAndHashAsync(
        string gatewayUrl,
        string expectedContentHash,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(gatewayUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new IpfsReadResult(null);
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var hash = ComputeSha256Hex(bytes);
            var matches = string.Equals(NormalizeHash(hash), NormalizeHash(expectedContentHash), StringComparison.OrdinalIgnoreCase);
            return new IpfsReadResult(matches);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new IpfsReadResult(null);
        }
        catch (HttpRequestException)
        {
            return new IpfsReadResult(null);
        }
        catch (TaskCanceledException)
        {
            return new IpfsReadResult(null);
        }
    }

    private static string ComputeSha256Hex(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return $"0x{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static string NormalizeHash(string hash) =>
        hash.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hash.ToLowerInvariant() : $"0x{hash.ToLowerInvariant()}";
}
