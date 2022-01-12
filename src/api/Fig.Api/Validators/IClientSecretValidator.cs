namespace Fig.Api;

public interface IClientSecretValidator
{
    bool IsValid(string clientSecret);
}