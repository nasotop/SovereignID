using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SovereignID.Authorization;

namespace Issuer.IntegrationTests;

internal static class JwtTestHelper
{
    public const string TestSigningKey = "development-signing-key-32-bytes-min!";
    public const string TestIssuer = "sovereignid-auth";
    public const string TestAudience = "sovereignid-clients";
    public const string HolderWalletAddress = "0x2222222222222222222222222222222222222222";
    public const string HolderSubjectDid = "did:ethr:sepolia:0x2222222222222222222222222222222222222222";
    public const string IssuerWalletAddress = "0x1111111111111111111111111111111111111111";

    public static string CreateHolderToken(DateTimeOffset? expiresAt = null) =>
        CreateToken(HolderWalletAddress, HolderSubjectDid, holder: true, expiresAt: expiresAt);

    public static string CreateIssuerToken(
        Guid institutionId,
        string role = "issuer",
        string? walletAddress = null,
        DateTimeOffset? expiresAt = null)
    {
        var address = walletAddress ?? IssuerWalletAddress;
        var did = $"did:ethr:sepolia:{address.ToLowerInvariant()}";
        return CreateToken(
            address,
            did,
            memberships: [new InstitutionMembership(institutionId, role)],
            expiresAt: expiresAt);
    }

    public static string CreateToken(
        string walletAddress,
        string did,
        bool platformAdmin = false,
        bool holder = false,
        IReadOnlyList<InstitutionMembership>? memberships = null,
        DateTimeOffset? expiresAt = null)
    {
        var expiry = expiresAt ?? DateTimeOffset.UtcNow.AddHours(1);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, walletAddress),
            new("address", walletAddress),
            new("did", did),
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
