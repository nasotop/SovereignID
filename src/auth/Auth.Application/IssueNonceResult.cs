namespace Auth.Application;

public sealed record IssueNonceResult(string Nonce, DateTimeOffset ExpiresAt);
