namespace Auth.Api.Models;

public sealed record NonceResponse(string Nonce, DateTimeOffset ExpiresAt);

public sealed record VerifyRequest(string Message, string Signature);

public sealed record MembershipResponse(string InstitutionId, string Role);

public sealed record VerifyResponse(
    string Jwt,
    string Address,
    DateTimeOffset ExpiresAt,
    Guid? UserId,
    bool PlatformAdmin,
    bool Holder,
    IReadOnlyList<MembershipResponse> Memberships);
