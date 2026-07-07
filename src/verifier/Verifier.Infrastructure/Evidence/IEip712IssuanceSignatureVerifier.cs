namespace Verifier.Infrastructure.Evidence;

internal interface IEip712IssuanceSignatureVerifier
{
    bool TryRecoverSigner(
        CredentialIssuanceMessage message,
        string signature,
        int chainId,
        string verifyingContract,
        out string recoveredAddress);
}

internal sealed record CredentialIssuanceMessage(
    string CredentialIdBytes32,
    string InstitutionIdBytes32,
    string ContentHash,
    string IpfsCid,
    string SubjectWallet);
