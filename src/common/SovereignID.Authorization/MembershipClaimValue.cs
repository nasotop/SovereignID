namespace SovereignID.Authorization;

public static class MembershipClaimValue
{
    public static string Format(Guid institutionId, string role) =>
        $"{institutionId:D}:{role.Trim().ToLowerInvariant()}";

    public static bool TryParse(string value, out InstitutionMembership membership)
    {
        membership = default!;
        var separatorIndex = value.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex >= value.Length - 1)
        {
            return false;
        }

        var institutionPart = value[..separatorIndex];
        var rolePart = value[(separatorIndex + 1)..];
        if (!Guid.TryParse(institutionPart, out var institutionId)
            || string.IsNullOrWhiteSpace(rolePart))
        {
            return false;
        }

        membership = new InstitutionMembership(institutionId, rolePart.ToLowerInvariant());
        return true;
    }
}
