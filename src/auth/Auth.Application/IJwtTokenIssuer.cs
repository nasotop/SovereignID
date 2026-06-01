namespace Auth.Application;

public sealed record JwtToken(string Token, DateTimeOffset ExpiresAt);

public interface IJwtTokenIssuer
{
    JwtToken Issue(string address);
}
