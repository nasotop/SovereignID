using Bff.Api;
using Bff.Api.Models;
using Microsoft.AspNetCore.Mvc;
using SovereignID.Bff.Clients.Academy.Models;
using SovereignID.Bff.Clients;
using AcademyApiClient = SovereignID.Bff.Clients.Academy.ApiClient;

namespace Bff.Api.Controllers;

[ApiController]
[Route("academy/institutions")]
[Produces("application/json")]
public sealed class AcademyInstitutionsController(
    AcademyApiClient academy,
    IHttpClientFactory httpClientFactory) : ControllerBase
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

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InstitutionSummary>), StatusCodes.Status200OK)]
    public Task<IActionResult> ListInstitutions(CancellationToken cancellationToken) =>
        SendAcademyAsync(
            new HttpRequestMessage(HttpMethod.Get, "academy/institutions"),
            cancellationToken);

    [HttpGet("{institutionId:guid}")]
    [ProducesResponseType(typeof(InstitutionSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetInstitution(Guid institutionId, CancellationToken cancellationToken) =>
        DownstreamResults.OkAsync(() =>
            academy.Academy.Institutions[institutionId].GetAsync(cancellationToken: cancellationToken));

    [HttpPatch("{institutionId:guid}")]
    [ProducesResponseType(typeof(InstitutionSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateInstitution(
        Guid institutionId,
        [FromBody] Bff.Api.Models.UpdateInstitutionRequest request,
        CancellationToken cancellationToken) =>
        SendAcademyAsync(
            CreateJsonRequest(
                HttpMethod.Patch,
                $"academy/institutions/{institutionId}",
                request),
            cancellationToken);

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

    [HttpGet("{institutionId:guid}/careers")]
    [ProducesResponseType(typeof(IReadOnlyList<CareerSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ListCareers(
        Guid institutionId,
        CancellationToken cancellationToken) =>
        SendAcademyAsync(
            new HttpRequestMessage(HttpMethod.Get, $"academy/institutions/{institutionId}/careers"),
            cancellationToken);

    [HttpGet("{institutionId:guid}/careers/{careerId:guid}")]
    [ProducesResponseType(typeof(CareerSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetCareer(
        Guid institutionId,
        Guid careerId,
        CancellationToken cancellationToken) =>
        SendAcademyAsync(
            new HttpRequestMessage(HttpMethod.Get, $"academy/institutions/{institutionId}/careers/{careerId}"),
            cancellationToken);

    [HttpPatch("{institutionId:guid}/careers/{careerId:guid}")]
    [ProducesResponseType(typeof(CareerSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> UpdateCareer(
        Guid institutionId,
        Guid careerId,
        [FromBody] Bff.Api.Models.UpdateCareerRequest request,
        CancellationToken cancellationToken) =>
        SendAcademyAsync(
            CreateJsonRequest(
                HttpMethod.Patch,
                $"academy/institutions/{institutionId}/careers/{careerId}",
                request),
            cancellationToken);

    [HttpDelete("{institutionId:guid}/careers/{careerId:guid}")]
    [ProducesResponseType(typeof(CareerSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> DeactivateCareer(
        Guid institutionId,
        Guid careerId,
        CancellationToken cancellationToken) =>
        SendAcademyAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"academy/institutions/{institutionId}/careers/{careerId}"),
            cancellationToken);

    [HttpPost("{institutionId:guid}/students")]
    [ProducesResponseType(typeof(StudentSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status409Conflict)]
    public Task<IActionResult> CreateStudent(
        Guid institutionId,
        [FromBody] Bff.Api.Models.CreateStudentRequest request,
        CancellationToken cancellationToken) =>
        SendAcademyAsync(
            CreateJsonRequest(
                HttpMethod.Post,
                $"academy/institutions/{institutionId}/students",
                request),
            cancellationToken);

    [HttpGet("{institutionId:guid}/students")]
    [ProducesResponseType(typeof(IReadOnlyList<StudentSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ListStudents(
        Guid institutionId,
        CancellationToken cancellationToken) =>
        SendAcademyAsync(
            new HttpRequestMessage(HttpMethod.Get, $"academy/institutions/{institutionId}/students"),
            cancellationToken);

    [HttpGet("{institutionId:guid}/students/{studentId:guid}")]
    [ProducesResponseType(typeof(StudentSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetStudent(
        Guid institutionId,
        Guid studentId,
        CancellationToken cancellationToken) =>
        SendAcademyAsync(
            new HttpRequestMessage(HttpMethod.Get, $"academy/institutions/{institutionId}/students/{studentId}"),
            cancellationToken);

    [HttpPost("{institutionId:guid}/students/{studentId:guid}/wallets")]
    [ProducesResponseType(typeof(Bff.Api.Models.StudentWalletSummary), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> AddStudentWallet(
        Guid institutionId,
        Guid studentId,
        [FromBody] Bff.Api.Models.AddStudentWalletRequest request,
        CancellationToken cancellationToken) =>
        SendAcademyAsync(
            CreateJsonRequest(
                HttpMethod.Post,
                $"academy/institutions/{institutionId}/students/{studentId}/wallets",
                request),
            cancellationToken);

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

    [HttpPost("{institutionId:guid}/users/invitations")]
    [ProducesResponseType(typeof(InstitutionInvitationCreated), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> CreateUserInvitation(
        Guid institutionId,
        [FromBody] CreateInstitutionInvitationRequest request,
        CancellationToken cancellationToken) =>
        SendAcademyAsync(
            CreateJsonRequest(
                HttpMethod.Post,
                $"academy/institutions/{institutionId}/users/invitations",
                request),
            cancellationToken);

    [HttpGet("{institutionId:guid}/users")]
    [ProducesResponseType(typeof(IReadOnlyList<Bff.Api.Models.InstitutionUserSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ListInstitutionUsers(
        Guid institutionId,
        CancellationToken cancellationToken) =>
        SendAcademyAsync(
            new HttpRequestMessage(HttpMethod.Get, $"academy/institutions/{institutionId}/users"),
            cancellationToken);

    [HttpPatch("{institutionId:guid}/users/{userId:guid}/role")]
    [ProducesResponseType(typeof(Bff.Api.Models.InstitutionUserSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateInstitutionUserRole(
        Guid institutionId,
        Guid userId,
        [FromBody] Bff.Api.Models.UpdateInstitutionUserRoleRequest request,
        CancellationToken cancellationToken) =>
        SendAcademyAsync(
            CreateJsonRequest(
                HttpMethod.Patch,
                $"academy/institutions/{institutionId}/users/{userId}/role",
                request),
            cancellationToken);

    [HttpDelete("{institutionId:guid}/users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(SovereignID.Bff.Clients.Academy.Models.ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> RevokeInstitutionUser(
        Guid institutionId,
        Guid userId,
        CancellationToken cancellationToken) =>
        SendAcademyAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"academy/institutions/{institutionId}/users/{userId}"),
            cancellationToken);

    private static HttpRequestMessage CreateJsonRequest<T>(
        HttpMethod method,
        string path,
        T body) =>
        new(method, path)
        {
            Content = JsonContent.Create(body)
        };

    private async Task<IActionResult> SendAcademyAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(DependencyInjection.AcademyDirectHttpClientName);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return NoContent();
        }

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
