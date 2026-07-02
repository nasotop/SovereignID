using System.Net;
using Verifier.Application;
using Verifier.Infrastructure.Persistence.Generated;
using DomainVerificationResult = Verifier.Domain.VerificationResult;
using VerificationLogEntity = Verifier.Infrastructure.Persistence.Generated.Entities.VerificationLog;
using VerificationResultEnum = Verifier.Infrastructure.Persistence.Generated.Entities.VerificationResult;

namespace Verifier.Infrastructure.Persistence.Stores.VerificationLogStore;

internal sealed class EfVerificationLogStore : IVerificationLogStore
{
    private const int CredentialIdQueryMaxLength = 80;
    private const int UserAgentMaxLength = 500;

    private readonly VerifierDbContext _db;
    private readonly IVerifierRequestContext _requestContext;

    public EfVerificationLogStore(VerifierDbContext db, IVerifierRequestContext requestContext)
    {
        _db = db;
        _requestContext = requestContext;
    }

    public async Task RecordAsync(VerificationLogEntry entry, CancellationToken cancellationToken)
    {
        var log = new VerificationLogEntity
        {
            CredentialId = entry.CredentialId,
            CredentialIdQuery = Truncate(entry.CredentialIdQuery, CredentialIdQueryMaxLength),
            Result = ToEntityResult(entry.Result),
            NotRevoked = entry.NotRevoked,
            NotExpired = entry.NotExpired,
            VerifierIp = _requestContext.ClientIp ?? IPAddress.Loopback,
            VerifierUserAgent = ResolveUserAgent(_requestContext.UserAgent)
        };

        _db.VerificationLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static VerificationResultEnum ToEntityResult(DomainVerificationResult result) => result switch
    {
        DomainVerificationResult.Valid => VerificationResultEnum.Valid,
        DomainVerificationResult.Revoked => VerificationResultEnum.Revoked,
        DomainVerificationResult.Expired => VerificationResultEnum.Expired,
        DomainVerificationResult.NotFound => VerificationResultEnum.NotFound,
        _ => throw new ArgumentOutOfRangeException(nameof(result), result, "Unknown verification result.")
    };

    private static string? ResolveUserAgent(string? userAgent) =>
        string.IsNullOrWhiteSpace(userAgent) ? null : Truncate(userAgent, UserAgentMaxLength);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
