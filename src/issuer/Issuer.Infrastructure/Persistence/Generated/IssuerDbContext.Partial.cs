using Issuer.Infrastructure.Persistence.Generated.Entities;
using Microsoft.EntityFrameworkCore;

namespace Issuer.Infrastructure.Persistence.Generated;

internal partial class IssuerDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Credential>()
            .Property(e => e.Status)
            .HasColumnName("status");

        modelBuilder.Entity<StudentWallet>()
            .Property(e => e.Status)
            .HasColumnName("status");
    }
}
