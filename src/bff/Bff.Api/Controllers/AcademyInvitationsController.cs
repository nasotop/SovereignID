using Bff.Api;
using Microsoft.AspNetCore.Mvc;
using SovereignID.Bff.Clients.Academy.Models;
using AcademyApiClient = SovereignID.Bff.Clients.Academy.ApiClient;

namespace Bff.Api.Controllers;

[ApiController]
[Route("academy/invitations")]
[Produces("application/json")]
public sealed class AcademyInvitationsController(AcademyApiClient academy) : ControllerBase
{
    [HttpPost("accept")]
    [ProducesResponseType(typeof(InstitutionInvitationAccepted), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Accept(
        [FromBody] AcceptInstitutionInvitationRequest request,
        CancellationToken cancellationToken) =>
        DownstreamResults.OkAsync(() =>
            academy.Academy.Invitations.Accept.PostAsync(request, cancellationToken: cancellationToken));
}
