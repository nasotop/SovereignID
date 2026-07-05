using Microsoft.EntityFrameworkCore;
using Reports.Infrastructure.Persistence.Generated.Entities;

namespace Reports.Infrastructure.Persistence.Generated;

internal partial class ReportsDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<VerificationResult>("verification_result");
        modelBuilder.HasPostgresEnum<CredentialStatus>("credential_status");
    }
}
