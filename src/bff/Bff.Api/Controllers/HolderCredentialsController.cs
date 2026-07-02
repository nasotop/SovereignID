using Bff.Api;
using Bff.Api.Models;
using Microsoft.AspNetCore.Mvc;
using KiotaIssuer = SovereignID.Bff.Clients.Issuer.Models;
using IssuerApiClient = SovereignID.Bff.Clients.Issuer.ApiClient;

namespace Bff.Api.Controllers;

[ApiController]
[Route("issuer/holders/me/credentials")]
[Produces("application/json")]
public sealed class HolderCredentialsController(IssuerApiClient issuer) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HolderCredentialSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> List(CancellationToken cancellationToken) =>
        DownstreamResults.OkMappedListAsync(
            () => issuer.Issuer.Holders.Me.Credentials.GetAsync(cancellationToken: cancellationToken),
            KiotaWireMappers.ToWire);

    [HttpGet("{credentialId:guid}")]
    [ProducesResponseType(typeof(HolderCredentialDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Get(Guid credentialId, CancellationToken cancellationToken) =>
        DownstreamResults.OkMappedAsync(
            () => issuer.Issuer.Holders.Me.Credentials[credentialId]
                .GetAsync(cancellationToken: cancellationToken),
            KiotaWireMappers.ToWire);
}
