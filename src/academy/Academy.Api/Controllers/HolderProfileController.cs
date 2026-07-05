using Academy.Api.Models;
using Academy.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SovereignID.Authorization;

namespace Academy.Api.Controllers;

[ApiController]
[Route("academy/holders/me")]
[Authorize(Policy = AuthorizationPolicies.HolderAuthenticated)]
[Produces("application/json")]
public sealed class HolderProfileController : ControllerBase
{
    private readonly AcademyService _academyService;

    public HolderProfileController(AcademyService academyService)
    {
        _academyService = academyService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(HolderDashboard), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HolderDashboard>> GetMe(CancellationToken cancellationToken)
    {
        var result = await _academyService.GetHolderDashboardAsync(
            User.FindFirst("address")?.Value,
            User.FindFirst("did")?.Value,
            cancellationToken);

        return FromResult(result, success => Ok(success));
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(HolderProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HolderProfile>> UpdateProfile(
        [FromBody] UpdateHolderProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.UpdateHolderProfileAsync(
            new UpdateHolderProfileCommand(
                User.FindFirst("address")?.Value ?? string.Empty,
                User.FindFirst("did")?.Value ?? string.Empty,
                request.DisplayName,
                request.FullName,
                request.BirthDate,
                request.ContactEmail,
                request.CountryCode,
                request.PhoneNumber),
            cancellationToken);

        return FromResult(result, success => Ok(success));
    }

    private static ActionResult<T> FromResult<T>(
        AcademyResult<T> result,
        Func<T, ActionResult<T>> onSuccess) =>
        result switch
        {
            AcademySuccess<T> success => onSuccess(success.Value),
            AcademyFailureResult<T> failure => throw new AcademyFailureException(failure.Failure),
            _ => throw new InvalidOperationException("Unexpected academy result.")
        };
}
