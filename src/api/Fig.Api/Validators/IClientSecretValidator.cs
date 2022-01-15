namespace Fig.Api.Validators;

public interface IClientSecretValidator
{
    bool IsValid(string clientSecret);
}