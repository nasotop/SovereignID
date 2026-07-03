using Issuer.Application;
using Issuer.Infrastructure.Blockchain;
using Issuer.Infrastructure.Persistence.Composition;
using Issuer.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Issuer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIssuerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IssuerOptions>(configuration.GetSection(IssuerOptions.SectionName));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IssuerService>();
        services.AddScoped<ListHolderCredentialsUseCase>();
        services.AddScoped<GetHolderCredentialUseCase>();
        services.AddIssuerPersistence(configuration);

        services.AddHttpClient<RpcBlockchainAnchorVerifier>();
        services.AddSingleton<NullBlockchainAnchorVerifier>();
        services.AddScoped<IBlockchainAnchorVerifier, ConfigurableBlockchainAnchorVerifier>();

        services.AddIssuerJwtAuthentication(configuration);
        services.AddIssuerAuthorizationHandlers();
        services.AddSingleton<IConfigureOptions<AuthorizationOptions>>(sp =>
        {
            var environment = sp.GetRequiredService<IHostEnvironment>();
            var options = sp.GetRequiredService<IOptions<IssuerOptions>>().Value.Auth;
            return new ConfigureNamedOptions<AuthorizationOptions>(null, authOptions =>
                IssuerAuthorizationPolicy.Configure(authOptions, environment, options));
        });

        return services;
    }

    public static void ValidateIssuerConfiguration(this IHost host)
    {
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        if (Persistence.Composition.PersistenceServiceCollectionExtensions.UsesPostgresPersistence(configuration)
            && string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required when Persistence:Provider is Postgres.");
        }

        var jwtSigningKey = configuration["Auth:JwtSigningKey"];
        if (string.IsNullOrWhiteSpace(jwtSigningKey) || jwtSigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Auth:JwtSigningKey must be configured with at least 32 UTF-8 bytes.");
        }
    }
}
