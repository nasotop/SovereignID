using Microsoft.AspNetCore.Mvc;
using SovereignID.Bff.Clients;

namespace Bff.Api.Controllers;

[ApiController]
[Route("reports")]
[Produces("application/json")]
public sealed class ReportsController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet("institutions/{institutionId:guid}/credentials-issued")]
    public Task<IActionResult> GetCredentialsIssued(
        Guid institutionId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        ProxyAsync(
            $"reports/institutions/{institutionId}/credentials-issued?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

    [HttpGet("institutions/{institutionId:guid}/credential-reads")]
    public Task<IActionResult> GetCredentialReads(
        Guid institutionId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        ProxyAsync(
            $"reports/institutions/{institutionId}/credential-reads?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

    [HttpGet("institutions/{institutionId:guid}/credentials-per-student")]
    public Task<IActionResult> GetCredentialsPerStudent(
        Guid institutionId,
        [FromQuery] DateOnly asOf,
        CancellationToken cancellationToken) =>
        ProxyAsync(
            $"reports/institutions/{institutionId}/credentials-per-student?asOf={asOf:yyyy-MM-dd}",
            cancellationToken);

    [HttpGet("institutions/{institutionId:guid}/credentials-revoked")]
    public Task<IActionResult> GetCredentialsRevoked(
        Guid institutionId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        ProxyAsync(
            $"reports/institutions/{institutionId}/credentials-revoked?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

    [HttpGet("institutions/{institutionId:guid}/verification-outcomes")]
    public Task<IActionResult> GetVerificationOutcomes(
        Guid institutionId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        ProxyAsync(
            $"reports/institutions/{institutionId}/verification-outcomes?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

    [HttpGet("platform/credentials-by-institution")]
    public Task<IActionResult> GetPlatformCredentialsByInstitution(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        ProxyAsync(
            $"reports/platform/credentials-by-institution?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            cancellationToken);

    [HttpGet("platform/students-by-institution")]
    public Task<IActionResult> GetPlatformStudentsByInstitution(
        [FromQuery] DateOnly asOf,
        CancellationToken cancellationToken) =>
        ProxyAsync(
            $"reports/platform/students-by-institution?asOf={asOf:yyyy-MM-dd}",
            cancellationToken);

    private async Task<IActionResult> ProxyAsync(string path, CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(DependencyInjection.ReportsDirectHttpClientName);
        using var response = await httpClient.GetAsync(path, cancellationToken);
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
