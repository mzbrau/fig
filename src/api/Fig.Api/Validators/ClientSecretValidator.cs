namespace Fig.Api;

public class ClientSecretValidator : IClientSecretValidator
{
    public bool IsValid(string clientSecret)
    {
        return Guid.TryParse(clientSecret, out _);
    }
}