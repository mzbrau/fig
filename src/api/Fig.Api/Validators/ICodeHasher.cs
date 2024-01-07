namespace Fig.Api.Validators;

public interface ICodeHasher
{
    string GetHash(string code);

    bool IsValid(string hash, string? code);
}