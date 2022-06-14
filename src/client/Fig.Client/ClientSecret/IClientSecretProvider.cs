using System.Security;

namespace Fig.Client.ClientSecret;

public interface IClientSecretProvider
{
    SecureString GetSecret(string clientName);
}