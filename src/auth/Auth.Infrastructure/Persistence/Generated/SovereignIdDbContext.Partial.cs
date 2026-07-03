using Auth.Infrastructure.Persistence.Generated.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence.Generated;

internal partial class SovereignIdDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstitutionUser>(entity =>
        {
            entity.Property(e => e.Role)
                .HasColumnName("role")
                .HasColumnType("user_role");
        });
    }
}
