namespace Auth.Application;

public abstract record VerifySiweResult;

public sealed record VerifySiweSuccess(
    string Jwt,
    string Address,
    DateTimeOffset ExpiresAt,
    Guid? UserId,
    bool PlatformAdmin,
    bool Holder,
    IReadOnlyList<InstitutionMembershipRecord> Memberships) : VerifySiweResult;

public sealed record VerifySiweFailure(AuthFailure Failure) : VerifySiweResult;
