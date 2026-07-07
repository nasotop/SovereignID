using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.Model;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using Verifier.Application;
using Verifier.Infrastructure.Blockchain;

namespace Verifier.Infrastructure.Evidence;

internal sealed class RpcCredentialRegistryReader : ICredentialRegistryReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Sha3Keccack Keccak = new();

    private static readonly string GetCredentialSelector =
        "0x" + Keccak.CalculateHash("getCredential(bytes32)")[..8];

    private readonly HttpClient _httpClient;
    private readonly EvidenceVerificationOptions _options;

    public RpcCredentialRegistryReader(HttpClient httpClient, IOptions<VerifierOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.Evidence;
    }

    public async Task<RegistryReadResult> ReadAsync(Guid credentialId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.RpcUrl) || string.IsNullOrWhiteSpace(_options.RegistryAddress))
        {
            return new RegistryReadResult(RegistryReadOutcome.Unavailable, null);
        }

        try
        {
            var credentialBytes = GuidToBytes32.Convert(credentialId);
            var callData = GetCredentialSelector + credentialBytes[2..].PadLeft(64, '0');

            var payload = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "eth_call",
                @params = new object[]
                {
                    new { to = _options.RegistryAddress, data = callData },
                    "latest"
                }
            };

            using var response = await _httpClient.PostAsJsonAsync(_options.RpcUrl, payload, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (document.RootElement.TryGetProperty("error", out _))
            {
                return new RegistryReadResult(RegistryReadOutcome.NotFound, null);
            }

            if (!document.RootElement.TryGetProperty("result", out var resultElement)
                || resultElement.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(resultElement.GetString())
                || resultElement.GetString() == "0x")
            {
                return new RegistryReadResult(RegistryReadOutcome.NotFound, null);
            }

            var decoded = DecodeGetCredentialResult(resultElement.GetString()!);
            return new RegistryReadResult(RegistryReadOutcome.Found, decoded);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new RegistryReadResult(RegistryReadOutcome.Unavailable, null);
        }
        catch (HttpRequestException)
        {
            return new RegistryReadResult(RegistryReadOutcome.Unavailable, null);
        }
        catch (TaskCanceledException)
        {
            return new RegistryReadResult(RegistryReadOutcome.Unavailable, null);
        }
    }

    private static OnChainCredentialData DecodeGetCredentialResult(string hexData)
    {
        var outputs = new[]
        {
            new Parameter("contentHash", "bytes32", 1),
            new Parameter("ipfsCid", "string", 2),
            new Parameter("institutionId", "bytes32", 3),
            new Parameter("issuer", "address", 4),
            new Parameter("subject", "address", 5),
            new Parameter("revoked", "bool", 6)
        };

        var decoded = new ParameterDecoder().DecodeDefaultData(hexData, outputs);
        return new OnChainCredentialData(
            $"0x{((byte[])decoded[0].Result).ToHex()}",
            (string)decoded[1].Result,
            $"0x{((byte[])decoded[2].Result).ToHex()}",
            ((string)decoded[3].Result).ToLowerInvariant(),
            ((string)decoded[4].Result).ToLowerInvariant(),
            (bool)decoded[5].Result);
    }
}

internal static class OnChainCoherence
{
    public static bool IsCoherent(OnChainCredentialData onChain, CredentialEvidence evidence)
    {
        return string.Equals(NormalizeHash(onChain.ContentHash), NormalizeHash(evidence.ContentHash), StringComparison.OrdinalIgnoreCase)
            && string.Equals(onChain.IpfsCid, evidence.IpfsCid, StringComparison.Ordinal)
            && string.Equals(onChain.InstitutionIdBytes32, GuidToBytes32.Convert(evidence.InstitutionId), StringComparison.OrdinalIgnoreCase)
            && string.Equals(onChain.Issuer, NormalizeAddress(evidence.IssuerWalletAddress ?? string.Empty), StringComparison.OrdinalIgnoreCase)
            && string.Equals(onChain.Subject, NormalizeAddress(evidence.SubjectWalletAddress), StringComparison.OrdinalIgnoreCase);
    }

    public static RegistryReadResult Evaluate(
        RegistryReadResult read,
        CredentialEvidence evidence)
    {
        if (read.Outcome != RegistryReadOutcome.Found || read.Data is null)
        {
            return read;
        }

        return IsCoherent(read.Data, evidence)
            ? read
            : new RegistryReadResult(RegistryReadOutcome.Incoherent, read.Data);
    }

    private static string NormalizeHash(string hash) =>
        hash.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hash.ToLowerInvariant() : $"0x{hash.ToLowerInvariant()}";

    private static string NormalizeAddress(string address) =>
        string.IsNullOrWhiteSpace(address) ? string.Empty : address.ToLowerInvariant();
}
