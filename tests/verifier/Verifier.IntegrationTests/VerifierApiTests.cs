using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Verifier.IntegrationTests;

public sealed class VerifierApiTests : IClassFixture<VerifierWebApplicationFactory>
{
    private readonly VerifierWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public VerifierApiTests(VerifierWebApplicationFactory factory)
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
    public async Task OpenApi_IsAvailable_InDevelopment()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ver01_ValidCredential_Returns200Valid()
    {
        var doc = await VerifyAsync(SeedData.ValidCredentialId.ToString());
        var root = doc.RootElement;

        Assert.Equal("valid", root.GetProperty("result").GetString());

        var checks = root.GetProperty("checks");
        Assert.True(checks.GetProperty("found").GetBoolean());
        Assert.True(checks.GetProperty("notRevoked").GetBoolean());
        Assert.True(checks.GetProperty("notExpired").GetBoolean());

        var credential = root.GetProperty("credential");
        Assert.Equal(JsonValueKind.Object, credential.ValueKind);
        Assert.Equal("TITULO", credential.GetProperty("type").GetString());
        Assert.Equal(SeedData.IssuerCode, credential.GetProperty("issuer").GetProperty("code").GetString());
        Assert.Equal(SeedData.IssuerDid, credential.GetProperty("issuer").GetProperty("did").GetString());
        Assert.Equal(11155111, credential.GetProperty("anchors").GetProperty("chainId").GetInt32());
    }

    [Fact]
    public async Task Ver02_RevokedCredential_Returns200Revoked()
    {
        var root = (await VerifyAsync(SeedData.RevokedCredentialId.ToString())).RootElement;

        Assert.Equal("revoked", root.GetProperty("result").GetString());
        Assert.False(root.GetProperty("checks").GetProperty("notRevoked").GetBoolean());
    }

    [Fact]
    public async Task Ver03_ExpiredByDate_Returns200Expired()
    {
        var root = (await VerifyAsync(SeedData.ExpiredCredentialId.ToString())).RootElement;

        Assert.Equal("expired", root.GetProperty("result").GetString());
        Assert.False(root.GetProperty("checks").GetProperty("notExpired").GetBoolean());
    }

    [Fact]
    public async Task Ver04_NotFound_Returns200NotFound_WithNullCredential()
    {
        var root = (await VerifyAsync(SeedData.MissingCredentialId.ToString())).RootElement;

        Assert.Equal("not_found", root.GetProperty("result").GetString());
        Assert.False(root.GetProperty("checks").GetProperty("found").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("credential").ValueKind);
    }

    [Fact]
    public async Task Ver05_RevokedAndExpired_RevokedTakesPrecedence()
    {
        var root = (await VerifyAsync(SeedData.RevokedAndExpiredCredentialId.ToString())).RootElement;

        Assert.Equal("revoked", root.GetProperty("result").GetString());
        Assert.False(root.GetProperty("checks").GetProperty("notRevoked").GetBoolean());
        Assert.False(root.GetProperty("checks").GetProperty("notExpired").GetBoolean());
    }

    [Fact]
    public async Task Ver06_ExternalChecks_AreNull()
    {
        var checks = (await VerifyAsync(SeedData.ValidCredentialId.ToString())).RootElement.GetProperty("checks");

        Assert.Equal(JsonValueKind.Null, checks.GetProperty("hashMatches").ValueKind);
        Assert.Equal(JsonValueKind.Null, checks.GetProperty("onChainExists").ValueKind);
        Assert.Equal(JsonValueKind.Null, checks.GetProperty("signatureValid").ValueKind);
    }

    [Fact]
    public async Task Ver07_MalformedUuid_Returns400WithErrorCode()
    {
        var response = await _client.PostAsJsonAsync("/verifications", new { credentialId = "not-a-uuid" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertInvalidCredentialIdAsync(response);
    }

    [Fact]
    public async Task Ver08_MissingCredentialId_Returns400WithErrorCode()
    {
        var response = await _client.PostAsJsonAsync("/verifications", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertInvalidCredentialIdAsync(response);
    }

    [Fact]
    public async Task VerLog_EveryAttemptIsRecorded_IncludingNotFound()
    {
        var queriedId = Guid.NewGuid().ToString();

        var root = (await VerifyAsync(queriedId)).RootElement;
        Assert.Equal("not_found", root.GetProperty("result").GetString());

        var count = await SeedData.CountVerificationLogsAsync(_factory.ConnectionString, queriedId);
        Assert.Equal(1, count);
    }

    private async Task<JsonDocument> VerifyAsync(string credentialId)
    {
        var response = await _client.PostAsJsonAsync("/verifications", new { credentialId });
        if (response.StatusCode != HttpStatusCode.OK)
        {
            Assert.Fail($"Expected 200 but got {(int)response.StatusCode}. Server error: {_factory.LastError}");
        }

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static async Task AssertInvalidCredentialIdAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_credential_id", doc.RootElement.GetProperty("error").GetString());
    }
}
