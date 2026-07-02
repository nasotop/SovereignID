using Bff.Api;
using Microsoft.AspNetCore.Mvc;
using SovereignID.Bff.Clients.Academy.Models;
using AcademyApiClient = SovereignID.Bff.Clients.Academy.ApiClient;

namespace Bff.Api.Controllers;

[ApiController]
[Route("academy/institutions")]
[Produces("application/json")]
public sealed class AcademyInstitutionsController(AcademyApiClient academy) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(InstitutionCreated), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> CreateInstitution(
        [FromBody] CreateInstitutionRequest request,
        CancellationToken cancellationToken) =>
        DownstreamResults.CreatedAsync(
            () => academy.Academy.Institutions.PostAsync(request, cancellationToken: cancellationToken),
            success => $"/academy/institutions/{success!.Institution!.Id}");

    [HttpGet("{institutionId:guid}")]
    [ProducesResponseType(typeof(InstitutionSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetInstitution(Guid institutionId, CancellationToken cancellationToken) =>
        DownstreamResults.OkAsync(() =>
            academy.Academy.Institutions[institutionId].GetAsync(cancellationToken: cancellationToken));

    [HttpPost("{institutionId:guid}/careers")]
    [ProducesResponseType(typeof(CareerSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> CreateCareer(
        Guid institutionId,
        [FromBody] CreateCareerRequest request,
        CancellationToken cancellationToken) =>
        DownstreamResults.CreatedAsync(
            () => academy.Academy.Institutions[institutionId].Careers
                .PostAsync(request, cancellationToken: cancellationToken),
            success => $"/academy/institutions/{institutionId}/careers/{success!.Id}");

    [HttpPost("{institutionId:guid}/students")]
    [ProducesResponseType(typeof(StudentSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> CreateStudent(
        Guid institutionId,
        [FromBody] CreateStudentRequest request,
        CancellationToken cancellationToken) =>
        DownstreamResults.CreatedAsync(
            () => academy.Academy.Institutions[institutionId].Students
                .PostAsync(request, cancellationToken: cancellationToken),
            success => $"/academy/institutions/{institutionId}/students/{success!.Id}");

    [HttpPost("{institutionId:guid}/invitations")]
    [ProducesResponseType(typeof(InstitutionInvitationCreated), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> CreateInvitation(
        Guid institutionId,
        [FromBody] CreateInstitutionInvitationRequest request,
        CancellationToken cancellationToken) =>
        DownstreamResults.CreatedAsync(
            () => academy.Academy.Institutions[institutionId].Invitations
                .PostAsync(request, cancellationToken: cancellationToken),
            success => $"/academy/institutions/{institutionId}/invitations/{success!.Id}");
}
