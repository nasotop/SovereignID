using Academy.Application;
using Microsoft.Extensions.Logging;

namespace Academy.Infrastructure.Email;

internal sealed class LoggingInstitutionInvitationEmailSender : IInstitutionInvitationEmailSender
{
    private readonly ILogger<LoggingInstitutionInvitationEmailSender> _logger;

    public LoggingInstitutionInvitationEmailSender(ILogger<LoggingInstitutionInvitationEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendInvitationAsync(
        string email,
        string invitationUrl,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Institution invitation queued for {Email}. Link expires at {ExpiresAt}. Url: {InvitationUrl}",
            email,
            expiresAt,
            invitationUrl);

        return Task.CompletedTask;
    }
}

