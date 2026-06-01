using System.Text;
using Auth.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.PostConfigure<AuthOptions>(options =>
        {
            var envKey = configuration["AUTH_JWT_SIGNING_KEY"];
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                options.JwtSigningKey = envKey;
            }
        });

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IChallengeStore, InMemoryChallengeStore>();
        services.AddSingleton<ISiweMessageParser, SiweMessageParser>();
        services.AddSingleton<ISignatureVerifier, PersonalSignSignatureVerifier>();
        services.AddSingleton<IJwtTokenIssuer, JwtBearerTokenIssuer>();
        services.AddScoped<IssueNonceUseCase>();
        services.AddScoped<VerifySiweUseCase>();

        return services;
    }

    public static void ValidateAuthConfiguration(this IHost host)
    {
        if (host.Services.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            return;
        }

        var options = host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthOptions>>().Value;
        if (string.IsNullOrWhiteSpace(options.JwtSigningKey)
            || Encoding.UTF8.GetByteCount(options.JwtSigningKey) < 32)
        {
            throw new InvalidOperationException(
                "AUTH_JWT_SIGNING_KEY must be configured with at least 32 UTF-8 bytes outside Development.");
        }
    }
}
