namespace Verifier.Application;

/// <summary>Lectura de una credencial (con su tipo y emisor) por su UUID (<c>credentials.id</c>).</summary>
public interface ICredentialReadStore
{
    /// <summary>Devuelve la credencial resuelta, o <c>null</c> si el UUID no corresponde a ninguna fila.</summary>
    Task<CredentialReadModel?> GetByIdAsync(Guid credentialId, CancellationToken cancellationToken);
}
