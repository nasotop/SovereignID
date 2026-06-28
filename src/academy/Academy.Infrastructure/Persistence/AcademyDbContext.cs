using Academy.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Academy.Infrastructure.Persistence;

internal sealed class AcademyDbContext : DbContext
{
    public AcademyDbContext(DbContextOptions<AcademyDbContext> options)
        : base(options)
    {
    }

    public DbSet<CareerEntity> Careers => Set<CareerEntity>();

    public DbSet<InstitutionEntity> Institutions => Set<InstitutionEntity>();

    public DbSet<InstitutionInvitationEntity> InstitutionInvitations => Set<InstitutionInvitationEntity>();

    public DbSet<InstitutionUserEntity> InstitutionUsers => Set<InstitutionUserEntity>();

    public DbSet<StudentEntity> Students => Set<StudentEntity>();

    public DbSet<StudentWalletEntity> StudentWallets => Set<StudentWalletEntity>();

    public DbSet<UserEntity> Users => Set<UserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstitutionEntity>(entity =>
        {
            entity.ToTable("institutions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(40);
            entity.Property(e => e.LegalName).HasColumnName("legal_name").HasMaxLength(200);
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(120);
            entity.Property(e => e.Did).HasColumnName("did").HasMaxLength(200);
            entity.Property(e => e.IssuerWalletAddress).HasColumnName("issuer_wallet_address").HasMaxLength(42);
            entity.Property(e => e.PublicKey).HasColumnName("public_key");
            entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(2);
            entity.Property(e => e.WebsiteUrl).HasColumnName("website_url").HasMaxLength(300);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.RegisteredAt).HasColumnName("registered_at");
            entity.Property(e => e.DeactivatedAt).HasColumnName("deactivated_at");
        });

        modelBuilder.Entity<InstitutionInvitationEntity>(entity =>
        {
            entity.ToTable("institution_invitations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InstitutionId).HasColumnName("institution_id");
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(200);
            entity.Property(e => e.Role).HasColumnName("role").HasColumnType("user_role");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(64);
            entity.Property(e => e.InvitationUrl).HasColumnName("invitation_url").HasMaxLength(700);
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.AcceptedAt).HasColumnName("accepted_at");
            entity.Property(e => e.AcceptedByUserId).HasColumnName("accepted_by_user_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
        });

        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.WalletAddress).HasColumnName("wallet_address").HasMaxLength(42);
            entity.Property(e => e.Did).HasColumnName("did").HasMaxLength(200);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(200);
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(120);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
        });

        modelBuilder.Entity<InstitutionUserEntity>(entity =>
        {
            entity.ToTable("institution_users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InstitutionId).HasColumnName("institution_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Role).HasColumnName("role").HasColumnType("user_role");
            entity.Property(e => e.GrantedByUserId).HasColumnName("granted_by_user_id");
            entity.Property(e => e.GrantedAt).HasColumnName("granted_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
        });

        modelBuilder.Entity<StudentEntity>(entity =>
        {
            entity.ToTable("students");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InstitutionId).HasColumnName("institution_id");
            entity.Property(e => e.ExternalReference).HasColumnName("external_reference").HasMaxLength(80);
            entity.Property(e => e.EnrollmentYear).HasColumnName("enrollment_year");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<StudentWalletEntity>(entity =>
        {
            entity.ToTable("student_wallets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.WalletAddress).HasColumnName("wallet_address").HasMaxLength(42);
            entity.Property(e => e.Did).HasColumnName("did").HasMaxLength(200);
            entity.Property(e => e.Status).HasColumnName("status").HasColumnType("wallet_status");
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");
            entity.Property(e => e.ActivatedAt).HasColumnName("activated_at");
            entity.Property(e => e.RotatedAt).HasColumnName("rotated_at");
            entity.Property(e => e.RotationReason).HasColumnName("rotation_reason").HasMaxLength(80);
        });

        modelBuilder.Entity<CareerEntity>(entity =>
        {
            entity.ToTable("careers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InstitutionId).HasColumnName("institution_id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(40);
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

    }
}

