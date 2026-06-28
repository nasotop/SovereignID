namespace Academy.Application;

public interface IInstitutionInvitationEmailSender
{
    Task SendInvitationAsync(
        string email,
        string invitationUrl,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);
}

