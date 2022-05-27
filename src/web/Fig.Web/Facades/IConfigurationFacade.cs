using Fig.Web.Models.Configuration;

namespace Fig.Web.Facades;

public interface IConfigurationFacade
{
    FigConfigurationModel ConfigurationModel { get; }

    Task LoadConfiguration();

    Task SaveConfiguration();
}