namespace Verifier.Application;

/// <summary>Emisor de la credencial (datos de <c>institutions</c>).</summary>
public sealed record IssuerReadModel(string Did, string DisplayName, string Code);

/// <summary>Anclas verificables de la credencial (IPFS / on-chain).</summary>
public sealed record CredentialAnchors(
    string IpfsCid,
    string ContentHash,
    string TransactionHash,
    int ChainId);

/// <summary>
/// Proyección de lectura de una credencial con su tipo y emisor, suficiente para computar el veredicto
/// y poblar el bloque <c>credential</c> de la respuesta.
/// </summary>
public sealed record CredentialReadModel(
    Guid Id,
    string TypeCode,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt,
    string SubjectDid,
    IssuerReadModel Issuer,
    CredentialAnchors Anchors);
