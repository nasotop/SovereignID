using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SovereignID.Authorization;

namespace Reports.IntegrationTests;

internal static class JwtTestHelper
{
    public const string TestSigningKey = "development-only-signing-key-32-bytes!";
    public const string TestIssuer = "sovereignid-auth";
    public const string TestAudience = "sovereignid-clients";

    public static string CreatePlatformAdminToken() =>
        CreateToken("0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", platformAdmin: true);

    public static string CreateInstitutionAdminToken(Guid institutionId) =>
        CreateToken(
            "0xbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
            memberships: [new InstitutionMembership(institutionId, "admin")]);

    public static string CreateInstitutionViewerToken(Guid institutionId) =>
        CreateToken(
            "0xcccccccccccccccccccccccccccccccccccccccc",
            memberships: [new InstitutionMembership(institutionId, "viewer")]);

    public static string CreateInstitutionIssuerToken(Guid institutionId) =>
        CreateToken(
            "0xdddddddddddddddddddddddddddddddddddddddd",
            memberships: [new InstitutionMembership(institutionId, "issuer")]);

    public static string CreateToken(
        string walletAddress,
        bool platformAdmin = false,
        IReadOnlyList<InstitutionMembership>? memberships = null)
    {
        var normalizedAddress = walletAddress.ToLowerInvariant();
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
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
