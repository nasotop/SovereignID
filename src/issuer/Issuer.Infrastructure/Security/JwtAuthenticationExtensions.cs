using System.Text;
using Issuer.Application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Issuer.Infrastructure.Security;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddIssuerJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var issuerSection = configuration.GetSection(IssuerOptions.SectionName);
        var authOptions = issuerSection.GetSection(nameof(IssuerOptions.Auth)).Get<AuthOptions>() ?? new AuthOptions();
        var signingKey = ResolveSigningKey(configuration, authOptions);
        var jwtIssuer = configuration["Auth:JwtIssuer"] ?? authOptions.JwtIssuer;
        var jwtAudience = configuration["Auth:JwtAudience"] ?? authOptions.JwtAudience;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorization();
        return services;
    }

    private static string ResolveSigningKey(IConfiguration configuration, AuthOptions authOptions)
    {
        var envKey = configuration["AUTH_JWT_SIGNING_KEY"];
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            return envKey;
        }

        var rootAuthKey = configuration["Auth:JwtSigningKey"];
        if (!string.IsNullOrWhiteSpace(rootAuthKey))
        {
            return rootAuthKey;
        }

        if (!string.IsNullOrWhiteSpace(authOptions.JwtSigningKey))
        {
            return authOptions.JwtSigningKey;
        }

        return "development-only-signing-key-32-bytes!";
    }
}
