using Reports.Api.Models;
using Reports.Application;
using Reports.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SovereignID.Authorization;

namespace Reports.Api.Controllers;

[ApiController]
[Route("reports/institutions/{institutionId:guid}")]
[Authorize(Policy = AuthorizationPolicies.PlatformOrInstitutionMember)]
[Produces("application/json")]
public sealed class InstitutionReportsController(IReportCatalog catalog) : ControllerBase
{
    /// <summary>Credenciales emitidas en el período (R-I1).</summary>
    [HttpGet("credentials-issued")]
    [ProducesResponseType(typeof(TimeSeriesReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCredentialsIssued(
        Guid institutionId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var report = await catalog.GetInstitutionTimeSeriesAsync(
            InstitutionReportId.CredentialsIssued,
            institutionId,
            from,
            to,
            cancellationToken);
        return Ok(ToTimeSeriesResponse(report));
    }

    /// <summary>Credenciales leídas (verificaciones) en el período (R-I2).</summary>
    [HttpGet("credential-reads")]
    [ProducesResponseType(typeof(TimeSeriesReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCredentialReads(
        Guid institutionId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var report = await catalog.GetInstitutionTimeSeriesAsync(
            InstitutionReportId.CredentialReads,
            institutionId,
            from,
            to,
            cancellationToken);
        return Ok(ToTimeSeriesResponse(report));
    }

    /// <summary>Promedio de credenciales por alumno activo registrado (R-I3).</summary>
    [HttpGet("credentials-per-student")]
    [ProducesResponseType(typeof(CredentialsPerStudentReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCredentialsPerStudent(
        Guid institutionId,
        [FromQuery] DateOnly asOf,
        CancellationToken cancellationToken)
    {
        var report = await catalog.GetInstitutionCredentialsPerStudentAsync(institutionId, asOf, cancellationToken);
        return Ok(new CredentialsPerStudentReportResponse(
            report.AsOf.ToString("yyyy-MM-dd"),
            report.TotalStudents,
            report.TotalCredentials,
            report.AverageCredentialsPerStudent));
    }

    /// <summary>Credenciales revocadas en el período (R-I4).</summary>
    [HttpGet("credentials-revoked")]
    [ProducesResponseType(typeof(TimeSeriesReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCredentialsRevoked(
        Guid institutionId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var report = await catalog.GetInstitutionTimeSeriesAsync(
            InstitutionReportId.CredentialsRevoked,
            institutionId,
            from,
            to,
            cancellationToken);
        return Ok(ToTimeSeriesResponse(report));
    }

    /// <summary>Verificaciones válidas vs inválidas en el período (R-I5).</summary>
    [HttpGet("verification-outcomes")]
    [ProducesResponseType(typeof(VerificationOutcomesReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVerificationOutcomes(
        Guid institutionId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var report = await catalog.GetInstitutionVerificationOutcomesAsync(
            institutionId,
            from,
            to,
            cancellationToken);

        return Ok(new VerificationOutcomesReportResponse(
            new ReportPeriodResponse(report.Period.From.ToString("yyyy-MM-dd"), report.Period.To.ToString("yyyy-MM-dd")),
            report.Source.ToString().ToLowerInvariant(),
            report.ValidTotal,
            report.InvalidTotal,
            report.Series.Select(s => new VerificationOutcomePointResponse(
                s.Date.ToString("yyyy-MM-dd"),
                s.Valid,
                s.Invalid)).ToList()));
    }

    private static TimeSeriesReportResponse ToTimeSeriesResponse(TimeSeriesReport report) =>
        new(
            new ReportPeriodResponse(
                report.Period.From.ToString("yyyy-MM-dd"),
                report.Period.To.ToString("yyyy-MM-dd")),
            report.Source.ToString().ToLowerInvariant(),
            report.Total,
            report.Series.Select(s => new TimeSeriesPointResponse(
                s.Date.ToString("yyyy-MM-dd"),
                s.Value)).ToList());
}
