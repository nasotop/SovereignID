using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Issuer.IntegrationTests;

public sealed class IssuerApiTests : IClassFixture<IssuerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IssuerApiTests(IssuerWebApplicationFactory factory)
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
    public async Task LinkInstitutionWallet_WithKnownInstitution_ReturnsLinkedWallet()
    {
        var request = new
        {
            walletAddress = "0x1111111111111111111111111111111111111111",
            did = "did:ethr:sepolia:0x1111111111111111111111111111111111111111",
            publicKey = "0xissuer-public-key"
        };

        var response = await _client.PostAsJsonAsync(
            "/issuer/institutions/11111111-1111-1111-1111-111111111111/wallet",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("11111111-1111-1111-1111-111111111111", body.GetProperty("institutionId").GetString());
        Assert.Equal(request.walletAddress, body.GetProperty("walletAddress").GetString());
        Assert.Equal(request.did, body.GetProperty("did").GetString());
    }

    [Fact]
    public async Task LinkTitle_WithKnownStudent_ReturnsCreatedCredential()
    {
        var request = new
        {
            careerId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            credentialTypeCode = "titulo",
            ipfsCid = "bafybeigdyrzt",
            ipfsGatewayUrl = "https://ipfs.io/ipfs/bafybeigdyrzt",
            contentHash = "0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            transactionHash = "0xbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            blockNumber = 123456L,
            eip712Signature = "0xcccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"
        };

        var response = await _client.PostAsJsonAsync(
            "/issuer/students/22222222-2222-2222-2222-222222222222/title",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("22222222-2222-2222-2222-222222222222", body.GetProperty("studentId").GetString());
        Assert.Equal("11111111-1111-1111-1111-111111111111", body.GetProperty("institutionId").GetString());
        Assert.Equal("33333333-3333-3333-3333-333333333333", body.GetProperty("careerId").GetString());
        Assert.Equal("active", body.GetProperty("status").GetString());
        Assert.Equal("did:ethr:sepolia:0x2222222222222222222222222222222222222222", body.GetProperty("subjectDid").GetString());
    }

    [Fact]
    public async Task LinkTitle_WithIncompletePayload_ReturnsProblemDetails()
    {
        var request = new
        {
            credentialTypeCode = "",
            ipfsCid = "",
            ipfsGatewayUrl = "",
            contentHash = "",
            transactionHash = "",
            blockNumber = 0L,
            eip712Signature = ""
        };

        var response = await _client.PostAsJsonAsync(
            "/issuer/students/22222222-2222-2222-2222-222222222222/title",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_credential_type", body.GetProperty("error").GetString());
    }
}
