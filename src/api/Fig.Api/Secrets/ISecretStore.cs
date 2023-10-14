using Fig.Contracts.Configuration;

namespace Fig.Api.Secrets;

public interface ISecretStore
{
    Task<SecretStoreTestResultDataContract> PerformTest();

    Task PersistSecrets(List<KeyValuePair<string, string>> secrets);

    Task<List<KeyValuePair<string, string>>> GetSecrets(List<string> keys);
}