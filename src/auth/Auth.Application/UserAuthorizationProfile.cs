namespace Auth.Application;

public sealed record InstitutionMembershipRecord(Guid InstitutionId, string Role);

public sealed record UserAuthorizationProfile(
    Guid? UserId,
    bool IsPlatformAdmin,
    bool IsHolder,
    IReadOnlyList<InstitutionMembershipRecord> Memberships);
