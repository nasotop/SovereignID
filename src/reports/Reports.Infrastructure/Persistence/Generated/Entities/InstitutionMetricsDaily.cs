namespace Reports.Infrastructure.Persistence.Generated.Entities;

public partial class InstitutionMetricsDaily
{
    public long Id { get; set; }
    public Guid InstitutionId { get; set; }
    public DateOnly MetricDate { get; set; }
    public int CredentialsIssued { get; set; }
    public int CredentialsRevoked { get; set; }
    public int VerificationsTotal { get; set; }
    public int VerificationsValid { get; set; }
    public int VerificationsInvalid { get; set; }
    public int UniqueStudentsActive { get; set; }
    public DateTime ComputedAt { get; set; }
    public virtual Institution Institution { get; set; } = null!;
}
