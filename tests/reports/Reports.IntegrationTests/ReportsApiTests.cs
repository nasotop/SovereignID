using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Reports.Infrastructure.Metrics;
using Reports.Infrastructure.Persistence.Stores;

namespace Reports.IntegrationTests;

public sealed class ReportsApiTests : IClassFixture<ReportsWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ReportsWebApplicationFactory _factory;
    private static readonly Guid DemoInstitutionId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public ReportsApiTests(ReportsWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CredentialsIssued_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            $"/reports/institutions/{DemoInstitutionId}/credentials-issued?from=2026-01-01&to=2026-01-31");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CredentialsIssued_WithoutMembership_ReturnsForbidden()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/reports/institutions/{DemoInstitutionId}/credentials-issued?from=2026-01-01&to=2026-01-31");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.CreateToken("0xeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee"));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CredentialsIssued_Viewer_ReturnsOk()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/reports/institutions/{DemoInstitutionId}/credentials-issued?from=2026-01-01&to=2026-01-31");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.CreateInstitutionViewerToken(DemoInstitutionId));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PlatformRanking_Issuer_ReturnsForbidden()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/reports/platform/credentials-by-institution?from=2026-01-01&to=2026-01-31");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.CreateInstitutionIssuerToken(DemoInstitutionId));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InvalidPeriod_ReturnsBadRequestWithStableCode()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/reports/institutions/{DemoInstitutionId}/credentials-issued?from=2024-01-01&to=2026-01-01");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.CreateInstitutionAdminToken(DemoInstitutionId));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_report_period", problem.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CredentialsIssued_WithoutSnapshot_ReturnsLiveSource()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-7);
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/reports/institutions/{DemoInstitutionId}/credentials-issued?from={from:yyyy-MM-dd}&to={today:yyyy-MM-dd}");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.CreateInstitutionAdminToken(DemoInstitutionId));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("live", body.GetProperty("source").GetString());
    }

    [Fact]
    public async Task CredentialsPerStudent_ReturnsAverage()
    {
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/reports/institutions/{DemoInstitutionId}/credentials-per-student?asOf={asOf:yyyy-MM-dd}");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.CreateInstitutionAdminToken(DemoInstitutionId));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("totalStudents").GetInt32());
        Assert.Equal(1, body.GetProperty("totalCredentials").GetInt32());
        Assert.Equal(1, body.GetProperty("averageCredentialsPerStudent").GetDouble());
    }

    [Fact]
    public async Task SnapshotBackfill_IsIdempotent()
    {
        using var scope = _factory.Services.CreateScope();
        var snapshotService = scope.ServiceProvider.GetRequiredService<IMetricsSnapshotService>();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);

        await snapshotService.UpsertDailyMetricsAsync(date);
        await snapshotService.UpsertDailyMetricsAsync(date);

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/reports/institutions/{DemoInstitutionId}/credentials-issued?from={date:yyyy-MM-dd}&to={date:yyyy-MM-dd}");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.CreateInstitutionAdminToken(DemoInstitutionId));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("source").GetString() is "snapshot" or "hybrid");
    }

    [Fact]
    public async Task PlatformAdmin_CanReadPlatformRanking()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/reports/platform/students-by-institution?asOf=2026-03-31");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.CreatePlatformAdminToken());

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
