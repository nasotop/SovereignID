using Microsoft.Extensions.Options;
using Verifier.Application;
using Verifier.Domain;
using Verifier.Infrastructure.Evidence;

namespace Verifier.UnitTests;

public sealed class CredentialEvidenceVerifierTests
{
    private static readonly Guid CredentialId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid InstitutionId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static CredentialEvidence CreateEvidence(string? signature = null) =>
        new(
            CredentialId,
            InstitutionId,
            "0x" + new string('b', 64),
            "bafytest",
            "https://ipfs.example/cid",
            "0xIssuer00000000000000000000000000000001",
            "0xSubject00000000000000000000000000000002",
            signature ?? "0x" + new string('a', 130),
            11155111);

    [Fact]
    public async Task OnChainExists_IsNull_WhenCheckDisabled()
    {
        var verifier = CreateVerifier(new EvidenceVerificationOptions());
        var result = await verifier.VerifyAsync(CreateEvidence(), CancellationToken.None);
        Assert.Null(result.OnChainExists);
    }

    [Fact]
    public async Task OnChainExists_IsTrue_WhenCoherentRecord()
    {
        var evidence = CreateEvidence();
        var registry = new InMemoryCredentialRegistryReader();
        registry.SetScenario(CredentialId, new RegistryReadResult(
            RegistryReadOutcome.Found,
            InMemoryCredentialRegistryReader.CreateCoherentData(evidence)));

        var verifier = CreateVerifier(
            new EvidenceVerificationOptions { OnChainCheckEnabled = true },
            registry: registry);

        var result = await verifier.VerifyAsync(evidence, CancellationToken.None);
        Assert.True(result.OnChainExists);
    }

    [Fact]
    public async Task OnChainExists_IsFalse_WhenIncoherentRecord()
    {
        var evidence = CreateEvidence();
        var registry = new InMemoryCredentialRegistryReader();
        registry.SetScenario(CredentialId, new RegistryReadResult(
            RegistryReadOutcome.Found,
            InMemoryCredentialRegistryReader.CreateIncoherentData(evidence)));

        var verifier = CreateVerifier(
            new EvidenceVerificationOptions { OnChainCheckEnabled = true },
            registry: registry);

        var result = await verifier.VerifyAsync(evidence, CancellationToken.None);
        Assert.False(result.OnChainExists);
    }

    [Fact]
    public async Task RevocationSource_OnChain_WhenOnlyOnChainRevoked()
    {
        var evidence = CreateEvidence();
        var registry = new InMemoryCredentialRegistryReader();
        registry.SetScenario(CredentialId, new RegistryReadResult(
            RegistryReadOutcome.Found,
            InMemoryCredentialRegistryReader.CreateCoherentData(evidence, revoked: true)));

        var verifier = CreateVerifier(
            new EvidenceVerificationOptions { OnChainCheckEnabled = true },
            registry: registry);

        var result = await verifier.VerifyAsync(evidence, CancellationToken.None);
        Assert.True(result.OnChainRevoked);
    }

    [Fact]
    public async Task HashMatches_UsesInMemoryReader()
    {
        var evidence = CreateEvidence();
        var ipfs = new InMemoryIpfsContentReader();
        ipfs.SetResponse(evidence.IpfsGatewayUrl, new IpfsReadResult(true));

        var verifier = CreateVerifier(
            new EvidenceVerificationOptions { IpfsCheckEnabled = true },
            ipfs: ipfs);

        var result = await verifier.VerifyAsync(evidence, CancellationToken.None);
        Assert.True(result.HashMatches);
    }

    [Theory]
    [InlineData(true, "on_chain")]
    [InlineData(false, "on_chain")]
    public async Task SignatureValid_OnChainSource_WhenOnChainExists(bool matches, string expectedSource)
    {
        var evidence = CreateEvidence();
        var issuer = evidence.IssuerWalletAddress!.ToLowerInvariant();
        var registry = new InMemoryCredentialRegistryReader();
        registry.SetScenario(CredentialId, new RegistryReadResult(
            RegistryReadOutcome.Found,
            InMemoryCredentialRegistryReader.CreateCoherentData(evidence)));

        var eip712 = new InMemoryEip712Verifier();
        eip712.Configure(matches ? issuer : "0xdead00000000000000000000000000000001");

        var verifier = CreateVerifier(
            new EvidenceVerificationOptions
            {
                OnChainCheckEnabled = true,
                SignatureCheckEnabled = true,
                RegistryAddress = "0x28b9137739fff83fEDC1AFEB1948cAA90EC0bD93"
            },
            registry: registry,
            eip712: eip712);

        var result = await verifier.VerifyAsync(evidence, CancellationToken.None);
        Assert.Equal(matches, result.SignatureValid);
        Assert.Equal(expectedSource, result.ValidationSource);
    }

    [Fact]
    public async Task SignatureValid_FallbackInconclusive_WhenMatchesBdOnly()
    {
        var evidence = CreateEvidence();
        var eip712 = new InMemoryEip712Verifier();
        eip712.Configure(evidence.IssuerWalletAddress!);

        var verifier = CreateVerifier(
            new EvidenceVerificationOptions { SignatureCheckEnabled = true },
            eip712: eip712);

        var result = await verifier.VerifyAsync(evidence, CancellationToken.None);
        Assert.Null(result.SignatureValid);
        Assert.Equal("bd_fallback_inconclusive", result.ValidationSource);
    }

    [Fact]
    public async Task SignatureValid_FallbackRejected_WhenMatchesNeither()
    {
        var evidence = CreateEvidence();
        var eip712 = new InMemoryEip712Verifier();
        eip712.Configure("0xdead00000000000000000000000000000001");

        var verifier = CreateVerifier(
            new EvidenceVerificationOptions { SignatureCheckEnabled = true },
            eip712: eip712);

        var result = await verifier.VerifyAsync(evidence, CancellationToken.None);
        Assert.False(result.SignatureValid);
        Assert.Equal("bd_fallback_rejected", result.ValidationSource);
    }

    [Fact]
    public async Task SignatureValid_NotEvaluated_WhenNoBdWallet()
    {
        var evidence = CreateEvidence() with { IssuerWalletAddress = null };
        var eip712 = new InMemoryEip712Verifier();
        eip712.Configure("0xdead00000000000000000000000000000001");

        var verifier = CreateVerifier(
            new EvidenceVerificationOptions { SignatureCheckEnabled = true },
            eip712: eip712);

        var result = await verifier.VerifyAsync(evidence, CancellationToken.None);
        Assert.Null(result.SignatureValid);
        Assert.Equal("not_evaluated", result.ValidationSource);
    }

    [Fact]
    public async Task SignatureValid_IsFalse_WhenSignatureMissing()
    {
        var verifier = CreateVerifier(
            new EvidenceVerificationOptions { SignatureCheckEnabled = true },
            eip712: new InMemoryEip712Verifier());
        var result = await verifier.VerifyAsync(CreateEvidence() with { Eip712Signature = null }, CancellationToken.None);
        Assert.False(result.SignatureValid);
    }

    private static CredentialEvidenceVerifier CreateVerifier(
        EvidenceVerificationOptions options,
        ICredentialRegistryReader? registry = null,
        IIpfsContentReader? ipfs = null,
        IEip712IssuanceSignatureVerifier? eip712 = null) =>
        new(
            Options.Create(new VerifierOptions { Evidence = options }),
            options.OnChainCheckEnabled ? registry : null,
            options.IpfsCheckEnabled ? ipfs : null,
            options.SignatureCheckEnabled ? eip712 : null);
}
