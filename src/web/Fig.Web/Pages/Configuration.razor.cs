using Fig.Web.Converters;
using Fig.Web.Facades;
using Fig.Web.Models.Configuration;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Pages
{
    public partial class Configuration
    {
        [Inject]
        private IConfigurationFacade ConfigurationFacade { get; set; } = null!;

        private FigConfigurationModel ConfigurationModel => ConfigurationFacade.ConfigurationModel;

        protected override async Task OnInitializedAsync()
        {
            await ConfigurationFacade.LoadConfiguration();
            await base.OnInitializedAsync();
        }

        private void OnConfigurationValueChanged()
        {
            ConfigurationFacade.SaveConfiguration();
        }
    }
}
