namespace Verifier.Domain;

public enum RevocationSource
{
    Bd,
    OnChain,
    Both
}

public static class RevocationSourceExtensions
{
    public static string ToWireValue(this RevocationSource source) => source switch
    {
        RevocationSource.Bd => "bd",
        RevocationSource.OnChain => "on_chain",
        RevocationSource.Both => "both",
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown revocation source.")
    };
}
