using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Auth.Application;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SovereignID.Authorization;

namespace Auth.Infrastructure;

public sealed class JwtBearerTokenIssuer : IJwtTokenIssuer
{
    private readonly TimeProvider _timeProvider;
    private readonly AuthOptions _options;

    public JwtBearerTokenIssuer(TimeProvider timeProvider, IOptions<AuthOptions> options)
    {
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public JwtToken Issue(string address, UserAuthorizationProfile authorizationProfile)
    {
        var normalizedAddress = address.ToLowerInvariant();
        var issuedAt = _timeProvider.GetUtcNow();
        var expiresAt = issuedAt.AddHours(_options.JwtTtlHours);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, normalizedAddress),
            new("address", normalizedAddress),
            new("did", $"did:ethr:sepolia:{normalizedAddress}"),
        };

        if (authorizationProfile.UserId is Guid userId)
        {
            claims.Add(new Claim(SovereignIdClaimTypes.UserId, userId.ToString("D")));
        }

        if (authorizationProfile.IsPlatformAdmin)
        {
            claims.Add(new Claim(SovereignIdClaimTypes.PlatformAdmin, "true"));
        }

        if (authorizationProfile.IsHolder)
        {
            claims.Add(new Claim(SovereignIdClaimTypes.Holder, "true"));
        }

        foreach (var membership in authorizationProfile.Memberships)
        {
            claims.Add(new Claim(
                SovereignIdClaimTypes.Membership,
                MembershipClaimValue.Format(membership.InstitutionId, membership.Role)));
        }

        var token = new JwtSecurityToken(
            issuer: _options.JwtIssuer,
            audience: _options.JwtAudience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
