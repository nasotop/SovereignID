using System.Security.Claims;

namespace SovereignID.Authorization;

public static class ClaimsPrincipalExtensions
{
    public static bool IsPlatformAdmin(this ClaimsPrincipal principal) =>
        principal.HasClaim(SovereignIdClaimTypes.PlatformAdmin, "true");

    public static bool IsHolder(this ClaimsPrincipal principal) =>
        principal.HasClaim(SovereignIdClaimTypes.Holder, "true");

    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(SovereignIdClaimTypes.UserId)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    public static IReadOnlyList<InstitutionMembership> GetMemberships(this ClaimsPrincipal principal) =>
        principal.FindAll(SovereignIdClaimTypes.Membership)
            .Select(claim => claim.Value)
            .Select(value =>
            {
                MembershipClaimValue.TryParse(value, out var membership);
                return membership;
            })
            .Where(membership => membership is not null)
            .Cast<InstitutionMembership>()
            .ToList();

    public static bool HasInstitutionRole(
        this ClaimsPrincipal principal,
        Guid institutionId,
        params string[] roles)
    {
        if (roles.Length == 0)
        {
            return false;
        }

        var normalizedRoles = roles
            .Select(role => role.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        return principal.GetMemberships()
            .Any(membership =>
                membership.InstitutionId == institutionId
                && normalizedRoles.Contains(membership.Role));
    }

    public static bool HasAnyInstitutionRole(
        this ClaimsPrincipal principal,
        params string[] roles)
    {
        if (roles.Length == 0)
        {
            return false;
        }

        var normalizedRoles = roles
            .Select(role => role.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        return principal.GetMemberships()
            .Any(membership => normalizedRoles.Contains(membership.Role));
    }
}
