using Auth.Infrastructure.Persistence.Generated;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure.Persistence.Composition;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddAuthPostgresPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // The connection string is resolved at runtime (not at registration) so that test hosts
        // and other late configuration sources (e.g. WebApplicationFactory) can override it.
        services.AddDbContext<SovereignIdDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection is required.");
            }

            options.UseNpgsql(connectionString);
        });

        return services;
    }
}
