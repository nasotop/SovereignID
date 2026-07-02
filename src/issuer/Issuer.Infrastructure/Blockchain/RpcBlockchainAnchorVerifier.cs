using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Issuer.Application;
using Microsoft.Extensions.Options;

namespace Issuer.Infrastructure.Blockchain;

internal sealed class RpcBlockchainAnchorVerifier : IBlockchainAnchorVerifier
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IssuerOptions _options;

    public RpcBlockchainAnchorVerifier(HttpClient httpClient, IOptions<IssuerOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<BlockchainAnchorVerificationResult> VerifyIssueAnchorAsync(
        BlockchainAnchorProof proof,
        string expectedIssuerWalletAddress,
        CancellationToken cancellationToken)
    {
        var formatError = ValidateProofFormat(proof);
        if (formatError is not null)
        {
            return formatError;
        }

        var receipt = await GetTransactionReceiptAsync(proof.TransactionHash, cancellationToken);
        if (receipt is null)
        {
            return BlockchainAnchorVerificationResult.Failure(
                "blockchain_tx_not_found",
                "Transaction receipt was not found on-chain.");
        }

        if (!string.Equals(receipt.From, NormalizeAddress(expectedIssuerWalletAddress), StringComparison.OrdinalIgnoreCase))
        {
            return BlockchainAnchorVerificationResult.Failure(
                "blockchain_issuer_mismatch",
                "Transaction sender does not match institution issuer wallet.");
        }

        if (receipt.BlockNumber < proof.BlockNumber)
        {
            return BlockchainAnchorVerificationResult.Failure(
                "blockchain_block_mismatch",
                "Submitted block number is greater than on-chain block number.");
        }

        return BlockchainAnchorVerificationResult.Success();
    }

    public async Task<BlockchainAnchorVerificationResult> VerifyRevocationAnchorAsync(
        BlockchainAnchorProof proof,
        string expectedIssuerWalletAddress,
        Guid credentialId,
        CancellationToken cancellationToken)
    {
        var formatError = ValidateProofFormat(proof with { TransactionHash = proof.TransactionHash });
        if (formatError is not null)
        {
            return formatError;
        }

        var receipt = await GetTransactionReceiptAsync(proof.TransactionHash, cancellationToken);
        if (receipt is null)
        {
            return BlockchainAnchorVerificationResult.Failure(
                "blockchain_revocation_tx_not_found",
                "Revocation transaction receipt was not found on-chain.");
        }

        if (!string.Equals(receipt.From, NormalizeAddress(expectedIssuerWalletAddress), StringComparison.OrdinalIgnoreCase))
        {
            return BlockchainAnchorVerificationResult.Failure(
                "blockchain_issuer_mismatch",
                "Revocation transaction sender does not match institution issuer wallet.");
        }

        return BlockchainAnchorVerificationResult.Success();
    }

    private static BlockchainAnchorVerificationResult? ValidateProofFormat(BlockchainAnchorProof proof)
    {
        if (!IsHexHash(proof.TransactionHash) || !IsHexHash(proof.ContentHash))
        {
            return BlockchainAnchorVerificationResult.Failure(
                "invalid_blockchain_proof",
                "transactionHash and contentHash must be 0x-prefixed 32-byte hex values.");
        }

        if (string.IsNullOrWhiteSpace(proof.Eip712Signature))
        {
            return BlockchainAnchorVerificationResult.Failure(
                "invalid_blockchain_proof",
                "eip712Signature is required.");
        }

        return null;
    }

    private async Task<TransactionReceipt?> GetTransactionReceiptAsync(
        string transactionHash,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "eth_getTransactionReceipt",
            @params = new[] { transactionHash }
        };

        using var response = await _httpClient.PostAsJsonAsync(_options.Blockchain.RpcUrl, payload, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var from = result.GetProperty("from").GetString();
        var blockHex = result.GetProperty("blockNumber").GetString();
        var status = result.GetProperty("status").GetString();

        if (!string.Equals(status, "0x1", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new TransactionReceipt(
            from ?? string.Empty,
            ParseHexLong(blockHex));
    }

    private static long ParseHexLong(string? value) =>
        long.Parse(value![2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    private static bool IsHexHash(string value) =>
        value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
        && value.Length == 66
        && value[2..].All(static c => Uri.IsHexDigit(c));

    private static string NormalizeAddress(string address) => address.ToLowerInvariant();

    private sealed record TransactionReceipt(string From, long BlockNumber);
}
