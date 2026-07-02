using Issuer.Infrastructure.Persistence.Composition;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Issuer.IntegrationTests;

public sealed class IssuerWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{PersistenceOptions.SectionName}:Provider"] = PersistenceProviders.InMemory,
                ["Auth:JwtIssuer"] = JwtTestHelper.TestIssuer,
                ["Auth:JwtAudience"] = JwtTestHelper.TestAudience,
                ["Auth:JwtSigningKey"] = JwtTestHelper.TestSigningKey
            });
        });
    }
}
