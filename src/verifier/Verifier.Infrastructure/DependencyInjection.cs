using Verifier.Application;
using Verifier.Infrastructure.Evidence;
using Verifier.Infrastructure.Persistence.Composition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Verifier.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddVerifierInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<VerifierOptions>(configuration.GetSection(VerifierOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);

        services.AddVerifierPersistence(configuration);
        services.AddVerifierEvidenceVerification(configuration);
        services.AddScoped<VerifyCredentialUseCase>();

        return services;
    }

    public static void ValidateVerifierConfiguration(this IHost host)
    {
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required.");
        }
    }
}

internal static class EvidenceServiceCollectionExtensions
{
    public static IServiceCollection AddVerifierEvidenceVerification(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var evidence = configuration.GetSection($"{VerifierOptions.SectionName}:Evidence")
            .Get<EvidenceVerificationOptions>() ?? new EvidenceVerificationOptions();

        services.AddHttpClient<RpcCredentialRegistryReader>();
        services.AddHttpClient<HttpIpfsContentReader>();

        services.AddSingleton<NullCredentialEvidenceVerifier>();
        services.AddScoped<CredentialEvidenceVerifier>(sp => new CredentialEvidenceVerifier(
            sp.GetRequiredService<IOptions<VerifierOptions>>(),
            sp.GetService<ICredentialRegistryReader>(),
            sp.GetService<IIpfsContentReader>(),
            sp.GetService<IEip712IssuanceSignatureVerifier>()));
        services.AddScoped<ICredentialEvidenceVerifier, ConfigurableCredentialEvidenceVerifier>();

        if (evidence.OnChainCheckEnabled)
        {
            services.AddScoped<ICredentialRegistryReader, RpcCredentialRegistryReader>();
        }

        if (evidence.IpfsCheckEnabled)
        {
            services.AddScoped<IIpfsContentReader, HttpIpfsContentReader>();
        }

        if (evidence.SignatureCheckEnabled)
        {
            services.AddScoped<IEip712IssuanceSignatureVerifier, NethereumEip712IssuanceSignatureVerifier>();
        }

        return services;
    }
}
