namespace Reports.Infrastructure.Persistence.Generated.Entities;

public partial class Student
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public virtual Institution Institution { get; set; } = null!;
}
