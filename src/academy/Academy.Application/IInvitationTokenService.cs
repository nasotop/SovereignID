namespace Academy.Application;

public interface IInvitationTokenService
{
    string CreateToken();

    string HashToken(string token);
}

