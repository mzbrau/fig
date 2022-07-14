namespace Fig.Api.SettingVerification.Dynamic;

public interface ICodeHasher
{
    string GetHash(string code);

    bool IsValid(string hash, string? code);
}