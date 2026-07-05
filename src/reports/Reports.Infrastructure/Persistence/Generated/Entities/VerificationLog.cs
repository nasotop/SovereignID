using System.Net;

namespace Reports.Infrastructure.Persistence.Generated.Entities;

public partial class VerificationLog
{
    public Guid Id { get; set; }
    public Guid? CredentialId { get; set; }
    public VerificationResult Result { get; set; }
    public DateTime VerifiedAt { get; set; }
    public IPAddress VerifierIp { get; set; } = null!;
    public virtual Credential? Credential { get; set; }
}
