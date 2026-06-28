using Academy.Application;
using Academy.Infrastructure.Email;
using Academy.Infrastructure.Persistence.Composition;
using Academy.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Academy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAcademyInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AcademyOptions>(configuration.GetSection(AcademyOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IInvitationTokenService, Sha256InvitationTokenService>();
        services.AddSingleton<IInstitutionInvitationEmailSender, LoggingInstitutionInvitationEmailSender>();
        services.AddScoped<AcademyService>();
        services.AddAcademyPersistence(configuration);

        return services;
    }

    public static void ValidateAcademyConfiguration(this IHost host)
    {
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        if (PersistenceServiceCollectionExtensions.UsesPostgresPersistence(configuration)
            && string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required when Persistence:Provider is Postgres.");
        }
    }
}

