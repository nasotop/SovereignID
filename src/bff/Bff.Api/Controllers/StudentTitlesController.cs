using Bff.Api;
using Bff.Api.Models;
using Microsoft.AspNetCore.Mvc;
using KiotaIssuer = SovereignID.Bff.Clients.Issuer.Models;
using IssuerApiClient = SovereignID.Bff.Clients.Issuer.ApiClient;

namespace Bff.Api.Controllers;

[ApiController]
[Route("issuer/students")]
[Produces("application/json")]
public sealed class StudentTitlesController(IssuerApiClient issuer) : ControllerBase
{
    [HttpPost("{studentId:guid}/title")]
    [ProducesResponseType(typeof(StudentTitleLinked), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(KiotaIssuer.ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> LinkTitle(
        Guid studentId,
        [FromBody] LinkStudentTitleRequest request,
        CancellationToken cancellationToken) =>
        DownstreamResults.CreatedMappedAsync(
            () => issuer.Issuer.Students[studentId].Title
                .PostAsync(KiotaWireMappers.ToKiota(request), cancellationToken: cancellationToken),
            success => $"/issuer/students/{studentId}/title/{success.CredentialId}",
            KiotaWireMappers.ToWire);
}
