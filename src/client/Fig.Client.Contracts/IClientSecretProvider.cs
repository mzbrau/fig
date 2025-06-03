using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Fig.Client.Contracts
{
    public interface IClientSecretProvider
    {
        string Name { get; }
        
        bool IsEnabled { get; }

        void AddLogger(ILoggerFactory logger);
        
        Task<string> GetSecret(string clientName);
    }
}