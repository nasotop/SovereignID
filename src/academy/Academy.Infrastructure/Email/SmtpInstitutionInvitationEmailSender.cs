using Academy.Application;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Academy.Infrastructure.Email;

internal sealed class SmtpInstitutionInvitationEmailSender : IInstitutionInvitationEmailSender
{
    private readonly AcademyOptions _options;
    private readonly ILogger<SmtpInstitutionInvitationEmailSender> _logger;

    public SmtpInstitutionInvitationEmailSender(
        IOptions<AcademyOptions> options,
        ILogger<SmtpInstitutionInvitationEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendInvitationAsync(
        string email,
        string invitationUrl,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        var smtp = _options.Email.Smtp;
        var message = BuildMessage(email, invitationUrl, expiresAt, smtp);

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(
                smtp.Host,
                smtp.Port,
                smtp.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(smtp.Username))
            {
                await client.AuthenticateAsync(smtp.Username, smtp.Password!, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "Institution invitation email sent to {Email}. Expires at {ExpiresAt}.",
                email,
                expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send institution invitation email to {Email}.", email);
            throw;
        }
    }

    private static MimeMessage BuildMessage(
        string email,
        string invitationUrl,
        DateTimeOffset expiresAt,
        SmtpOptions smtp)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.FromDisplayName, smtp.FromAddress));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = smtp.Subject;

        var expiresText = expiresAt.ToString("u");
        var plainText = $"""
            Has sido invitado a unirte a una institución en SovereignID.

            Acepta la invitación usando el siguiente enlace:
            {invitationUrl}

            Este enlace expira el {expiresText} (UTC).
            """;

        var html = $"""
            <p>Has sido invitado a unirte a una institución en <strong>SovereignID</strong>.</p>
            <p><a href="{invitationUrl}">Aceptar invitación</a></p>
            <p>Este enlace expira el {expiresText} (UTC).</p>
            """;

        message.Body = new BodyBuilder
        {
            TextBody = plainText,
            HtmlBody = html
        }.ToMessageBody();

        return message;
    }
}
