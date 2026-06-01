using Auth.Application;
using Nethereum.Signer;

namespace Auth.Infrastructure;

public sealed class PersonalSignSignatureVerifier : ISignatureVerifier
{
    private readonly EthereumMessageSigner _signer = new();

    public bool TryRecoverAddress(string originalPayload, string signature, out string recoveredAddress)
    {
        recoveredAddress = string.Empty;

        try
        {
            recoveredAddress = _signer.EncodeUTF8AndEcRecover(originalPayload, signature);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
