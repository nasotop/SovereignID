using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Academy.IntegrationTests;

public sealed class AcademyApiTests : IClassFixture<AcademyWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AcademyApiTests(AcademyWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_IsAvailable_InDevelopment()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InstitutionFlow_CreatesInvitation_AcceptsWallet_CareerAndStudent()
    {
        var createInstitutionResponse = await _client.PostAsJsonAsync("/academy/institutions", new
        {
            code = $"INST-{Guid.NewGuid():N}"[..12],
            legalName = "Institucion Demo SpA",
            displayName = "Institucion Demo",
            contactEmail = "admin@demo.test",
            countryCode = "CL"
        });

        Assert.Equal(HttpStatusCode.Created, createInstitutionResponse.StatusCode);
        using var institutionJson = await JsonDocument.ParseAsync(await createInstitutionResponse.Content.ReadAsStreamAsync());
        var institutionId = institutionJson.RootElement
            .GetProperty("institution")
            .GetProperty("id")
            .GetGuid();
        var invitationUrl = institutionJson.RootElement
            .GetProperty("invitation")
            .GetProperty("invitationUrl")
            .GetString();
        var token = ExtractToken(invitationUrl);

        var acceptResponse = await _client.PostAsJsonAsync("/academy/invitations/accept", new
        {
            token,
            walletAddress = "0x1111111111111111111111111111111111111111",
            displayName = "Admin Demo"
        });

        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        var createCareerResponse = await _client.PostAsJsonAsync($"/academy/institutions/{institutionId}/careers", new
        {
            code = "ING-SW",
            name = "Ingenieria de Software"
        });

        Assert.Equal(HttpStatusCode.Created, createCareerResponse.StatusCode);
        using var careerJson = await JsonDocument.ParseAsync(await createCareerResponse.Content.ReadAsStreamAsync());
        var careerId = careerJson.RootElement.GetProperty("id").GetGuid();

        var createStudentResponse = await _client.PostAsJsonAsync($"/academy/institutions/{institutionId}/students", new
        {
            externalReference = $"ALU-{Guid.NewGuid():N}"[..12],
            enrollmentYear = 2026,
            walletAddress = "0x2222222222222222222222222222222222222222"
        });

        Assert.Equal(HttpStatusCode.Created, createStudentResponse.StatusCode);
        using var studentJson = await JsonDocument.ParseAsync(await createStudentResponse.Content.ReadAsStreamAsync());
        var studentId = studentJson.RootElement.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, careerId);
        Assert.NotEqual(Guid.Empty, studentId);
    }

    [Fact]
    public async Task CreateStudent_WithInvalidWallet_ReturnsProblemDetails()
    {
        var createInstitutionResponse = await _client.PostAsJsonAsync("/academy/institutions", new
        {
            code = $"BAD-{Guid.NewGuid():N}"[..12],
            legalName = "Institucion Error SpA",
            displayName = "Institucion Error",
            contactEmail = "admin-error@demo.test"
        });
        createInstitutionResponse.EnsureSuccessStatusCode();
        using var institutionJson = await JsonDocument.ParseAsync(await createInstitutionResponse.Content.ReadAsStreamAsync());
        var institutionId = institutionJson.RootElement.GetProperty("institution").GetProperty("id").GetGuid();

        var response = await _client.PostAsJsonAsync($"/academy/institutions/{institutionId}/students", new
        {
            externalReference = "ALU-INVALID",
            walletAddress = "not-a-wallet"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problemJson = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("invalid_wallet_address", problemJson.RootElement.GetProperty("error").GetString());
    }

    private static string ExtractToken(string? invitationUrl)
    {
        Assert.False(string.IsNullOrWhiteSpace(invitationUrl));
        var uri = new Uri(invitationUrl);
        var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        var tokenPair = query.Single(part => part.StartsWith("token=", StringComparison.Ordinal));
        return Uri.UnescapeDataString(tokenPair["token=".Length..]);
    }
}

