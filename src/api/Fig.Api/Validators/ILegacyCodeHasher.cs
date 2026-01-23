namespace Fig.Api.Validators;

public interface ILegacyCodeHasher
{
    string GetHash(string code);

    bool IsValid(string hash, string? code);
}