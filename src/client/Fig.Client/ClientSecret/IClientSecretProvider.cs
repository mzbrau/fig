namespace Fig.Client.ClientSecret;

public interface IClientSecretProvider
{
    string GetSecret(string clientName);
}