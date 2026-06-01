namespace Auth.Application;

public sealed record AuthFailure(
    string ErrorCode,
    int StatusCode,
    string Detail);
