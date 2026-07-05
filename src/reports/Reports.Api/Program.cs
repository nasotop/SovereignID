using Reports.Api.OpenApi;
using Reports.Infrastructure;
using Reports.Infrastructure.Metrics;

if (TryRunSnapshotCli(args, out var exitCode))
{
    Environment.Exit(exitCode);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<Reports.Api.ReportsFailureExceptionFilter>();
});
builder.Services.AddProblemDetails();
builder.Services.AddReportsOpenApiDocumentation();
builder.Services.AddReportsInfrastructure(builder.Configuration);

var app = builder.Build();

app.ValidateReportsConfiguration();

if (app.Environment.IsDevelopment())
{
    app.MapReportsOpenApiDocumentation();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static bool TryRunSnapshotCli(string[] args, out int exitCode)
{
    exitCode = 0;
    if (args.Length == 0 || !string.Equals(args[0], "snapshot", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    var from = ParseDateArg(args, "--from");
    var to = ParseDateArg(args, "--to");
    if (from is null || to is null)
    {
        Console.Error.WriteLine("Usage: dotnet run --project Reports.Api -- snapshot --from YYYY-MM-DD --to YYYY-MM-DD");
        exitCode = 1;
        return true;
    }

    if (to < from)
    {
        Console.Error.WriteLine("--to must be on or after --from.");
        exitCode = 1;
        return true;
    }

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddReportsInfrastructure(builder.Configuration);
    using var host = builder.Build();
    host.ValidateReportsConfiguration();
    using var scope = host.Services.CreateScope();
    var snapshotService = scope.ServiceProvider.GetRequiredService<IMetricsSnapshotService>();
    snapshotService.UpsertRangeAsync(from.Value, to.Value).GetAwaiter().GetResult();
    Console.WriteLine($"Snapshot backfill completed for {from:yyyy-MM-dd}..{to:yyyy-MM-dd}.");
    return true;
}

static DateOnly? ParseDateArg(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)
            && DateOnly.TryParse(args[i + 1], out var date))
        {
            return date;
        }
    }

    return null;
}

public partial class Program;
