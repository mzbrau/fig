namespace Fig.Api.Validators;

public class ClientSecretValidator : IClientSecretValidator
{
    public bool IsValid(string clientSecret)
    {
        return clientSecret.Length >= 32;
    }
}