using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.Model;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Verifier.UnitTests;

/// <summary>
/// Regression: Nethereum <c>Parameter</c> ctor is (type, name, order).
/// Swapping type/name throws <c>ArgumentException: Unknown type: contentHash</c> at decode time.
/// </summary>
public sealed class RpcCredentialRegistryDecodeTests
{
    [Fact]
    public void DecodeGetCredential_UsesAbiTypeThenName()
    {
        // Real Sepolia eth_call result for getCredential (truncated fixture shape).
        const string hex =
            "0x" +
            "a7a61502fda4a86c1ac79933abaebfebab10a32d6fa2a8669f333685cfe7fadd" +
            "00000000000000000000000000000000000000000000000000000000000000c0" +
            "0000000000000000000000000000000011111111111111111111111111111111" +
            "000000000000000000000000f6461f392288b5732a7703e8b83f64cab134eada" +
            "000000000000000000000000f6461f392288b5732a7703e8b83f64cab134eada" +
            "0000000000000000000000000000000000000000000000000000000000000000" +
            "000000000000000000000000000000000000000000000000000000000000002e" +
            "516d5671586f41774c447769764d6370484e776b61733163396a6773314c6f53" +
            "525338616a38385944727966696f000000000000000000000000000000000000";

        var outputs = new[]
        {
            new Parameter("bytes32", "contentHash", 1),
            new Parameter("string", "ipfsCid", 2),
            new Parameter("bytes32", "institutionId", 3),
            new Parameter("address", "issuer", 4),
            new Parameter("address", "subject", 5),
            new Parameter("bool", "revoked", 6)
        };

        var decoded = new ParameterDecoder().DecodeDefaultData(hex, outputs);

        Assert.Equal(
            "a7a61502fda4a86c1ac79933abaebfebab10a32d6fa2a8669f333685cfe7fadd",
            ((byte[])decoded[0].Result).ToHex());
        Assert.Equal("QmVqXoAwLDwivMcpHNwkas1c9jgs1LoSRS8aj88YDryfio", (string)decoded[1].Result);
        Assert.False((bool)decoded[5].Result);
    }
}
