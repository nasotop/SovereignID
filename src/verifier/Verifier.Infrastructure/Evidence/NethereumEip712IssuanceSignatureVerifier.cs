using System.Text.Json;
using Microsoft.Extensions.Options;
using Nethereum.Signer.EIP712;
using Verifier.Application;

namespace Verifier.Infrastructure.Evidence;

internal static class CredentialIssuanceTypedData
{
    public const string DomainName = "SovereignID";
    public const string DomainVersion = "1";

    public static string BuildJson(
        int chainId,
        string verifyingContract,
        CredentialIssuanceMessage message)
    {
        var typedData = new
        {
            types = new Dictionary<string, object>
            {
                ["EIP712Domain"] = new[]
                {
                    new { name = "name", type = "string" },
                    new { name = "version", type = "string" },
                    new { name = "chainId", type = "uint256" },
                    new { name = "verifyingContract", type = "address" }
                },
                ["CredentialIssuance"] = new[]
                {
                    new { name = "credentialId", type = "bytes32" },
                    new { name = "institutionId", type = "bytes32" },
                    new { name = "contentHash", type = "bytes32" },
                    new { name = "ipfsCid", type = "string" },
                    new { name = "subjectWallet", type = "address" }
                }
            },
            primaryType = "CredentialIssuance",
            domain = new
            {
                name = DomainName,
                version = DomainVersion,
                chainId,
                verifyingContract
            },
            message = new
            {
                credentialId = message.CredentialIdBytes32,
                institutionId = message.InstitutionIdBytes32,
                contentHash = message.ContentHash,
                ipfsCid = message.IpfsCid,
                subjectWallet = message.SubjectWallet
            }
        };

        return JsonSerializer.Serialize(typedData);
    }
}

internal sealed class NethereumEip712IssuanceSignatureVerifier : IEip712IssuanceSignatureVerifier
{
    private readonly EvidenceVerificationOptions _options;
    private readonly Eip712TypedDataSigner _signer = new();

    public NethereumEip712IssuanceSignatureVerifier(IOptions<VerifierOptions> options) =>
        _options = options.Value.Evidence;

    public bool TryRecoverSigner(
        CredentialIssuanceMessage message,
        string signature,
        int chainId,
        string verifyingContract,
        out string recoveredAddress)
    {
        recoveredAddress = string.Empty;

        if (!IsValidSignatureFormat(signature))
        {
            return false;
        }

        try
        {
            var typedDataJson = CredentialIssuanceTypedData.BuildJson(
                chainId,
                verifyingContract,
                message);

            recoveredAddress = _signer.RecoverFromSignatureV4(typedDataJson, signature).ToLowerInvariant();
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    internal static bool IsValidSignatureFormat(string? signature) =>
        !string.IsNullOrWhiteSpace(signature)
        && signature.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
        && signature.Length == 132
        && signature[2..].All(Uri.IsHexDigit);
}

internal sealed class InMemoryEip712Verifier : IEip712IssuanceSignatureVerifier
{
    private string _expectedWallet = string.Empty;
    private bool _signatureValid = true;

    public void Configure(string expectedWallet, bool signatureValid = true)
    {
        _expectedWallet = expectedWallet.ToLowerInvariant();
        _signatureValid = signatureValid;
    }

    public bool TryRecoverSigner(
        CredentialIssuanceMessage message,
        string signature,
        int chainId,
        string verifyingContract,
        out string recoveredAddress)
    {
        if (!NethereumEip712IssuanceSignatureVerifier.IsValidSignatureFormat(signature) || !_signatureValid)
        {
            recoveredAddress = string.Empty;
            return false;
        }

        recoveredAddress = _expectedWallet;
        return true;
    }
}
