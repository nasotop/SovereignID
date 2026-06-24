namespace Auth.Infrastructure.Persistence.Composition;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    /// <summary>
    /// <see cref="PersistenceProviders.InMemory"/> o <see cref="PersistenceProviders.Postgres"/>.
    /// </summary>
    public string Provider { get; set; } = PersistenceProviders.InMemory;
}

public static class PersistenceProviders
{
    public const string InMemory = "InMemory";
    public const string Postgres = "Postgres";
}
