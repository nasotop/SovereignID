using Academy.Api.Models;
using Academy.Application;
using Microsoft.AspNetCore.Mvc;

namespace Academy.Api.Controllers;

[ApiController]
[Route("academy/institutions")]
[Produces("application/json")]
public sealed class InstitutionsController : ControllerBase
{
    private readonly AcademyService _academyService;

    public InstitutionsController(AcademyService academyService)
    {
        _academyService = academyService;
    }

    /// <summary>Crea una institucion y envia una invitacion para vincular una wallet MetaMask existente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(InstitutionCreated), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InstitutionCreated>> CreateInstitution(
        [FromBody] CreateInstitutionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.CreateInstitutionAsync(
            new CreateInstitutionCommand(
                request.Code,
                request.LegalName,
                request.DisplayName,
                request.ContactEmail,
                request.CountryCode,
                request.WebsiteUrl),
            cancellationToken);

        return FromResult(result, success => CreatedAtAction(nameof(GetInstitution), new { institutionId = success.Institution.Id }, success));
    }

    /// <summary>Consulta una institucion por identificador.</summary>
    [HttpGet("{institutionId:guid}")]
    [ProducesResponseType(typeof(InstitutionSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstitutionSummary>> GetInstitution(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.GetInstitutionAsync(institutionId, cancellationToken);
        return FromResult(result, success => Ok(success));
    }

    /// <summary>Crea una carrera dentro de una institucion.</summary>
    [HttpPost("{institutionId:guid}/careers")]
    [ProducesResponseType(typeof(CareerSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CareerSummary>> CreateCareer(
        Guid institutionId,
        [FromBody] CreateCareerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.CreateCareerAsync(
            new CreateCareerCommand(institutionId, request.Code, request.Name),
            cancellationToken);

        return FromResult(result, success => Created($"/academy/institutions/{institutionId}/careers/{success.Id}", success));
    }

    /// <summary>Crea un estudiante y, si se informa, vincula su wallet MetaMask existente como primaria.</summary>
    [HttpPost("{institutionId:guid}/students")]
    [ProducesResponseType(typeof(StudentSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudentSummary>> CreateStudent(
        Guid institutionId,
        [FromBody] CreateStudentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.CreateStudentAsync(
            new CreateStudentCommand(institutionId, request.ExternalReference, request.EnrollmentYear, request.WalletAddress),
            cancellationToken);

        return FromResult(result, success => Created($"/academy/institutions/{institutionId}/students/{success.Id}", success));
    }

    /// <summary>Crea una invitacion para que un usuario de institucion vincule una wallet MetaMask existente.</summary>
    [HttpPost("{institutionId:guid}/invitations")]
    [ProducesResponseType(typeof(InstitutionInvitationCreated), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstitutionInvitationCreated>> CreateInvitation(
        Guid institutionId,
        [FromBody] CreateInstitutionInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.CreateInvitationAsync(
            new CreateInstitutionInvitationCommand(institutionId, request.Email, request.Role, request.CreatedByUserId),
            cancellationToken);

        return FromResult(result, success => Created($"/academy/institutions/{institutionId}/invitations/{success.Id}", success));
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

