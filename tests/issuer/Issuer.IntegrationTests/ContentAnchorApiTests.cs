using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Issuer.Application;
using Issuer.Application.ContentAnchor;
using Issuer.Infrastructure.ContentAnchor;
using Issuer.Infrastructure.Persistence.Composition;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace Issuer.IntegrationTests;

public sealed class ContentAnchorApiTests : IClassFixture<ContentAnchorWebApplicationFactory>
{
    private static readonly Guid DemoInstitutionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly ContentAnchorWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ContentAnchorApiTests(ContentAnchorWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AnchorDocument_WithInMemoryAdapter_ReturnsContentHashAndCid()
    {
        var document = JsonSerializer.SerializeToElement(new
        {
            @context = new[] { "https://www.w3.org/ns/credentials/v2" },
            type = new[] { "VerifiableCredential", "UniversityDegreeCredential" },
            issuer = "did:ethr:sepolia:0x1111111111111111111111111111111111111111",
            credentialSubject = new { id = "did:ethr:sepolia:0x2222", degree = "Licenciatura" }
        });

        var response = await _client.PostAsJsonAsync(
            $"/issuer/institutions/{DemoInstitutionId}/documents/anchor",
            new { document });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var contentHash = body.GetProperty("contentHash").GetString();
        var ipfsCid = body.GetProperty("ipfsCid").GetString();
        var gatewayUrl = body.GetProperty("ipfsGatewayUrl").GetString();

        Assert.False(string.IsNullOrWhiteSpace(contentHash));
        Assert.StartsWith("0x", contentHash);
        Assert.False(string.IsNullOrWhiteSpace(ipfsCid));
        Assert.StartsWith("bafybei", ipfsCid);
        Assert.Equal($"https://ipfs.io/ipfs/{ipfsCid}", gatewayUrl);
        Assert.True(_factory.PinningAdapter.TryGet(ipfsCid!, out var stored));
        Assert.Equal(contentHash, ContentHashComputer.ComputeSha256Hex(stored!));
    }

    [Fact]
    public async Task AnchorDocument_WhenDisabled_ReturnsIpfsNotConfigured()
    {
        await using var factory = new ContentAnchorDisabledWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/issuer/institutions/{DemoInstitutionId}/documents/anchor",
            new { document = new { type = "VerifiableCredential" } });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ipfs_not_configured", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task AnchorDocument_WithInvalidDocument_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            $"/issuer/institutions/{DemoInstitutionId}/documents/anchor",
            new { document = "not-an-object" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_anchor_document", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task LinkTitle_WithVerifyEnabled_AndMatchingGateway_ReturnsCreated()
    {
        var document = JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["id"] = "urn:credential:verify-ok",
            ["type"] = new[] { "VerifiableCredential" }
        });

        var anchorResponse = await _client.PostAsJsonAsync(
            $"/issuer/institutions/{DemoInstitutionId}/documents/anchor",
            new { document });
        Assert.Equal(HttpStatusCode.OK, anchorResponse.StatusCode);
        var anchor = await anchorResponse.Content.ReadFromJsonAsync<JsonElement>();

        var linkRequest = new
        {
            careerId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            credentialTypeCode = "titulo",
            ipfsCid = anchor.GetProperty("ipfsCid").GetString(),
            ipfsGatewayUrl = anchor.GetProperty("ipfsGatewayUrl").GetString(),
            contentHash = anchor.GetProperty("contentHash").GetString(),
            transactionHash = "0xbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            blockNumber = 123456L,
            eip712Signature = "0xcccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"
        };

        var response = await _client.PostAsJsonAsync(
            "/issuer/students/22222222-2222-2222-2222-222222222222/title",
            linkRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task LinkTitle_WithVerifyEnabled_AndHashMismatch_ReturnsConflict()
    {
        var document = JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["id"] = "urn:credential:verify-mismatch",
            ["type"] = new[] { "VerifiableCredential" }
        });

        var anchorResponse = await _client.PostAsJsonAsync(
            $"/issuer/institutions/{DemoInstitutionId}/documents/anchor",
            new { document });
        var anchor = await anchorResponse.Content.ReadFromJsonAsync<JsonElement>();

        var linkRequest = new
        {
            careerId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            credentialTypeCode = "titulo",
            ipfsCid = anchor.GetProperty("ipfsCid").GetString(),
            ipfsGatewayUrl = anchor.GetProperty("ipfsGatewayUrl").GetString(),
            contentHash = "0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            transactionHash = "0xbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            blockNumber = 123456L,
            eip712Signature = "0xcccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"
        };

        var response = await _client.PostAsJsonAsync(
            "/issuer/students/22222222-2222-2222-2222-222222222222/title",
            linkRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("content_anchor_invalid", body.GetProperty("error").GetString());
    }
}

public sealed class ContentAnchorAuthorizationTests : IClassFixture<ContentAnchorAuthenticatedWebApplicationFactory>
{
    private static readonly Guid DemoInstitutionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherInstitutionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly HttpClient _client;

