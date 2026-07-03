using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Issuer.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Issuer.IntegrationTests;

public sealed class IssuerAuthorizationTests : IClassFixture<IssuerAuthenticatedWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly Guid DemoInstitutionId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public IssuerAuthorizationTests(IssuerAuthenticatedWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LinkTitle_WithoutMembership_ReturnsForbidden()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/issuer/students/22222222-2222-2222-2222-222222222222/title")
        {
            Content = JsonContent.Create(new
            {
                careerId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                credentialTypeCode = "TITULO",
                ipfsCid = "bafybeigdyrzt",
                ipfsGatewayUrl = "https://ipfs.io/ipfs/bafybeigdyrzt",
                contentHash = "0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                transactionHash = "0xbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                blockNumber = 123456L,
                eip712Signature = "0xcccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.CreateHolderToken());

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RevokeCredential_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/issuer/credentials/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/revoke",
            new
            {
                reason = "Test revocation",
                revocationTxHash = "0xabababababababababababababababababababababababababababababababab",
                blockNumber = 123458L,
                eip712Signature = "0xbababababababababababababababababababababababababababababababababababababababababababababababababababababababababababababababababababa"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RevokeCredential_WithIssuerMembership_ReturnsOk()
    {
        var issueRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/issuer/students/22222222-2222-2222-2222-222222222222/title")
        {
            Content = JsonContent.Create(new
            {
                careerId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                credentialTypeCode = "TITULO",
                ipfsCid = "bafybeigdyrzt3",
                ipfsGatewayUrl = "https://ipfs.io/ipfs/bafybeigdyrzt3",
                contentHash = "0xdddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd",
                transactionHash = "0xeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee",
                blockNumber = 123457L,
                eip712Signature = "0xffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"
            })
        };
        issueRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.CreateIssuerToken(DemoInstitutionId));

        var issueResponse = await _client.SendAsync(issueRequest);
        issueResponse.EnsureSuccessStatusCode();

        var issued = await issueResponse.Content.ReadFromJsonAsync<JsonElement>();
        var credentialId = issued.GetProperty("credentialId").GetString();

        var revokeRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"/issuer/credentials/{credentialId}/revoke")
        {
            Content = JsonContent.Create(new
            {
                reason = "Authorized revocation",
                revocationTxHash = "0xabababababababababababababababababababababababababababababababab",
                blockNumber = 123458L,
                eip712Signature = "0xbababababababababababababababababababababababababababababababababababababababababababababababababababababababababababababababababababa"
            })
        };
        revokeRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.CreateIssuerToken(DemoInstitutionId));

        var response = await _client.SendAsync(revokeRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public sealed class IssuerAuthenticatedWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = "InMemory",
                ["Auth:JwtIssuer"] = JwtTestHelper.TestIssuer,
                ["Auth:JwtAudience"] = JwtTestHelper.TestAudience,
                ["Auth:JwtSigningKey"] = JwtTestHelper.TestSigningKey,
                [$"{IssuerOptions.SectionName}:Auth:RequireAuthentication"] = "true"
            });
        });
    }
}
