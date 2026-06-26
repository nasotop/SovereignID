namespace Verifier.Api.Models;

/// <summary>Cuerpo de <c>POST /verifications</c>.</summary>
public sealed record VerificationRequest(string? CredentialId);

/// <summary>Booleanos por chequeo; <c>null</c> = no evaluado.</summary>
public sealed record VerificationChecksResponse(
    bool? Found,
    bool? NotRevoked,
    bool? NotExpired,
    bool? HashMatches,
    bool? OnChainExists,
    bool? SignatureValid);

/// <summary>Emisor de la credencial (datos de <c>institutions</c>).</summary>
public sealed record IssuerResponse(string Did, string DisplayName, string Code);

/// <summary>Anclas verificables (IPFS / on-chain).</summary>
public sealed record AnchorsResponse(string IpfsCid, string ContentHash, string TransactionHash, int ChainId);

/// <summary>Bloque <c>credential</c>; presente cuando la credencial existe.</summary>
public sealed record CredentialResponse(
    Guid Id,
    string Type,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ExpiresAt,
    string SubjectDid,
    IssuerResponse Issuer,
    AnchorsResponse Anchors);

/// <summary>Respuesta de <c>POST /verifications</c>: veredicto resumido, chequeos y credencial (o <c>null</c>).</summary>
public sealed record VerificationResponse(
    string Result,
    VerificationChecksResponse Checks,
    CredentialResponse? Credential);
