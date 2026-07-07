namespace Verifier.Application;

public sealed class VerifierOptions
{
    public const string SectionName = "Verifier";

    public EvidenceVerificationOptions Evidence { get; set; } = new();
}
