using Academy.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Academy.Infrastructure.Email;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddAcademyEmail(this IServiceCollection services, IConfiguration configuration)
    {
        if (UsesSmtpEmail(configuration))
        {
            services.AddSingleton<IInstitutionInvitationEmailSender, SmtpInstitutionInvitationEmailSender>();
        }
        else
        {
            services.AddSingleton<IInstitutionInvitationEmailSender, LoggingInstitutionInvitationEmailSender>();
        }

        return services;
    }

    public static bool UsesSmtpEmail(IConfiguration configuration) =>
        string.Equals(
            configuration.GetSection(AcademyOptions.SectionName).Get<AcademyOptions>()?.Email.Provider
            ?? EmailProviders.Logging,
            EmailProviders.Smtp,
            StringComparison.OrdinalIgnoreCase);

    public static void ValidateAcademyEmailConfiguration(IConfiguration configuration)
    {
        if (!UsesSmtpEmail(configuration))
        {
            return;
        }

        var smtp = configuration
            .GetSection($"{AcademyOptions.SectionName}:Email:Smtp")
            .Get<SmtpOptions>() ?? new SmtpOptions();

        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            throw new InvalidOperationException(
                "Academy:Email:Smtp:Host is required when Academy:Email:Provider is Smtp.");
        }

        if (string.IsNullOrWhiteSpace(smtp.FromAddress) || !smtp.FromAddress.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Academy:Email:Smtp:FromAddress must be a valid email when Academy:Email:Provider is Smtp.");
        }

        if (smtp.Port is < 1 or > 65535)
        {
            throw new InvalidOperationException(
                "Academy:Email:Smtp:Port must be between 1 and 65535 when Academy:Email:Provider is Smtp.");
        }

        if (!string.IsNullOrWhiteSpace(smtp.Username) && string.IsNullOrWhiteSpace(smtp.Password))
        {
            throw new InvalidOperationException(
                "Academy:Email:Smtp:Password is required when Academy:Email:Smtp:Username is set.");
        }
    }
}
