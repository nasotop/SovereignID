using Issuer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Issuer.Infrastructure.Persistence;

internal sealed class IssuerDbContext : DbContext
{
    public IssuerDbContext(DbContextOptions<IssuerDbContext> options)
        : base(options)
    {
    }

    public DbSet<CareerEntity> Careers => Set<CareerEntity>();

    public DbSet<CredentialEntity> Credentials => Set<CredentialEntity>();

    public DbSet<CredentialTypeEntity> CredentialTypes => Set<CredentialTypeEntity>();

    public DbSet<InstitutionEntity> Institutions => Set<InstitutionEntity>();

    public DbSet<StudentEntity> Students => Set<StudentEntity>();

    public DbSet<StudentWalletEntity> StudentWallets => Set<StudentWalletEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstitutionEntity>(entity =>
        {
            entity.ToTable("institutions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Did).HasColumnName("did").HasMaxLength(200);
            entity.Property(e => e.IssuerWalletAddress).HasColumnName("issuer_wallet_address").HasMaxLength(42);
            entity.Property(e => e.PublicKey).HasColumnName("public_key");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
        });

        modelBuilder.Entity<StudentEntity>(entity =>
        {
            entity.ToTable("students");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InstitutionId).HasColumnName("institution_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
        });

        modelBuilder.Entity<StudentWalletEntity>(entity =>
        {
            entity.ToTable("student_wallets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.Did).HasColumnName("did").HasMaxLength(200);
            entity.Property(e => e.Status).HasColumnName("status").HasColumnType("wallet_status");
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");
        });

        modelBuilder.Entity<CareerEntity>(entity =>
        {
            entity.ToTable("careers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InstitutionId).HasColumnName("institution_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
        });

        modelBuilder.Entity<CredentialTypeEntity>(entity =>
        {
            entity.ToTable("credential_types");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(40);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
        });

        modelBuilder.Entity<CredentialEntity>(entity =>
        {
            entity.ToTable("credentials");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InstitutionId).HasColumnName("institution_id");
            entity.Property(e => e.CredentialTypeId).HasColumnName("credential_type_id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.CareerId).HasColumnName("career_id");
            entity.Property(e => e.IssuedToWalletId).HasColumnName("issued_to_wallet_id");
            entity.Property(e => e.SubjectDid).HasColumnName("subject_did").HasMaxLength(200);
            entity.Property(e => e.IssuerDid).HasColumnName("issuer_did").HasMaxLength(200);
            entity.Property(e => e.IpfsCid).HasColumnName("ipfs_cid").HasMaxLength(80);
            entity.Property(e => e.IpfsGatewayUrl).HasColumnName("ipfs_gateway_url").HasMaxLength(500);
            entity.Property(e => e.ContentHash).HasColumnName("content_hash").HasMaxLength(66);
            entity.Property(e => e.TransactionHash).HasColumnName("transaction_hash").HasMaxLength(66);
            entity.Property(e => e.BlockNumber).HasColumnName("block_number");
            entity.Property(e => e.ChainId).HasColumnName("chain_id");
            entity.Property(e => e.Eip712Signature).HasColumnName("eip712_signature").HasMaxLength(132);
            entity.Property(e => e.Status).HasColumnName("status").HasColumnType("credential_status");
            entity.Property(e => e.IssuedAt).HasColumnName("issued_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        });
    }
}
