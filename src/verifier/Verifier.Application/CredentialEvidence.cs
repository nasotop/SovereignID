namespace Verifier.Application;

/// <summary>Entrada para la verificación de evidencia externa de una credencial existente.</summary>
public sealed record CredentialEvidence(
    Guid CredentialId,
    Guid InstitutionId,
    string ContentHash,
    string IpfsCid,
    string IpfsGatewayUrl,
    string? IssuerWalletAddress,
    string SubjectWalletAddress,
    string? Eip712Signature,
    int ChainId);

/// <summary>Resultado de los chequeos con dependencia externa.</summary>
public sealed record CredentialEvidenceChecks(
    bool? HashMatches,
    bool? OnChainExists,
    bool? OnChainRevoked,
    bool? SignatureValid,
    string? ValidationSource);
