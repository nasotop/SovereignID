using Verifier.Application;
using Verifier.Infrastructure.Persistence.Composition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Verifier.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddVerifierInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<VerifierOptions>(configuration.GetSection(VerifierOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);

        services.AddVerifierPersistence(configuration);
        services.AddScoped<VerifyCredentialUseCase>();

        return services;
    }

    public static void ValidateVerifierConfiguration(this IHost host)
    {
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required.");
        }
    }
}
