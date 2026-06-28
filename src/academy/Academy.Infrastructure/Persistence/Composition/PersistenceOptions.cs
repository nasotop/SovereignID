namespace Academy.Infrastructure.Persistence.Composition;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public string Provider { get; set; } = PersistenceProviders.InMemory;
}

public static class PersistenceProviders
{
    public const string InMemory = "InMemory";
    public const string Postgres = "Postgres";
}
