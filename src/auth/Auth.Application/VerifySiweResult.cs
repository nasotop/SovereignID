namespace Auth.Application;

public abstract record VerifySiweResult;

public sealed record VerifySiweSuccess(
    string Jwt,
    string Address,
    DateTimeOffset ExpiresAt) : VerifySiweResult;

public sealed record VerifySiweFailure(AuthFailure Failure) : VerifySiweResult;
