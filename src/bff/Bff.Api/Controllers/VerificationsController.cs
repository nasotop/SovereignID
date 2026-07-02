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
    [HttpPost]
    [ProducesResponseType(typeof(VerificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(KiotaVerifier.ProblemDetails), StatusCodes.Status400BadRequest)]
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
