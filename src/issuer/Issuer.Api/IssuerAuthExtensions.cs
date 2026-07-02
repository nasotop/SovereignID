using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Issuer.Api;

internal static class IssuerAuthExtensions
{
    public static IServiceCollection AddIssuerJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var signingKey = configuration["Auth:JwtSigningKey"]
            ?? throw new InvalidOperationException("Auth:JwtSigningKey is required.");
        var issuer = configuration["Auth:JwtIssuer"] ?? "sovereignid-auth";
        var audience = configuration["Auth:JwtAudience"] ?? "sovereignid-clients";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();

        return services;
    }
}