    public ContentAnchorAuthorizationTests(ContentAnchorAuthenticatedWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AnchorDocument_WithoutMembership_ReturnsForbidden()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/issuer/institutions/{DemoInstitutionId}/documents/anchor")
        {
            Content = JsonContent.Create(new { document = new { type = "VerifiableCredential" } })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTestHelper.CreateIssuerToken(OtherInstitutionId));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public sealed class ContentAnchorWebApplicationFactory : WebApplicationFactory<Program>
{
    public InMemoryContentPinningAdapter PinningAdapter { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{PersistenceOptions.SectionName}:Provider"] = PersistenceProviders.InMemory,
                ["Auth:JwtIssuer"] = JwtTestHelper.TestIssuer,
                ["Auth:JwtAudience"] = JwtTestHelper.TestAudience,
                ["Auth:JwtSigningKey"] = JwtTestHelper.TestSigningKey,
                ["Issuer:ContentAnchor:Enabled"] = "true",
                ["Issuer:ContentAnchor:VerifyEnabled"] = "true",
                ["Issuer:ContentAnchor:GatewayBase"] = "https://ipfs.io/ipfs",
                ["Issuer:ContentAnchor:IpfsTimeoutSeconds"] = "5"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IContentPinningAdapter>();
            services.AddSingleton(PinningAdapter);
            services.AddSingleton<IContentPinningAdapter>(PinningAdapter);

            services.ConfigureAll<HttpClientFactoryOptions>(options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(b =>
                {
                    b.PrimaryHandler = new InMemoryGatewayHandler(PinningAdapter);
                });
            });
        });
    }
}

public sealed class ContentAnchorDisabledWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{PersistenceOptions.SectionName}:Provider"] = PersistenceProviders.InMemory,
                ["Auth:JwtIssuer"] = JwtTestHelper.TestIssuer,
                ["Auth:JwtAudience"] = JwtTestHelper.TestAudience,
                ["Auth:JwtSigningKey"] = JwtTestHelper.TestSigningKey,
                ["Issuer:ContentAnchor:Enabled"] = "false",
                ["Issuer:ContentAnchor:VerifyEnabled"] = "false"
            });
        });
    }
}

public sealed class ContentAnchorAuthenticatedWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{PersistenceOptions.SectionName}:Provider"] = PersistenceProviders.InMemory,
                ["Auth:JwtIssuer"] = JwtTestHelper.TestIssuer,
                ["Auth:JwtAudience"] = JwtTestHelper.TestAudience,
                ["Auth:JwtSigningKey"] = JwtTestHelper.TestSigningKey,
                [$"{IssuerOptions.SectionName}:Auth:RequireAuthentication"] = "true",
                ["Issuer:ContentAnchor:Enabled"] = "true",
                ["Issuer:ContentAnchor:VerifyEnabled"] = "false"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var adapter = new InMemoryContentPinningAdapter();
            services.RemoveAll<IContentPinningAdapter>();
            services.AddSingleton<IContentPinningAdapter>(adapter);
        });
    }
}

internal sealed class InMemoryGatewayHandler : HttpMessageHandler
{
    private readonly InMemoryContentPinningAdapter _adapter;

    public InMemoryGatewayHandler(InMemoryContentPinningAdapter adapter)
    {
        _adapter = adapter;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var cid = path.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (cid is not null && _adapter.TryGet(cid, out var bytes) && bytes is not null)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
                }
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
