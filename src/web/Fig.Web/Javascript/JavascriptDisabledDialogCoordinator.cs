using Fig.Contracts.Authentication;
using Fig.Web.Facades;
using Fig.Web.Services;

namespace Fig.Web.Javascript;

public class JavascriptDisabledDialogCoordinator : IJavascriptDisabledDialogCoordinator
{
    private readonly IAccountService _accountService;
    private readonly IConfigurationFacade _configurationFacade;
    private readonly ILocalStorageService _localStorageService;

    public JavascriptDisabledDialogCoordinator(
        IAccountService accountService,
        IConfigurationFacade configurationFacade,
        ILocalStorageService localStorageService)
    {
        _accountService = accountService;
        _configurationFacade = configurationFacade;
        _localStorageService = localStorageService;
    }

    public async Task<bool> ShouldAutoOpen()
    {
        if (_accountService.AuthenticatedUser?.Role != Role.Administrator)
            return false;

        if (!_configurationFacade.WebFeaturesLoaded)
            await _configurationFacade.LoadWebFeatures();

        if (!_configurationFacade.WebFeaturesLoaded || _configurationFacade.AllowDisplayScripts)
            return false;

        var suppressed = await _localStorageService.GetItem<bool?>(JavascriptDisabledDialogConstants.SuppressLocalStorageKey);
        return suppressed != true;
    }

    public async Task SuppressPermanently()
    {
        await _localStorageService.SetItem(JavascriptDisabledDialogConstants.SuppressLocalStorageKey, true);
    }
}
