namespace Auth.Application;

public interface IUserAuthorizationResolver
{
    Task<UserAuthorizationProfile> ResolveAsync(string walletAddress, CancellationToken cancellationToken = default);
}
