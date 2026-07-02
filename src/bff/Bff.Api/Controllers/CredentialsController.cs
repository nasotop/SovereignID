using Bff.Api;
using Bff.Api.Models;
using Microsoft.AspNetCore.Mvc;
using KiotaIssuer = SovereignID.Bff.Clients.Issuer.Models;
using IssuerApiClient = SovereignID.Bff.Clients.Issuer.ApiClient;

namespace Bff.Api.Controllers;

[ApiController]
[Route("issuer/credentials")]
[Produces("application/json")]
public sealed class CredentialsController(IssuerApiClient issuer) : ControllerBase
{
    [HttpGet("{credentialId:guid}")]
    [ProducesResponseType(typeof(HolderCredentialDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Get(Guid credentialId, CancellationToken cancellationToken) =>
        DownstreamResults.OkMappedAsync(
            () => issuer.Issuer.Credentials[credentialId].GetAsync(cancellationToken: cancellationToken),
            KiotaWireMappers.ToWire);

    [HttpPost("{credentialId:guid}/revoke")]
    [ProducesResponseType(typeof(CredentialRevoked), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> RevokeCredential(
        Guid credentialId,
        [FromBody] RevokeCredentialRequest request,
        CancellationToken cancellationToken) =>
        DownstreamResults.OkMappedAsync(
            () => issuer.Issuer.Credentials[credentialId].Revoke
                .PostAsync(KiotaWireMappers.ToKiota(request), cancellationToken: cancellationToken),
            KiotaWireMappers.ToWire);
}
