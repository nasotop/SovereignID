using Issuer.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SovereignID.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Issuer.Infrastructure.Security;

public static class IssuerAuthorizationPolicy
{
    public const string IssuerPolicyName = AuthorizationPolicies.InstitutionIssuer;

    public static void Configure(
        AuthorizationOptions options,
        IHostEnvironment environment,
        AuthOptions authOptions)
    {
        var requireAuth = authOptions.RequireAuthentication
            || (environment.IsProduction() && authOptions.RequireAuthentication is not false);

        if (!requireAuth && environment.IsDevelopment())
        {
            options.AddPolicy(AuthorizationPolicies.PlatformAdmin, policy => policy.RequireAssertion(_ => true));
            options.AddPolicy(AuthorizationPolicies.PlatformOrInstitutionMember, policy => policy.RequireAssertion(_ => true));
            options.AddPolicy(AuthorizationPolicies.InstitutionAdmin, policy => policy.RequireAssertion(_ => true));
            options.AddPolicy(AuthorizationPolicies.InstitutionIssuer, policy => policy.RequireAssertion(_ => true));
            options.AddPolicy(AuthorizationPolicies.HolderAuthenticated, policy => policy.RequireAssertion(_ => true));
            return;
        }

        options.AddSovereignIdAuthorizationPolicies();
    }
}

public static class IssuerAuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddIssuerAuthorizationHandlers(this IServiceCollection services)
    {
        services.AddSovereignIdAuthorizationHandlers();
        return services;
    }
}
