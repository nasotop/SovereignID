namespace SovereignID.Bff.Clients;

public sealed class DownstreamOptions
{
    public const string SectionName = "Downstream";

    public string Verifier { get; set; } = "http://localhost:5196";

    public string Issuer { get; set; } = "http://localhost:5197";

    public string Academy { get; set; } = "http://localhost:5195";

    public string Identity { get; set; } = "http://localhost:5198";
}
