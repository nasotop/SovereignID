namespace Verifier.Infrastructure.Persistence.Generated.Entities;

/// <summary>Wallet de estudiante (lectura mínima para join en verificación).</summary>
public sealed class StudentWallet
{
    public Guid Id { get; set; }

    public string WalletAddress { get; set; } = null!;
}
