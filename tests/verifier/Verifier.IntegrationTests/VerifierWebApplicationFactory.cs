using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Verifier.IntegrationTests;

public sealed class VerifierWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public ControllableTimeProvider TimeProvider { get; } = new();

    public string ConnectionString => _postgres.GetConnectionString();

    public string? LastError { get; private set; }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using (var connection = new NpgsqlConnection(_postgres.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(CanonicalSchema.Read(), connection);
            await command.ExecuteNonQueryAsync();
        }

        await SeedData.SeedAsync(_postgres.GetConnectionString());

        TimeProvider.SetUtcNow(SeedData.Now);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString()
            });
        });

        builder.ConfigureLogging(logging =>
        {
            logging.AddProvider(new CapturingLoggerProvider(message => LastError = message));
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TimeProvider);
        });
    }
}

internal sealed class CapturingLoggerProvider(Action<string> onError) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new CapturingLogger(onError);

    public void Dispose() { }

    private sealed class CapturingLogger(Action<string> onError) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                onError($"{formatter(state, exception)} | {exception}");
            }
        }
    }
}
