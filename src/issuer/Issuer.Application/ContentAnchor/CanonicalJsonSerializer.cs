using System.Text;
using System.Text.Json;
using Corvus.Text.Json.Canonicalization;
using CorvusJson = Corvus.Text.Json;

namespace Issuer.Application.ContentAnchor;

public static class CanonicalJsonSerializer
{
    public static bool TryCanonicalize(JsonElement document, out byte[] canonicalBytes, out string? error)
    {
        canonicalBytes = [];
        error = null;

        if (document.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
        {
            error = "document must be a JSON object or array.";
            return false;
        }

        try
        {
            var raw = document.GetRawText();
            using var parsed = CorvusJson.ParsedJsonDocument<CorvusJson.JsonElement>.Parse(raw);
            canonicalBytes = JsonCanonicalizer.Canonicalize(parsed.RootElement);
            return true;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryCanonicalize(string json, out byte[] canonicalBytes, out string? error)
    {
        canonicalBytes = [];
        error = null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return TryCanonicalize(doc.RootElement, out canonicalBytes, out error);
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static string ToUtf8String(ReadOnlySpan<byte> canonicalBytes) =>
        Encoding.UTF8.GetString(canonicalBytes);
}
