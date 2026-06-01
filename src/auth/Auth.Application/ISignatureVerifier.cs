namespace Auth.Application;

public interface ISignatureVerifier
{
    bool TryRecoverAddress(string originalPayload, string signature, out string recoveredAddress);
}
