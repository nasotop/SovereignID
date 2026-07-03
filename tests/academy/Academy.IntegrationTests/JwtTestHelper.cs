using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SovereignID.Authorization;

namespace Academy.IntegrationTests;

internal static class JwtTestHelper
{
    public const string TestSigningKey = "development-only-signing-key-32-bytes!";
    public const string TestIssuer = "sovereignid-auth";
    public const string TestAudience = "sovereignid-clients";
    public const string PlatformAdminWalletAddress = "0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    public static string CreatePlatformAdminToken(DateTimeOffset? expiresAt = null) =>
        CreateToken(PlatformAdminWalletAddress, platformAdmin: true, expiresAt: expiresAt);

    public static string CreateInstitutionAdminToken(
        Guid institutionId,
        string walletAddress = "0xbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
        DateTimeOffset? expiresAt = null) =>
        CreateToken(
            walletAddress,
            memberships: [new InstitutionMembership(institutionId, "admin")],
            expiresAt: expiresAt);

    public static string CreateToken(
        string walletAddress,
        bool platformAdmin = false,
        bool holder = false,
        IReadOnlyList<InstitutionMembership>? memberships = null,
        DateTimeOffset? expiresAt = null)
    {
        var normalizedAddress = walletAddress.ToLowerInvariant();
        var expiry = expiresAt ?? DateTimeOffset.UtcNow.AddHours(1);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, normalizedAddress),
            new("address", normalizedAddress),
            new("did", $"did:ethr:sepolia:{normalizedAddress}"),
        };

        if (platformAdmin)
        {
            claims.Add(new Claim(SovereignIdClaimTypes.PlatformAdmin, "true"));
        }

        if (holder)
        {
            claims.Add(new Claim(SovereignIdClaimTypes.Holder, "true"));
        }

        foreach (var membership in memberships ?? [])
        {
            claims.Add(new Claim(
                SovereignIdClaimTypes.Membership,
                MembershipClaimValue.Format(membership.InstitutionId, membership.Role)));
        }

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: expiry.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
