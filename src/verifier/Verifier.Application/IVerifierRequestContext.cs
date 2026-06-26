using System.Net;

namespace Verifier.Application;

/// <summary>
/// Metadatos técnicos del verificador anónimo (resueltos en la capa de entrada),
/// usados para enriquecer el registro en <c>verification_logs</c> sin acoplar la
/// infraestructura al pipeline HTTP.
/// </summary>
public interface IVerifierRequestContext
{
    IPAddress? ClientIp { get; }

    string? UserAgent { get; }
}
