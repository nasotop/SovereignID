using Auth.Application;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Authorization;

public sealed class InMemoryUserAuthorizationResolver : IUserAuthorizationResolver
{
    private readonly AuthOptions _options;

    public InMemoryUserAuthorizationResolver(IOptions<AuthOptions> options)
    {
        _options = options.Value;
    }

    public Task<UserAuthorizationProfile> ResolveAsync(string walletAddress, CancellationToken cancellationToken = default)
    {
        var normalizedAddress = walletAddress.ToLowerInvariant();
        var isPlatformAdmin = _options.PlatformAdminAddresses
            .Any(address => string.Equals(address, normalizedAddress, StringComparison.OrdinalIgnoreCase));

        var profile = new UserAuthorizationProfile(
            UserId: null,
            IsPlatformAdmin: isPlatformAdmin,
            IsHolder: false,
            Memberships: []);

        return Task.FromResult(profile);
    }
}
