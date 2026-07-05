using Reports.Infrastructure.Persistence.Composition;
using Reports.Infrastructure.Persistence.Stores;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Reports.IntegrationTests;

public sealed class ReportsWebApplicationFactory : WebApplicationFactory<Program>
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
