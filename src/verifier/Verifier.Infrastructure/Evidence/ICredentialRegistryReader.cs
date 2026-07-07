namespace Verifier.Infrastructure.Evidence;

internal enum RegistryReadOutcome
{
    Found,
    Incoherent,
    NotFound,
    Unavailable
}

internal sealed record OnChainCredentialData(
    string ContentHash,
    string IpfsCid,
    string InstitutionIdBytes32,
    string Issuer,
    string Subject,
    bool Revoked);

internal sealed record RegistryReadResult(
    RegistryReadOutcome Outcome,
    OnChainCredentialData? Data);

internal interface ICredentialRegistryReader
{
    Task<RegistryReadResult> ReadAsync(Guid credentialId, CancellationToken cancellationToken);
}
