namespace Verifier.Domain;

/// <summary>
/// Booleanos por chequeo de verificación. <c>null</c> significa "no evaluado".
/// En v1 solo se computan <see cref="Found"/>, <see cref="NotRevoked"/> y <see cref="NotExpired"/>;
/// los chequeos con dependencia externa se devuelven <c>null</c>.
/// </summary>
public sealed record VerificationChecks(
    bool? Found,
    bool? NotRevoked,
    bool? NotExpired,
    bool? HashMatches,
    bool? OnChainExists,
    bool? SignatureValid);

/// <summary>Resultado completo de una verificación: el <see cref="Result"/> resumido más el detalle por chequeo.</summary>
public sealed record VerificationVerdict(VerificationResult Result, VerificationChecks Checks);
