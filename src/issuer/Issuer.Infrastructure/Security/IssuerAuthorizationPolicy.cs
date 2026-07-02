using Issuer.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Issuer.Infrastructure.Security;

public static class IssuerAuthorizationPolicy
{
    public const string IssuerPolicyName = "IssuerAuthenticated";

    public static void Configure(AuthorizationOptions options, IHostEnvironment environment, AuthOptions authOptions)
    {
        var requireAuth = authOptions.RequireAuthentication
            || (environment.IsProduction() && authOptions.RequireAuthentication is not false);

        if (!requireAuth && environment.IsDevelopment())
        {
            options.AddPolicy(IssuerPolicyName, policy => policy.RequireAssertion(_ => true));
            return;
        }

        options.AddPolicy(IssuerPolicyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("address");
        });
    }
}
