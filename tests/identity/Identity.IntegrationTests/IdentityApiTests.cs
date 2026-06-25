using System.Net;

namespace Identity.IntegrationTests;

public sealed class IdentityApiTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IdentityApiTests(IdentityWebApplicationFactory factory)
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
