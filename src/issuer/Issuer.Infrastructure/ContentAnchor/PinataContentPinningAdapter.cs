using System.Net.Http.Headers;
using System.Text.Json;
using Issuer.Application;
using Issuer.Application.ContentAnchor;
using Microsoft.Extensions.Options;

namespace Issuer.Infrastructure.ContentAnchor;

internal sealed class PinataContentPinningAdapter : IContentPinningAdapter
{
    private const string PinFileUrl = "https://api.pinata.cloud/pinning/pinFileToIPFS";

    private readonly HttpClient _httpClient;
    private readonly ContentAnchorOptions _options;

    public PinataContentPinningAdapter(HttpClient httpClient, IOptions<IssuerOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.ContentAnchor;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.IpfsTimeoutSeconds));
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.PinataApiKey)
        && !string.IsNullOrWhiteSpace(_options.PinataApiSecret);

    public async Task<PinResult> PinAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return PinResult.Failure("Pinata credentials are not configured.");
        }

        try
        {
            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(content.ToArray());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            form.Add(fileContent, "file", "credential.json");

            using var request = new HttpRequestMessage(HttpMethod.Post, PinFileUrl) { Content = form };
            request.Headers.Add("pinata_api_key", _options.PinataApiKey);
            request.Headers.Add("pinata_secret_api_key", _options.PinataApiSecret);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return PinResult.Failure($"Pinata returned {(int)response.StatusCode}: {Truncate(body)}");
            }

            using var json = JsonDocument.Parse(body);
            if (!json.RootElement.TryGetProperty("IpfsHash", out var hashProp)
                || hashProp.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(hashProp.GetString()))
            {
                return PinResult.Failure("Pinata response did not include IpfsHash.");
            }

            return PinResult.Success(hashProp.GetString()!);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return PinResult.Failure("Pinata request timed out.");
        }
        catch (HttpRequestException ex)
        {
            return PinResult.Failure($"Pinata request failed: {ex.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return PinResult.Failure("Pinata request timed out.");
        }
        catch (JsonException ex)
        {
            return PinResult.Failure($"Pinata response was not valid JSON: {ex.Message}");
        }
    }

    private static string Truncate(string value) =>
        value.Length <= 200 ? value : value[..200];
}
