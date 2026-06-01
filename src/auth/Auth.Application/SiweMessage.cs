namespace Auth.Application;

public sealed record SiweMessage(
    string OriginalPayload,
    string Domain,
    string Address,
    string Statement,
    Uri Uri,
    int ChainId,
    string Nonce,
    DateTimeOffset IssuedAt);
