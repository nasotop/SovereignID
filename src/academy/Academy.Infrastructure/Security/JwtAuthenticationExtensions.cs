using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Academy.Infrastructure.Security;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddAcademyJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtIssuer = configuration["Auth:JwtIssuer"] ?? "sovereignid-auth";
        var jwtAudience = configuration["Auth:JwtAudience"] ?? "sovereignid-clients";
        var signingKey = ResolveSigningKey(configuration);

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

    private static string ResolveSigningKey(IConfiguration configuration)
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

        return "development-only-signing-key-32-bytes!";
    }
}
