using Bff.Api;
using Microsoft.AspNetCore.Mvc;
using SovereignID.Bff.Clients;
using SovereignID.Bff.Clients.Academy.Models;

namespace Bff.Api.Controllers;

[ApiController]
[Route("academy/invitations")]
[Produces("application/json")]
public sealed class AcademyInvitationsController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpPost("accept")]
    [ProducesResponseType(typeof(InstitutionInvitationAccepted), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Accept(
        [FromBody] AcceptInstitutionInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(DependencyInjection.AcademyDirectHttpClientName);
        using var response = await httpClient.PostAsJsonAsync(
            "academy/invitations/accept",
            request,
            cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = contentType,
        };
    }
}
