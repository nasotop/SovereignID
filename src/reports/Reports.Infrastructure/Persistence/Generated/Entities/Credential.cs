namespace Reports.Infrastructure.Persistence.Generated.Entities;

public partial class Credential
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid StudentId { get; set; }
    public CredentialStatus Status { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public virtual Institution Institution { get; set; } = null!;
    public virtual ICollection<VerificationLog> VerificationLogs { get; set; } = [];
}
