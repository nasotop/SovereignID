using Microsoft.EntityFrameworkCore;
using Reports.Infrastructure.Persistence.Generated.Entities;

namespace Reports.Infrastructure.Persistence.Generated;

internal partial class ReportsDbContext : DbContext
{
    public ReportsDbContext(DbContextOptions<ReportsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Institution> Institutions { get; set; }
    public virtual DbSet<Student> Students { get; set; }
    public virtual DbSet<Credential> Credentials { get; set; }
    public virtual DbSet<VerificationLog> VerificationLogs { get; set; }
    public virtual DbSet<InstitutionMetricsDaily> InstitutionMetricsDailies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Institution>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("institutions_pkey");
            entity.ToTable("institutions");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasMaxLength(20).HasColumnName("code");
            entity.Property(e => e.LegalName).HasMaxLength(200).HasColumnName("legal_name");
            entity.Property(e => e.DisplayName).HasMaxLength(100).HasColumnName("display_name");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("students_pkey");
            entity.ToTable("students");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InstitutionId).HasColumnName("institution_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone").HasColumnName("created_at");
            entity.HasOne(d => d.Institution).WithMany(p => p.Students)
                .HasForeignKey(d => d.InstitutionId)
                .HasConstraintName("students_institution_id_fkey");
        });

        modelBuilder.Entity<Credential>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("credentials_pkey");
            entity.ToTable("credentials");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InstitutionId).HasColumnName("institution_id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.IssuedAt).HasColumnType("timestamp without time zone").HasColumnName("issued_at");
            entity.Property(e => e.RevokedAt).HasColumnType("timestamp without time zone").HasColumnName("revoked_at");
            entity.HasOne(d => d.Institution).WithMany(p => p.Credentials)
                .HasForeignKey(d => d.InstitutionId)
                .HasConstraintName("credentials_institution_id_fkey");
        });

        modelBuilder.Entity<VerificationLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("verification_logs_pkey");
            entity.ToTable("verification_logs");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CredentialId).HasColumnName("credential_id");
            entity.Property(e => e.Result).HasColumnName("result");
            entity.Property(e => e.VerifiedAt).HasColumnType("timestamp without time zone").HasColumnName("verified_at");
            entity.Property(e => e.VerifierIp).HasColumnName("verifier_ip");
            entity.HasOne(d => d.Credential).WithMany(p => p.VerificationLogs)
                .HasForeignKey(d => d.CredentialId)
                .HasConstraintName("verification_logs_credential_id_fkey");
        });

        modelBuilder.Entity<InstitutionMetricsDaily>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("institution_metrics_daily_pkey");
            entity.ToTable("institution_metrics_daily");
            entity.HasIndex(e => new { e.InstitutionId, e.MetricDate }, "uq_inst_metric_date").IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InstitutionId).HasColumnName("institution_id");
            entity.Property(e => e.MetricDate).HasColumnName("metric_date");
            entity.Property(e => e.CredentialsIssued).HasColumnName("credentials_issued");
            entity.Property(e => e.CredentialsRevoked).HasColumnName("credentials_revoked");
            entity.Property(e => e.VerificationsTotal).HasColumnName("verifications_total");
            entity.Property(e => e.VerificationsValid).HasColumnName("verifications_valid");
            entity.Property(e => e.VerificationsInvalid).HasColumnName("verifications_invalid");
            entity.Property(e => e.UniqueStudentsActive).HasColumnName("unique_students_active");
            entity.Property(e => e.ComputedAt).HasColumnType("timestamp without time zone").HasColumnName("computed_at");
            entity.HasOne(d => d.Institution).WithMany(p => p.InstitutionMetricsDailies)
                .HasForeignKey(d => d.InstitutionId)
                .HasConstraintName("institution_metrics_daily_institution_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
