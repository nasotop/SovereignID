using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Auth.Api.Models;
using Nethereum.Signer;
using Xunit;

namespace Auth.IntegrationTests;

public sealed class AuthEndpointsTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AC01_HappyPath_ReturnsJwtWithLowercaseSub()
    {
        var (key, address) = SiweTestHelper.CreateWallet();
        var nonceResponse = await _client.GetFromJsonAsync<NonceResponse>("/auth/nonce");
        Assert.NotNull(nonceResponse);

        var issuedAt = _factory.TimeProvider.GetUtcNow();
        var message = SiweTestHelper.BuildMessage(address, nonceResponse.Nonce, issuedAt);
        var signature = SiweTestHelper.Sign(message, key);

        var verifyResponse = await PostVerify(message, signature);
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var body = await verifyResponse.Content.ReadFromJsonAsync<VerifyResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Jwt));
        Assert.Equal(address, body.Address, StringComparer.OrdinalIgnoreCase);
        Assert.True(body.ExpiresAt > issuedAt);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.Jwt);
        Assert.Equal(address.ToLowerInvariant(), jwt.Subject);
    }

    [Fact]
    public async Task AC02_Replay_ReturnsNonceConsumed()
    {
        var (message, signature) = await CreateSignedVerifyPayloadAsync();

        var first = await PostVerify(message, signature);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await PostVerify(message, signature);
        var error = await ReadErrorCode(second);

        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal("nonce_consumed", error);
    }

    [Fact]
    public async Task AC03_ExpiredNonce_ReturnsNonceExpired()
    {
        var (key, address) = SiweTestHelper.CreateWallet();
        var nonceResponse = await _client.GetFromJsonAsync<NonceResponse>("/auth/nonce");
        Assert.NotNull(nonceResponse);

        var issuedAt = _factory.TimeProvider.GetUtcNow();
        var message = SiweTestHelper.BuildMessage(address, nonceResponse.Nonce, issuedAt);
        var signature = SiweTestHelper.Sign(message, key);

        _factory.TimeProvider.Advance(TimeSpan.FromMinutes(11));

        var response = await PostVerify(message, signature);
        var error = await ReadErrorCode(response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("nonce_expired", error);
    }

    [Fact]
    public async Task AC04_UnsupportedChain_ReturnsUnsupportedChain()
    {
        var (key, address) = SiweTestHelper.CreateWallet();
        var nonceResponse = await _client.GetFromJsonAsync<NonceResponse>("/auth/nonce");
        Assert.NotNull(nonceResponse);

        var issuedAt = _factory.TimeProvider.GetUtcNow();
        var message = SiweTestHelper.BuildMessage(address, nonceResponse.Nonce, issuedAt, chainId: 1);
        var signature = SiweTestHelper.Sign(message, key);

        var response = await PostVerify(message, signature);
        var error = await ReadErrorCode(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("unsupported_chain", error);
    }

    [Fact]
    public async Task AC05_TamperedMessage_ReturnsSignatureMismatch()
    {
        var (message, signature) = await CreateSignedVerifyPayloadAsync();
        var tampered = message.Replace("Sign in", "Sign out", StringComparison.Ordinal);

        var response = await PostVerify(tampered, signature);
        var error = await ReadErrorCode(response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("signature_mismatch", error);
    }

    [Fact]
    public async Task AC06_UnknownNonce_ReturnsNonceUnknown()
    {
        var (key, address) = SiweTestHelper.CreateWallet();
        await _client.GetFromJsonAsync<NonceResponse>("/auth/nonce");

        var issuedAt = _factory.TimeProvider.GetUtcNow();
        var unknownNonce = "0123456789abcdef0123456789abcdef";
        var message = SiweTestHelper.BuildMessage(address, unknownNonce, issuedAt);
        var signature = SiweTestHelper.Sign(message, key);

        var response = await PostVerify(message, signature);
        var error = await ReadErrorCode(response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("nonce_unknown", error);
    }

    [Fact]
    public async Task AC07_MalformedPayload_ReturnsSiweParseFailedWithDetail()
    {
        var (key, address) = SiweTestHelper.CreateWallet();
        var nonceResponse = await _client.GetFromJsonAsync<NonceResponse>("/auth/nonce");
        Assert.NotNull(nonceResponse);

        var issuedAt = _factory.TimeProvider.GetUtcNow();
        var message = SiweTestHelper.BuildMessage(address, nonceResponse.Nonce, issuedAt)
            .Replace("Version: 1", "Version: 2", StringComparison.Ordinal);
        var signature = SiweTestHelper.Sign(message, key);

        var response = await PostVerify(message, signature);
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();
        var error = document.GetProperty("error").GetString();
        var detail = document.GetProperty("detail").GetString();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("siwe_parse_failed", error);
        Assert.Contains("Version", detail, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(string Message, string Signature)> CreateSignedVerifyPayloadAsync()
    {
        var (key, address) = SiweTestHelper.CreateWallet();
        var nonceResponse = await _client.GetFromJsonAsync<NonceResponse>("/auth/nonce")
            ?? throw new InvalidOperationException("Nonce response was null.");

        var issuedAt = _factory.TimeProvider.GetUtcNow();
        var message = SiweTestHelper.BuildMessage(address, nonceResponse.Nonce, issuedAt);
        var signature = SiweTestHelper.Sign(message, key);
        return (message, signature);
    }

    private Task<HttpResponseMessage> PostVerify(string message, string signature) =>
        _client.PostAsJsonAsync("/auth/verify", new VerifyRequest(message, signature));

    private static async Task<string?> ReadErrorCode(HttpResponseMessage response)
    {
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();
        return document.GetProperty("error").GetString();
    }
}
