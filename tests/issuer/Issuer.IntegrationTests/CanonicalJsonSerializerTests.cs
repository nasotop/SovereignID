using System.Security.Cryptography;
using System.Text;
using Issuer.Application.ContentAnchor;

namespace Issuer.IntegrationTests;

public sealed class CanonicalJsonSerializerTests
{
    [Fact]
    public void Canonicalize_SortsObjectKeys_PerRfc8785()
    {
        // RFC 8785 §3.2.3 example: object members sorted by UTF-16 code units
        const string input = """{"b":2,"a":1}""";
        const string expected = """{"a":1,"b":2}""";

        Assert.True(CanonicalJsonSerializer.TryCanonicalize(input, out var bytes, out var error), error);
        Assert.Equal(expected, Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Canonicalize_PreservesArrayOrder_AndIsDeterministic()
    {
        const string input = """{"z":[3,1,2],"a":{"y":2,"x":1}}""";
        const string expected = """{"a":{"x":1,"y":2},"z":[3,1,2]}""";

        Assert.True(CanonicalJsonSerializer.TryCanonicalize(input, out var first, out var error1), error1);
        Assert.True(CanonicalJsonSerializer.TryCanonicalize(input, out var second, out var error2), error2);

        Assert.Equal(expected, Encoding.UTF8.GetString(first));
        Assert.Equal(first, second);
        Assert.Equal(ContentHashComputer.ComputeSha256Hex(first), ContentHashComputer.ComputeSha256Hex(second));
    }

    [Fact]
    public void Canonicalize_RejectsNonObjectOrArrayTopLevel()
    {
        Assert.False(CanonicalJsonSerializer.TryCanonicalize("\"just-a-string\"", out _, out var error));
        Assert.Contains("object or array", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContentHash_UsesSha256HexWith0xPrefix()
    {
        var bytes = Encoding.UTF8.GetBytes("""{"a":1}""");
        var hash = ContentHashComputer.ComputeSha256Hex(bytes);
        var expected = $"0x{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";

        Assert.Equal(expected, hash);
        Assert.StartsWith("0x", hash);
        Assert.Equal(66, hash.Length);
    }
}
