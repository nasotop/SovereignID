using Academy.Api.Models;
using Academy.Application;
using Microsoft.AspNetCore.Mvc;

namespace Academy.Api.Controllers;

[ApiController]
[Route("academy/invitations")]
[Produces("application/json")]
public sealed class InstitutionInvitationsController : ControllerBase
{
    private readonly AcademyService _academyService;

    public InstitutionInvitationsController(AcademyService academyService)
    {
        _academyService = academyService;
    }

    /// <summary>Acepta una invitacion y guarda la wallet MetaMask existente del usuario institucional.</summary>
    [HttpPost("accept")]
    [ProducesResponseType(typeof(InstitutionInvitationAccepted), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstitutionInvitationAccepted>> Accept(
        [FromBody] AcceptInstitutionInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.AcceptInvitationAsync(
            new AcceptInstitutionInvitationCommand(
                request.Token,
                request.WalletAddress,
                request.DisplayName),
            cancellationToken);

        return result switch
        {
            AcademySuccess<InstitutionInvitationAccepted> success => Ok(success.Value),
            AcademyFailureResult<InstitutionInvitationAccepted> failure => throw new AcademyFailureException(failure.Failure),
            _ => throw new InvalidOperationException("Unexpected academy result.")
        };
    }
}

