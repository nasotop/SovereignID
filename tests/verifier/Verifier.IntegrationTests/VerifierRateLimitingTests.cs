using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Verifier.IntegrationTests;

public sealed class VerifierRateLimitingTests : IClassFixture<RateLimitedVerifierFactory>
{
    private readonly HttpClient _client;

    public VerifierRateLimitingTests(RateLimitedVerifierFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task BurstBeyondCapacity_Returns429_WithRateLimitErrorCode()
    {
        var credentialId = Guid.NewGuid();
        var responses = new List<HttpResponseMessage>();

        for (var i = 0; i < 6; i++)
        {
            responses.Add(await PostVerificationAsync(credentialId));
        }

        var rateLimited = responses.FirstOrDefault(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        Assert.NotNull(rateLimited);

        var problem = await rateLimited.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        Assert.Equal("rate_limit_exceeded", problem?.Error);
    }

    private Task<HttpResponseMessage> PostVerificationAsync(Guid credentialId) =>
        _client.PostAsJsonAsync("/verifications", new { credentialId = credentialId.ToString() });
}

public sealed class RateLimitedVerifierFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=sovereignid;Username=sovereignid;Password=sovereignid_dev",
                ["Verifier:RateLimiting:TokenLimit"] = "2",
                ["Verifier:RateLimiting:TokensPerPeriod"] = "1",
                ["Verifier:RateLimiting:PeriodSeconds"] = "60"
            });
        });
    }
}

internal sealed class ProblemDetailsResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
