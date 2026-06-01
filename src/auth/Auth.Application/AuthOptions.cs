namespace Auth.Application;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public int ChallengeTtlSeconds { get; set; } = 600;
    public int JwtTtlHours { get; set; } = 24;
    public int AllowedChainId { get; set; } = 11155111;
    public string JwtIssuer { get; set; } = "sovereignid-auth";
    public string JwtAudience { get; set; } = "sovereignid-clients";
    public string JwtSigningKey { get; set; } = string.Empty;
}
