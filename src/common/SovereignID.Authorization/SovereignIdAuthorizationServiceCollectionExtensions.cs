using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SovereignID.Authorization.Handlers;

namespace SovereignID.Authorization;

public static class SovereignIdAuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddSovereignIdAuthorizationHandlers(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IAuthorizationHandler, PlatformAdminAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, InstitutionRouteAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, HolderAuthenticatedAuthorizationHandler>();
        return services;
    }

    public static void AddSovereignIdAuthorizationPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(AuthorizationPolicies.PlatformAdmin, policy =>
            policy.Requirements.Add(new PlatformAdminRequirement()));

        options.AddPolicy(AuthorizationPolicies.PlatformOrInstitutionMember, policy =>
            policy.Requirements.Add(new InstitutionRouteRequirement("admin", "issuer")));

        options.AddPolicy(AuthorizationPolicies.InstitutionAdmin, policy =>
            policy.Requirements.Add(new InstitutionRouteRequirement("admin")));

        options.AddPolicy(AuthorizationPolicies.InstitutionIssuer, policy =>
            policy.Requirements.Add(new InstitutionRouteRequirement("admin", "issuer")));

        options.AddPolicy(AuthorizationPolicies.HolderAuthenticated, policy =>
            policy.Requirements.Add(new HolderAuthenticatedRequirement()));
    }
}
