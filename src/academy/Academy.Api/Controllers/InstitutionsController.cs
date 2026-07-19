using Academy.Api.Models;
using Academy.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SovereignID.Authorization;

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
    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
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

    /// <summary>Lista instituciones activas para administracion de plataforma.</summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
    [ProducesResponseType(typeof(IReadOnlyList<InstitutionSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InstitutionSummary>>> ListInstitutions(
        CancellationToken cancellationToken)
    {
        var result = await _academyService.ListInstitutionsAsync(cancellationToken);
        return FromResult(result, success => Ok(success));
    }

    /// <summary>Consulta una institucion por identificador.</summary>
    [HttpGet("{institutionId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.PlatformOrInstitutionMember)]
    [ProducesResponseType(typeof(InstitutionSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstitutionSummary>> GetInstitution(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.GetInstitutionAsync(institutionId, cancellationToken);
        return FromResult(result, success => Ok(success));
    }

    /// <summary>Actualiza datos editables de una institucion sin cambiar su codigo institucional.</summary>
    [HttpPatch("{institutionId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.InstitutionAdmin)]
    [ProducesResponseType(typeof(InstitutionSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstitutionSummary>> UpdateInstitution(
        Guid institutionId,
        [FromBody] UpdateInstitutionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.UpdateInstitutionAsync(
            new UpdateInstitutionCommand(
                institutionId,
                request.LegalName,
                request.DisplayName,
                request.CountryCode,
                request.WebsiteUrl,
                request.IsActive),
            cancellationToken);

        return FromResult(result, success => Ok(success));
    }

    /// <summary>Crea una carrera dentro de una institucion.</summary>
    [HttpPost("{institutionId:guid}/careers")]
    [Authorize(Policy = AuthorizationPolicies.InstitutionAdmin)]
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

    /// <summary>Lista carreras de una institucion.</summary>
    [HttpGet("{institutionId:guid}/careers")]
    [Authorize(Policy = AuthorizationPolicies.PlatformOrInstitutionMember)]
    [ProducesResponseType(typeof(IReadOnlyList<CareerSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CareerSummary>>> ListCareers(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.ListCareersAsync(institutionId, cancellationToken);
        return FromResult(result, success => Ok(success));
    }

    /// <summary>Consulta una carrera de una institucion.</summary>
    [HttpGet("{institutionId:guid}/careers/{careerId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.PlatformOrInstitutionMember)]
    [ProducesResponseType(typeof(CareerSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CareerSummary>> GetCareer(
        Guid institutionId,
        Guid careerId,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.GetCareerAsync(institutionId, careerId, cancellationToken);
        return FromResult(result, success => Ok(success));
    }

    /// <summary>Actualiza codigo y nombre de una carrera.</summary>
    [HttpPatch("{institutionId:guid}/careers/{careerId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.InstitutionAdmin)]
    [ProducesResponseType(typeof(CareerSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CareerSummary>> UpdateCareer(
        Guid institutionId,
        Guid careerId,
        [FromBody] UpdateCareerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.UpdateCareerAsync(
            new UpdateCareerCommand(institutionId, careerId, request.Code, request.Name),
            cancellationToken);

        return FromResult(result, success => Ok(success));
    }

    /// <summary>Desactiva una carrera sin borrar su historial.</summary>
    [HttpDelete("{institutionId:guid}/careers/{careerId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.InstitutionAdmin)]
    [ProducesResponseType(typeof(CareerSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CareerSummary>> DeactivateCareer(
        Guid institutionId,
        Guid careerId,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.DeactivateCareerAsync(institutionId, careerId, cancellationToken);
        return FromResult(result, success => Ok(success));
    }

    /// <summary>Crea un estudiante y, si se informa, vincula su wallet MetaMask existente como primaria.</summary>
    [HttpPost("{institutionId:guid}/students")]
    [Authorize(Policy = AuthorizationPolicies.InstitutionAdmin)]
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

    /// <summary>Lista estudiantes de una institucion.</summary>
    [HttpGet("{institutionId:guid}/students")]
    [Authorize(Policy = AuthorizationPolicies.PlatformOrInstitutionMember)]
    [ProducesResponseType(typeof(IReadOnlyList<StudentSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<StudentSummary>>> ListStudents(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.ListStudentsAsync(institutionId, cancellationToken);
        return FromResult(result, success => Ok(success));
    }

    /// <summary>Consulta un estudiante de una institucion.</summary>
    [HttpGet("{institutionId:guid}/students/{studentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.PlatformOrInstitutionMember)]
    [ProducesResponseType(typeof(StudentSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentSummary>> GetStudent(
        Guid institutionId,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.GetStudentAsync(institutionId, studentId, cancellationToken);
        return FromResult(result, success => Ok(success));
    }

    /// <summary>Vincula manualmente una wallet existente a un estudiante.</summary>
    [HttpPost("{institutionId:guid}/students/{studentId:guid}/wallets")]
    [Authorize(Policy = AuthorizationPolicies.InstitutionAdmin)]
    [ProducesResponseType(typeof(StudentWalletSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentWalletSummary>> AddStudentWallet(
        Guid institutionId,
        Guid studentId,
        [FromBody] AddStudentWalletRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.AddStudentWalletAsync(
            new AddStudentWalletCommand(institutionId, studentId, request.WalletAddress, request.MakePrimary),
            cancellationToken);

        return FromResult(result, success => Created($"/academy/institutions/{institutionId}/students/{studentId}/wallets/{success.Id}", success));
    }

    /// <summary>Crea una invitacion para que un usuario de institucion vincule una wallet MetaMask existente.</summary>
    [HttpPost("{institutionId:guid}/invitations")]
    [Authorize(Policy = AuthorizationPolicies.InstitutionAdmin)]
    [ProducesResponseType(typeof(InstitutionInvitationCreated), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstitutionInvitationCreated>> CreateInvitation(
        Guid institutionId,
        [FromBody] CreateInstitutionInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var createdByUserId = User.GetUserId() ?? request.CreatedByUserId;
        var result = await _academyService.CreateInvitationAsync(
            new CreateInstitutionInvitationCommand(institutionId, request.Email, request.Role, createdByUserId),
            cancellationToken);

        return FromResult(result, success => Created($"/academy/institutions/{institutionId}/invitations/{success.Id}", success));
    }

    /// <summary>Crea una invitacion para un usuario institucional desde el recurso users.</summary>
    [HttpPost("{institutionId:guid}/users/invitations")]
    [Authorize(Policy = AuthorizationPolicies.InstitutionAdmin)]
    [ProducesResponseType(typeof(InstitutionInvitationCreated), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<ActionResult<InstitutionInvitationCreated>> CreateUserInvitation(
        Guid institutionId,
        [FromBody] CreateInstitutionInvitationRequest request,
        CancellationToken cancellationToken) =>
        CreateInvitation(institutionId, request, cancellationToken);

    /// <summary>Lista usuarios institucionales y sus roles.</summary>
    [HttpGet("{institutionId:guid}/users")]
    [Authorize(Policy = AuthorizationPolicies.InstitutionAdmin)]
    [ProducesResponseType(typeof(IReadOnlyList<InstitutionUserSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<InstitutionUserSummary>>> ListInstitutionUsers(
        Guid institutionId,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.ListInstitutionUsersAsync(institutionId, cancellationToken);
        return FromResult(result, success => Ok(success));
    }

    /// <summary>Cambia el rol de un usuario institucional.</summary>
    [HttpPatch("{institutionId:guid}/users/{userId:guid}/role")]
    [Authorize(Policy = AuthorizationPolicies.InstitutionAdmin)]
    [ProducesResponseType(typeof(InstitutionUserSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstitutionUserSummary>> UpdateInstitutionUserRole(
        Guid institutionId,
        Guid userId,
        [FromBody] UpdateInstitutionUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.UpdateInstitutionUserRoleAsync(
            institutionId,
            userId,
            request.Role,
            cancellationToken);

        return FromResult(result, success => Ok(success));
    }

    /// <summary>Revoca el acceso de un usuario a una institucion.</summary>
    [HttpDelete("{institutionId:guid}/users/{userId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.InstitutionAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> RevokeInstitutionUser(
        Guid institutionId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _academyService.RevokeInstitutionUserAsync(institutionId, userId, cancellationToken);
        return FromResult(result, _ => NoContent());
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

