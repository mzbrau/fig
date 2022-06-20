using System.Security;

namespace Fig.Client.ClientSecret;

public interface ISecretResolver
{
    SecureString ResolveSecret();
}