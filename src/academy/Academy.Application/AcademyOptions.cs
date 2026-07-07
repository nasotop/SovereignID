namespace Academy.Application;

public sealed class AcademyOptions
{
    public const string SectionName = "Academy";

    public string InvitationBaseUrl { get; set; } = "http://localhost:4200/institution-invitations/accept";

    public int InvitationTtlHours { get; set; } = 72;

    public EmailOptions Email { get; set; } = new();
}

public sealed class EmailOptions
{
    public string Provider { get; set; } = EmailProviders.Logging;

    public SmtpOptions Smtp { get; set; } = new();
}

public static class EmailProviders
{
    public const string Logging = "Logging";

    public const string Smtp = "Smtp";
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public bool UseStartTls { get; set; } = true;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = string.Empty;

    public string FromDisplayName { get; set; } = "SovereignID";

    public string Subject { get; set; } = "Invitación a SovereignID";
}
