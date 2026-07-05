using Reports.Api.Models;
using Reports.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SovereignID.Authorization;

namespace Reports.Api.Controllers;

[ApiController]
[Route("reports/platform")]
[Authorize(Policy = AuthorizationPolicies.PlatformAdmin)]
[Produces("application/json")]
public sealed class PlatformReportsController(IReportCatalog catalog) : ControllerBase
{
    /// <summary>Credenciales emitidas por institución en el período (R-P1).</summary>
    [HttpGet("credentials-by-institution")]
    [ProducesResponseType(typeof(PlatformCredentialsByInstitutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCredentialsByInstitution(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var report = await catalog.GetPlatformCredentialsByInstitutionAsync(from, to, cancellationToken);
        return Ok(new PlatformCredentialsByInstitutionResponse(
            new ReportPeriodResponse(
                report.Period.From.ToString("yyyy-MM-dd"),
                report.Period.To.ToString("yyyy-MM-dd")),
            report.Source.ToString().ToLowerInvariant(),
            report.Items.Select(i => new PlatformRankingItemResponse(i.InstitutionId, i.DisplayName, i.Total)).ToList()));
    }

    /// <summary>Alumnos activos por institución a una fecha (R-P2).</summary>
    [HttpGet("students-by-institution")]
    [ProducesResponseType(typeof(PlatformStudentsByInstitutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudentsByInstitution(
        [FromQuery] DateOnly asOf,
        CancellationToken cancellationToken)
    {
        var report = await catalog.GetPlatformStudentsByInstitutionAsync(asOf, cancellationToken);
        return Ok(new PlatformStudentsByInstitutionResponse(
            asOf.ToString("yyyy-MM-dd"),
            report.Items.Select(i => new PlatformStudentsItemResponse(
                i.InstitutionId,
                i.DisplayName,
                i.TotalStudents)).ToList()));
    }
}
