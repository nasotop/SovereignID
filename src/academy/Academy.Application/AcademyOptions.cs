namespace Academy.Application;

public sealed class AcademyOptions
{
    public const string SectionName = "Academy";

    public string InvitationBaseUrl { get; set; } = "http://localhost:4200/institution-invitations/accept";

    public int InvitationTtlHours { get; set; } = 72;
}

