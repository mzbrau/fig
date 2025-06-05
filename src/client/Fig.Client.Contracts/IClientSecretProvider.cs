using System.Threading.Tasks;

namespace Fig.Client.Contracts
{
    public interface IClientSecretProvider
    {
        Task<string> GetSecret(string clientName);
    }
}