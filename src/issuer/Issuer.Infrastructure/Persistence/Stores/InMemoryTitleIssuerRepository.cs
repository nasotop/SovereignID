using Issuer.Application;

namespace Issuer.Infrastructure.Persistence.Stores;

internal sealed class InMemoryTitleIssuerRepository : ITitleIssuerRepository
{
    public static readonly Guid InstitutionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid StudentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid CareerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid WalletId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public Task<InstitutionIssuerWalletLinked?> LinkInstitutionIssuerWalletAsync(
        LinkInstitutionIssuerWalletCommand command,
        CancellationToken cancellationToken)
    {
        if (command.InstitutionId != InstitutionId)
        {
            return Task.FromResult<InstitutionIssuerWalletLinked?>(null);
        }

        return Task.FromResult<InstitutionIssuerWalletLinked?>(new InstitutionIssuerWalletLinked(
            InstitutionId,
            command.WalletAddress,
            command.Did,
            command.PublicKey));
    }

    public Task<StudentTitleLinked?> LinkStudentTitleAsync(
        LinkStudentTitleCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (command.StudentId != StudentId
            || command.CareerId != CareerId
            || !string.Equals(command.CredentialTypeCode, "TITULO", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<StudentTitleLinked?>(null);
        }

        return Task.FromResult<StudentTitleLinked?>(new StudentTitleLinked(
            Guid.NewGuid(),
            InstitutionId,
            StudentId,
            CareerId,
            WalletId,
            "did:ethr:sepolia:0x2222222222222222222222222222222222222222",
            "did:ethr:sepolia:0x1111111111111111111111111111111111111111",
            "active",
            now));
    }
}
