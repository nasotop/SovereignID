namespace Issuer.Application;

public interface IIssuerRequestContext
{
    bool IsAuthenticated { get; }

    string? WalletAddress { get; }

    string? SubjectDid { get; }
}
