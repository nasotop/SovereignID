namespace Auth.Application;

public interface ISiweMessageParser
{
    SiweParseResult TryParse(string message);
}
