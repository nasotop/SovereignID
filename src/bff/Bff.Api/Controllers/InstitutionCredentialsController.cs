using Bff.Api;
using Bff.Api.Models;
using Microsoft.AspNetCore.Mvc;
using KiotaIssuer = SovereignID.Bff.Clients.Issuer.Models;
using IssuerApiClient = SovereignID.Bff.Clients.Issuer.ApiClient;

namespace Bff.Api.Controllers;

[ApiController]
[Route("issuer/institutions")]
[Produces("application/json")]
public sealed class InstitutionCredentialsController(IssuerApiClient issuer) : ControllerBase
{
    [HttpGet("{institutionId:guid}/credentials")]
    [ProducesResponseType(typeof(IReadOnlyList<CredentialSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<IActionResult> ListCredentials(
        Guid institutionId,
        CancellationToken cancellationToken) =>
        DownstreamResults.OkMappedListAsync(
            () => issuer.Issuer.Institutions[institutionId].Credentials.GetAsync(cancellationToken: cancellationToken),
            KiotaWireMappers.ToWire);
}
