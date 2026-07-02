namespace Issuer.Application;

public sealed class IssuerOptions
{
    public const string SectionName = "Issuer";

    public int DefaultChainId { get; set; } = 11155111;
}
