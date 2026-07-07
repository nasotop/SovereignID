using Microsoft.EntityFrameworkCore;
using Verifier.Infrastructure.Persistence.Generated.Entities;

namespace Verifier.Infrastructure.Persistence.Generated;

internal partial class VerifierDbContext
{
    public virtual DbSet<StudentWallet> StudentWallets { get; set; } = null!;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudentWallet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("student_wallets_pkey");
            entity.ToTable("student_wallets");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.WalletAddress)
                .HasMaxLength(42)
                .HasColumnName("wallet_address");
        });
    }
}
