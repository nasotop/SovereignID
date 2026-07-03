using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Auth.Api.Models;
using Auth.Infrastructure.Persistence.Composition;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nethereum.Signer;
using SovereignID.Authorization;

namespace Auth.IntegrationTests;

public sealed class AuthRbacEndpointsTests : IClassFixture<AuthRbacWebApplicationFactory>
{
    private readonly AuthRbacWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthRbacEndpointsTests(AuthRbacWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Verify_WithPlatformAdminAllowlist_ReturnsPlatformAdminClaim()
    {
        var address = _factory.PlatformAdminKey.GetPublicAddress();
        var nonceResponse = await _client.GetFromJsonAsync<NonceResponse>("/auth/nonce");
        Assert.NotNull(nonceResponse);

        var issuedAt = _factory.TimeProvider.GetUtcNow();
        var message = SiweTestHelper.BuildMessage(address, nonceResponse.Nonce, issuedAt);
        var signature = SiweTestHelper.Sign(message, _factory.PlatformAdminKey);

        var verifyResponse = await _client.PostAsJsonAsync("/auth/verify", new VerifyRequest(message, signature));
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var body = await verifyResponse.Content.ReadFromJsonAsync<VerifyResponse>();
        Assert.NotNull(body);
        Assert.True(body.PlatformAdmin);
        Assert.False(body.Holder);
        Assert.Empty(body.Memberships);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.Jwt);
        Assert.Contains(jwt.Claims, claim =>
            claim.Type == SovereignIdClaimTypes.PlatformAdmin && claim.Value == "true");
    }

    [Fact]
    public async Task Verify_WithoutAllowlist_DoesNotReturnPlatformAdminClaim()
    {
        var (key, address) = SiweTestHelper.CreateWallet();
        var nonceResponse = await _client.GetFromJsonAsync<NonceResponse>("/auth/nonce");
        Assert.NotNull(nonceResponse);

        var issuedAt = _factory.TimeProvider.GetUtcNow();
        var message = SiweTestHelper.BuildMessage(address, nonceResponse.Nonce, issuedAt);
        var signature = SiweTestHelper.Sign(message, key);

        var verifyResponse = await _client.PostAsJsonAsync("/auth/verify", new VerifyRequest(message, signature));
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var body = await verifyResponse.Content.ReadFromJsonAsync<VerifyResponse>();
        Assert.NotNull(body);
        Assert.False(body.PlatformAdmin);
    }
}

public sealed class AuthRbacWebApplicationFactory : WebApplicationFactory<Program>
{
    public ControllableTimeProvider TimeProvider { get; } = new();

    public EthECKey PlatformAdminKey { get; } = EthECKey.GenerateKey();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{PersistenceOptions.SectionName}:Provider"] = PersistenceProviders.InMemory,
                ["Auth:PlatformAdminAddresses:0"] = PlatformAdminKey.GetPublicAddress()
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TimeProvider);
        });
    }
}
