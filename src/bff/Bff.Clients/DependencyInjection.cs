using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using AcademyApiClient = SovereignID.Bff.Clients.Academy.ApiClient;
using IssuerApiClient = SovereignID.Bff.Clients.Issuer.ApiClient;
using VerifierApiClient = SovereignID.Bff.Clients.Verifier.ApiClient;

namespace SovereignID.Bff.Clients;

public static class DependencyInjection
{
    public const string AcademyDirectHttpClientName = "AcademyDirect";
    public const string IssuerDirectHttpClientName = "IssuerDirect";
    public const string ReportsDirectHttpClientName = "ReportsDirect";

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
        services.AddTransient<AuthorizationForwardingHandler>();
        services.AddHttpClient(
                AcademyDirectHttpClientName,
                client => client.BaseAddress = new Uri(options.Academy.TrimEnd('/') + "/"))
            .AddHttpMessageHandler<AuthorizationForwardingHandler>();
        services.AddHttpClient(
                IssuerDirectHttpClientName,
                client => client.BaseAddress = new Uri(options.Issuer.TrimEnd('/') + "/"))
            .AddHttpMessageHandler<AuthorizationForwardingHandler>();
        services.AddHttpClient(
                ReportsDirectHttpClientName,
                client => client.BaseAddress = new Uri(options.Reports.TrimEnd('/') + "/"))
            .AddHttpMessageHandler<AuthorizationForwardingHandler>();

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
