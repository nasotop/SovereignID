namespace Issuer.Application;

public sealed class IssuerOptions
{
    public const string SectionName = "Issuer";

    public int DefaultChainId { get; set; } = 11155111;

    public string CredentialRegistryAddress { get; set; } = string.Empty;

    public BlockchainOptions Blockchain { get; set; } = new();

    public AuthOptions Auth { get; set; } = new();
}

public sealed class BlockchainOptions
{
    public bool Enabled { get; set; }

    public string RpcUrl { get; set; } = "https://rpc.sepolia.org";
}

public sealed class AuthOptions
{
    public bool RequireAuthentication { get; set; }

    public string JwtSigningKey { get; set; } = string.Empty;

    public string JwtIssuer { get; set; } = "sovereignid-auth";

    public string JwtAudience { get; set; } = "sovereignid-clients";
}
