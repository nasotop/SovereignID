using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Verifier.Domain;

namespace Verifier.Api;

internal static class VerifierRateLimitingExtensions
{
    public const string VerificationsPolicyName = "verifications";

    public static IServiceCollection AddVerifierRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var tokenLimit = configuration.GetValue("Verifier:RateLimiting:TokenLimit", 10);
        var tokensPerPeriod = configuration.GetValue("Verifier:RateLimiting:TokensPerPeriod", 1);
        var periodSeconds = configuration.GetValue("Verifier:RateLimiting:PeriodSeconds", 3);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Title = "Too many requests",
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "Rate limit exceeded for verification requests.",
                    Extensions = { ["error"] = VerifierErrorCodes.RateLimitExceeded }
                });
            };

            options.AddPolicy(VerificationsPolicyName, httpContext =>
            {
                var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
                var resolvedTokenLimit = config.GetValue("Verifier:RateLimiting:TokenLimit", tokenLimit);
                var resolvedTokensPerPeriod = config.GetValue("Verifier:RateLimiting:TokensPerPeriod", tokensPerPeriod);
                var resolvedPeriodSeconds = config.GetValue("Verifier:RateLimiting:PeriodSeconds", periodSeconds);

                return RateLimitPartition.GetTokenBucketLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = resolvedTokenLimit,
                        TokensPerPeriod = resolvedTokensPerPeriod,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(resolvedPeriodSeconds),
                        AutoReplenishment = true,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }

    public static IServiceCollection AddVerifierForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var network in configuration.GetSection("Verifier:ForwardedHeaders:KnownNetworks").Get<string[]>() ?? DefaultKnownNetworks())
            {
                if (TryParseNetwork(network, out var parsed))
                {
                    options.KnownIPNetworks.Add(parsed);
                }
            }

            foreach (var proxy in configuration.GetSection("Verifier:ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
            {
                if (System.Net.IPAddress.TryParse(proxy, out var parsed))
                {
                    options.KnownProxies.Add(parsed);
                }
            }
        });

        return services;
    }

    private static string[] DefaultKnownNetworks() =>
    [
        "10.0.0.0/8",
        "172.16.0.0/12",
        "192.168.0.0/16"
    ];

    private static bool TryParseNetwork(string cidr, out System.Net.IPNetwork network) =>
        System.Net.IPNetwork.TryParse(cidr, out network);
}
