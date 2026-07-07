namespace Verifier.Application;

public sealed class EvidenceVerificationOptions
{
    public bool OnChainCheckEnabled { get; set; }

    public bool IpfsCheckEnabled { get; set; }

    public bool SignatureCheckEnabled { get; set; }

    public string RpcUrl { get; set; } = "https://rpc.sepolia.org";

    public string RegistryAddress { get; set; } = string.Empty;

    public int ChainId { get; set; } = 11155111;

    public int IpfsTimeoutSeconds { get; set; } = 8;
}
