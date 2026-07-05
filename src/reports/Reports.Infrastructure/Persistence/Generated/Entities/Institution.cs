namespace Reports.Infrastructure.Persistence.Generated.Entities;

public partial class Institution
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string LegalName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public bool IsActive { get; set; }
    public virtual ICollection<Student> Students { get; set; } = [];
    public virtual ICollection<Credential> Credentials { get; set; } = [];
    public virtual ICollection<InstitutionMetricsDaily> InstitutionMetricsDailies { get; set; } = [];
}
