namespace Application.Abstractions.Phone;

public interface IPhoneRecoveryCodeService
{
    IReadOnlyList<string> GeneratePlainCodes(int count = 8);
    string Hash(string code);
    bool Verify(string plainCode, string hash);
}
