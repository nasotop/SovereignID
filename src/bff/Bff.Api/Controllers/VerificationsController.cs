using Bff.Api;
using Bff.Api.Models;
using Microsoft.AspNetCore.Mvc;
using KiotaVerifier = SovereignID.Bff.Clients.Verifier.Models;
using VerifierApiClient = SovereignID.Bff.Clients.Verifier.ApiClient;

namespace Bff.Api.Controllers;

[ApiController]
[Route("verifications")]
[Produces("application/json")]
public sealed class VerificationsController(VerifierApiClient verifier) : ControllerBase
{
    /// <summary>Verifica una Verifiable Credential por su UUID (pass-through al verifier).</summary>
    /// <remarks>Los veredictos de negocio se devuelven con <c>200</c> y el campo <c>result</c>. Los errores de protocolo usan RFC 7807 Problem Details con extensión <c>error</c>: <c>invalid_credential_id</c> (<c>400</c>) o <c>rate_limit_exceeded</c> (<c>429</c>).</remarks>
    [HttpPost]
    [ProducesResponseType(typeof(VerificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(KiotaVerifier.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(KiotaVerifier.ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> Verify(
        [FromBody] VerificationRequest request,
        CancellationToken cancellationToken)
    {
        var kiotaRequest = new KiotaVerifier.VerificationRequest
        {
            CredentialId = request.CredentialId,
        };

        return DownstreamResults.OkMappedAsync(
            () => verifier.Verifications.PostAsync(kiotaRequest, cancellationToken: cancellationToken),
            KiotaWireMappers.ToWire);
    }
}
