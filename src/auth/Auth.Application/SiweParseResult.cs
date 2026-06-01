namespace Auth.Application;

public sealed record SiweParseResult(SiweMessage? Message, string? ErrorDetail)
{
    public bool IsSuccess => Message is not null;
}
