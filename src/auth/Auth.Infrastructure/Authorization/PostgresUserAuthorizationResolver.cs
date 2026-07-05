using Auth.Application;
using Auth.Infrastructure.Persistence.Generated;
using Auth.Infrastructure.Persistence.Generated.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Authorization;

internal sealed class PostgresUserAuthorizationResolver : IUserAuthorizationResolver
{
    private readonly SovereignIdDbContext _dbContext;
    private readonly AuthOptions _options;
    private readonly TimeProvider _timeProvider;

    public PostgresUserAuthorizationResolver(
        SovereignIdDbContext dbContext,
        IOptions<AuthOptions> options,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<UserAuthorizationProfile> ResolveAsync(
        string walletAddress,
        CancellationToken cancellationToken = default)
    {
        var normalizedAddress = walletAddress.ToLowerInvariant();
        var isPlatformAdmin = _options.PlatformAdminAddresses
            .Any(address => string.Equals(address, normalizedAddress, StringComparison.OrdinalIgnoreCase));

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.WalletAddress == normalizedAddress && candidate.IsActive,
                cancellationToken);

        IReadOnlyList<InstitutionMembershipRecord> memberships = [];
        if (user is not null)
        {
            var hasGlobalPlatformAdminRole = await _dbContext.UserGlobalRoles
                .AsNoTracking()
                .AnyAsync(
                    role => role.UserId == user.Id
                        && role.Role == GlobalUserRole.platform_admin
                        && role.RevokedAt == null,
                    cancellationToken);

            isPlatformAdmin = isPlatformAdmin || hasGlobalPlatformAdminRole;

            var membershipRows = await _dbContext.InstitutionUsers
                .AsNoTracking()
                .Where(row => row.UserId == user.Id && row.RevokedAt == null)
                .ToListAsync(cancellationToken);

            memberships = membershipRows
                .Select(row => new InstitutionMembershipRecord(row.InstitutionId, row.Role.ToString()))
                .ToList();

            await _dbContext.Users
                .Where(candidate => candidate.Id == user.Id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        candidate => candidate.LastLoginAt,
                        ToPostgresTimestamp(_timeProvider.GetUtcNow())),
                    cancellationToken);
        }

        var isHolder = await _dbContext.StudentWallets
            .AsNoTracking()
            .AnyAsync(
                wallet => wallet.WalletAddress == normalizedAddress
                    && wallet.IsPrimary
                    && wallet.RotatedAt == null,
                cancellationToken);

        return new UserAuthorizationProfile(user?.Id, isPlatformAdmin, isHolder, memberships);
    }

    private static DateTime ToPostgresTimestamp(DateTimeOffset value) =>
        DateTime.SpecifyKind(value.UtcDateTime, DateTimeKind.Unspecified);
}
