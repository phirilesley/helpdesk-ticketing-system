namespace HelpDeskSystem.Application.Interfaces;

public interface IMfaService
{
    string GenerateSharedSecret();
    bool VerifyCode(string sharedSecret, string code, DateTime? nowUtc = null);
}
