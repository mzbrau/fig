using System.Threading.Tasks;

namespace Fig.Client;

public interface IFigConfigurationProvider
{
    Task<T> Initialize<T>() where T : SettingsBase;
}