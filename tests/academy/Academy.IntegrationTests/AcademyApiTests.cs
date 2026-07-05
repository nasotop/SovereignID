using System.Net;
using System.Net.Http.Headers;
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
    public async Task CreateInstitution_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/academy/institutions", new
        {
            code = $"INST-{Guid.NewGuid():N}"[..12],
            legalName = "Institucion Demo SpA",
            displayName = "Institucion Demo",
            contactEmail = "admin@demo.test",
            countryCode = "CL"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InstitutionFlow_CreatesInvitation_AcceptsWallet_CareerAndStudent()
    {
        var createInstitutionResponse = await SendPlatformAdminPostAsync("/academy/institutions", new
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
        using var acceptedJson = await JsonDocument.ParseAsync(await acceptResponse.Content.ReadAsStreamAsync());
        var acceptedUserId = acceptedJson.RootElement.GetProperty("userId").GetGuid();

        var listInstitutionsResponse = await SendAuthorizedGetAsync(
            "/academy/institutions",
            JwtTestHelper.CreatePlatformAdminToken());

        Assert.Equal(HttpStatusCode.OK, listInstitutionsResponse.StatusCode);

        var adminToken = JwtTestHelper.CreateInstitutionAdminToken(institutionId);
        var listUsersResponse = await SendAuthorizedGetAsync(
            $"/academy/institutions/{institutionId}/users",
            adminToken);

        Assert.Equal(HttpStatusCode.OK, listUsersResponse.StatusCode);
        using var usersJson = await JsonDocument.ParseAsync(await listUsersResponse.Content.ReadAsStreamAsync());
        Assert.Contains(
            usersJson.RootElement.EnumerateArray(),
            user => user.GetProperty("userId").GetGuid() == acceptedUserId
                && user.GetProperty("role").GetString() == "admin");

        var createCareerResponse = await SendAuthorizedPostAsync(
            $"/academy/institutions/{institutionId}/careers",
            adminToken,
            new
            {
                code = "ING-SW",
                name = "Ingenieria de Software"
            });

        Assert.Equal(HttpStatusCode.Created, createCareerResponse.StatusCode);
        using var careerJson = await JsonDocument.ParseAsync(await createCareerResponse.Content.ReadAsStreamAsync());
        var careerId = careerJson.RootElement.GetProperty("id").GetGuid();

        var createStudentResponse = await SendAuthorizedPostAsync(
            $"/academy/institutions/{institutionId}/students",
            adminToken,
            new
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

        var listStudentsResponse = await SendAuthorizedGetAsync(
            $"/academy/institutions/{institutionId}/students",
            adminToken);

        Assert.Equal(HttpStatusCode.OK, listStudentsResponse.StatusCode);

        var createStudentWithoutWalletResponse = await SendAuthorizedPostAsync(
            $"/academy/institutions/{institutionId}/students",
            adminToken,
            new
            {
                externalReference = $"ALU-{Guid.NewGuid():N}"[..12],
                enrollmentYear = 2026
            });

        Assert.Equal(HttpStatusCode.Created, createStudentWithoutWalletResponse.StatusCode);
        using var studentWithoutWalletJson = await JsonDocument.ParseAsync(await createStudentWithoutWalletResponse.Content.ReadAsStreamAsync());
        var studentWithoutWalletId = studentWithoutWalletJson.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(JsonValueKind.Null, studentWithoutWalletJson.RootElement.GetProperty("primaryWalletAddress").ValueKind);

        var addWalletResponse = await SendAuthorizedPostAsync(
            $"/academy/institutions/{institutionId}/students/{studentWithoutWalletId}/wallets",
            adminToken,
            new
            {
                walletAddress = "0x3333333333333333333333333333333333333333",
                makePrimary = true
            });

        Assert.Equal(HttpStatusCode.Created, addWalletResponse.StatusCode);
        using var walletJson = await JsonDocument.ParseAsync(await addWalletResponse.Content.ReadAsStreamAsync());
        Assert.Equal("0x3333333333333333333333333333333333333333", walletJson.RootElement.GetProperty("walletAddress").GetString());
        Assert.True(walletJson.RootElement.GetProperty("isPrimary").GetBoolean());

        var updateRoleResponse = await SendAuthorizedPatchAsync(
            $"/academy/institutions/{institutionId}/users/{acceptedUserId}/role",
            adminToken,
            new { role = "viewer" });

        Assert.Equal(HttpStatusCode.OK, updateRoleResponse.StatusCode);
        using var updatedRoleJson = await JsonDocument.ParseAsync(await updateRoleResponse.Content.ReadAsStreamAsync());
        Assert.Equal("viewer", updatedRoleJson.RootElement.GetProperty("role").GetString());

        var revokeUserResponse = await SendAuthorizedDeleteAsync(
            $"/academy/institutions/{institutionId}/users/{acceptedUserId}",
            adminToken);

        Assert.Equal(HttpStatusCode.NoContent, revokeUserResponse.StatusCode);
    }

    [Fact]
    public async Task CreateInstitution_WithIssuerMembershipOnly_ReturnsForbidden()
    {
        var institutionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var issuerToken = JwtTestHelper.CreateToken(
            "0xcccccccccccccccccccccccccccccccccccccccc",
            memberships: [new SovereignID.Authorization.InstitutionMembership(institutionId, "issuer")]);

        var response = await SendAuthorizedPostAsync("/academy/institutions", issuerToken, new
        {
            code = $"INST-{Guid.NewGuid():N}"[..12],
            legalName = "Institucion Demo SpA",
            displayName = "Institucion Demo",
            contactEmail = "admin@demo.test",
            countryCode = "CL"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateStudent_WithInvalidWallet_ReturnsProblemDetails()
    {
        var createInstitutionResponse = await SendPlatformAdminPostAsync("/academy/institutions", new
        {
            code = $"BAD-{Guid.NewGuid():N}"[..12],
            legalName = "Institucion Error SpA",
            displayName = "Institucion Error",
            contactEmail = "admin-error@demo.test"
        });
        createInstitutionResponse.EnsureSuccessStatusCode();
        using var institutionJson = await JsonDocument.ParseAsync(await createInstitutionResponse.Content.ReadAsStreamAsync());
        var institutionId = institutionJson.RootElement.GetProperty("institution").GetProperty("id").GetGuid();
        var adminToken = JwtTestHelper.CreateInstitutionAdminToken(institutionId);

        var response = await SendAuthorizedPostAsync(
            $"/academy/institutions/{institutionId}/students",
            adminToken,
            new
            {
                externalReference = "ALU-INVALID",
                walletAddress = "not-a-wallet"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problemJson = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("invalid_wallet_address", problemJson.RootElement.GetProperty("error").GetString());
    }

    private Task<HttpResponseMessage> SendPlatformAdminPostAsync(string url, object body) =>
        SendAuthorizedPostAsync(url, JwtTestHelper.CreatePlatformAdminToken(), body);

    private Task<HttpResponseMessage> SendAuthorizedGetAsync(string url, string bearerToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return _client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> SendAuthorizedPostAsync(
        HttpClient client,
        string url,
        string bearerToken,
        object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return client.SendAsync(request);
    }

    private Task<HttpResponseMessage> SendAuthorizedPostAsync(string url, string bearerToken, object body) =>
        SendAuthorizedPostAsync(_client, url, bearerToken, body);

    private Task<HttpResponseMessage> SendAuthorizedPatchAsync(string url, string bearerToken, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> SendAuthorizedDeleteAsync(string url, string bearerToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return _client.SendAsync(request);
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

