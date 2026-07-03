using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SovereignID.Authorization;

namespace Academy.Infrastructure.Security;

public static class AcademyAuthorizationPolicy
{
    public static void Configure(AuthorizationOptions options)
    {
        options.AddSovereignIdAuthorizationPolicies();
    }
}

public static class AcademyAuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddAcademyAuthorization(this IServiceCollection services)
    {
        services.AddSovereignIdAuthorizationHandlers();
        services.AddSingleton<IConfigureOptions<AuthorizationOptions>>(_ =>
            new ConfigureNamedOptions<AuthorizationOptions>(null, AcademyAuthorizationPolicy.Configure));
        return services;
    }
}
