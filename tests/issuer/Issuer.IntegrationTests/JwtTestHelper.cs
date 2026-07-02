using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Issuer.IntegrationTests;

internal static class JwtTestHelper
{
    public const string TestSigningKey = "development-signing-key-32-bytes-min!";
    public const string TestIssuer = "sovereignid-auth";
    public const string TestAudience = "sovereignid-clients";
    public const string HolderWalletAddress = "0x2222222222222222222222222222222222222222";
    public const string HolderSubjectDid = "did:ethr:sepolia:0x2222222222222222222222222222222222222222";

    public static string CreateHolderToken(DateTimeOffset? expiresAt = null)
    {
        var expiry = expiresAt ?? DateTimeOffset.UtcNow.AddHours(1);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, HolderWalletAddress),
            new Claim("address", HolderWalletAddress),
            new Claim("did", HolderSubjectDid)
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: expiry.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
