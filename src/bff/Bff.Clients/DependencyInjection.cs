using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using AcademyApiClient = SovereignID.Bff.Clients.Academy.ApiClient;
using IdentityApiClient = SovereignID.Bff.Clients.Identity.ApiClient;
using IssuerApiClient = SovereignID.Bff.Clients.Issuer.ApiClient;
using VerifierApiClient = SovereignID.Bff.Clients.Verifier.ApiClient;

namespace SovereignID.Bff.Clients;

public static class DependencyInjection
{
    public static IServiceCollection AddBffDownstreamClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DownstreamOptions>(configuration.GetSection(DownstreamOptions.SectionName));
        services.AddHttpContextAccessor();

        var options = configuration.GetSection(DownstreamOptions.SectionName).Get<DownstreamOptions>()
            ?? new DownstreamOptions();

        RegisterClient<VerifierApiClient>(services, options.Verifier);
        RegisterClient<IssuerApiClient>(services, options.Issuer);
        RegisterClient<AcademyApiClient>(services, options.Academy);
        RegisterClient<IdentityApiClient>(services, options.Identity);

        return services;
    }

    private static void RegisterClient<TClient>(IServiceCollection services, string baseUrl)
        where TClient : class
    {
        services.AddSingleton<TClient>(sp =>
        {
            var handler = new AuthorizationForwardingHandler(sp.GetRequiredService<IHttpContextAccessor>())
            {
                InnerHandler = new HttpClientHandler(),
            };

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            };

            var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient)
            {
                BaseUrl = baseUrl.TrimEnd('/'),
            };

            return (TClient)Activator.CreateInstance(typeof(TClient), adapter)!;
        });
    }
}
