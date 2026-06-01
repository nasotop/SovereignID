using System.Globalization;
using System.Text.RegularExpressions;
using Auth.Application;

namespace Auth.Infrastructure;

public sealed partial class SiweMessageParser : ISiweMessageParser
{
    private const string AccountPromptSuffix = " wants you to sign in with your Ethereum account:";

    [GeneratedRegex("^0x[0-9a-fA-F]{40}$", RegexOptions.CultureInvariant)]
    private static partial Regex AddressRegex();

    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex NonceRegex();

    public SiweParseResult TryParse(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return Fail("SIWE message is empty.");
        }

        var normalized = message.Replace("\r\n", "\n", StringComparison.Ordinal);
        var lines = normalized.Split('\n');

        if (lines.Length < 10)
        {
            return Fail($"Expected at least 10 lines, found {lines.Length}.");
        }

        if (!lines[0].EndsWith(AccountPromptSuffix, StringComparison.Ordinal))
        {
            return Fail("Line 1 must end with ' wants you to sign in with your Ethereum account:'.");
        }

        var domain = lines[0][..^AccountPromptSuffix.Length];
        if (string.IsNullOrWhiteSpace(domain))
        {
            return Fail("Line 1 must include a domain.");
        }

        var address = lines[1];
        if (!AddressRegex().IsMatch(address))
        {
            return Fail("Line 2 must contain a valid Ethereum address (0x + 40 hex digits).");
        }

        if (!string.IsNullOrEmpty(lines[2]))
        {
            return Fail("Line 3 must be empty.");
        }

        var statement = lines[3];

        if (!string.IsNullOrEmpty(lines[4]))
        {
            return Fail("Line 5 must be empty.");
        }

        if (!TryReadPrefixedValue(lines[5], "URI:", out var uriValue, out var uriError))
        {
            return Fail(uriError!);
        }

        if (!Uri.TryCreate(uriValue, UriKind.Absolute, out var uri))
        {
            return Fail("Line 6 URI must be an absolute URI.");
        }

        if (!TryReadPrefixedValue(lines[6], "Version:", out var versionValue, out var versionError))
        {
            return Fail(versionError!);
        }

        if (versionValue != "1")
        {
            return Fail("Line 7 must be exactly 'Version: 1'.");
        }

        if (!TryReadPrefixedValue(lines[7], "Chain ID:", out var chainIdValue, out var chainError))
        {
            return Fail(chainError!);
        }

        if (!int.TryParse(chainIdValue, NumberStyles.None, CultureInfo.InvariantCulture, out var chainId))
        {
            return Fail("Line 8 Chain ID must be a decimal integer.");
        }

        if (!TryReadPrefixedValue(lines[8], "Nonce:", out var nonceValue, out var nonceError))
        {
            return Fail(nonceError!);
        }

        if (!NonceRegex().IsMatch(nonceValue))
        {
            return Fail("Line 9 Nonce must be exactly 32 lowercase hexadecimal characters.");
        }

        if (!TryReadPrefixedValue(lines[9], "Issued At:", out var issuedAtValue, out var issuedAtError))
        {
            return Fail(issuedAtError!);
        }

        if (!DateTimeOffset.TryParse(
                issuedAtValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var issuedAt))
        {
            return Fail("Line 10 Issued At must be a valid ISO 8601 timestamp.");
        }

        var optionalError = ParseOptionalFields(lines, startIndex: 10);
        if (optionalError is not null)
        {
            return Fail(optionalError);
        }

        return new SiweParseResult(
            new SiweMessage(
                message,
                domain,
                address,
                statement,
                uri,
                chainId,
                nonceValue,
                issuedAt),
            null);
    }

    private static string? ParseOptionalFields(string[] lines, int startIndex)
    {
        var index = startIndex;
        while (index < lines.Length)
        {
            var line = lines[index];

            if (string.IsNullOrEmpty(line))
            {
                index++;
                continue;
            }

            if (line.StartsWith("Expiration Time:", StringComparison.Ordinal)
                || line.StartsWith("Not Before:", StringComparison.Ordinal)
                || line.StartsWith("Request ID:", StringComparison.Ordinal))
            {
                index++;
                continue;
            }

            if (line == "Resources:")
            {
                index++;
                while (index < lines.Length)
                {
                    var resourceLine = lines[index];
                    if (string.IsNullOrEmpty(resourceLine))
                    {
                        index++;
                        continue;
                    }

                    if (!resourceLine.StartsWith("- ", StringComparison.Ordinal))
                    {
                        return $"Unrecognized line in Resources block: '{resourceLine}'.";
                    }

                    var resourceUri = resourceLine[2..];
                    if (!Uri.TryCreate(resourceUri, UriKind.Absolute, out _))
                    {
                        return $"Resource URI must be absolute: '{resourceUri}'.";
                    }

                    index++;
                }

                continue;
            }

            return $"Unrecognized SIWE line: '{line}'.";
        }

        return null;
    }

    private static bool TryReadPrefixedValue(
        string line,
        string prefix,
        out string value,
        out string? error)
    {
        if (!line.StartsWith(prefix, StringComparison.Ordinal))
        {
            value = string.Empty;
            error = $"Expected line to start with '{prefix.TrimEnd()}'.";
            return false;
        }

        value = line[prefix.Length..].TrimStart();
        if (string.IsNullOrEmpty(value))
        {
            error = $"Expected a value after '{prefix.TrimEnd()}'.";
            return false;
        }

        error = null;
        return true;
    }

    private static SiweParseResult Fail(string detail) => new(null, detail);
}
