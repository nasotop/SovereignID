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

        modelBuilder.Entity<UserGlobalRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_global_roles_pkey");

            entity.ToTable("user_global_roles", tb =>
                tb.HasComment("Roles globales de plataforma. Para MVP se usa platform_admin seeded por wallet."));

            entity.HasIndex(e => new { e.UserId, e.Role }, "uq_user_global_role").IsUnique();
            entity.HasIndex(e => e.UserId, "user_global_roles_user_id_idx");
            entity.HasIndex(e => e.Role, "user_global_roles_role_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Role)
                .HasColumnName("role")
                .HasColumnType("global_user_role")
                .HasComment("Rol global, ej: platform_admin");
            entity.Property(e => e.GrantedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("granted_at");
            entity.Property(e => e.RevokedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("revoked_at")
                .HasComment("Si tiene valor, el rol global esta inactivo");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_global_roles_user_id_fkey");
        });
    }
}
