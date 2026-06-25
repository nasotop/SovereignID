using System.Net;

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
}
