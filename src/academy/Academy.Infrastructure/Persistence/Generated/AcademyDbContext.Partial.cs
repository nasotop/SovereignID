using Academy.Infrastructure.Persistence.Generated.Entities;
using Microsoft.EntityFrameworkCore;

namespace Academy.Infrastructure.Persistence.Generated;

internal partial class AcademyDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstitutionInvitation>(entity =>
        {
            entity.Property(e => e.Role)
                .HasColumnName("role")
                .HasColumnType("user_role");
        });

        modelBuilder.Entity<InstitutionUser>(entity =>
        {
            entity.Property(e => e.Role)
                .HasColumnName("role")
                .HasColumnType("user_role");
        });

        modelBuilder.Entity<StudentWallet>(entity =>
        {
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasColumnType("wallet_status");
        });
    }
}
