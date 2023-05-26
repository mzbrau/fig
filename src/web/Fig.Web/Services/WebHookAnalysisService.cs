using System.Text.RegularExpressions;
using Fig.Common.NetStandard.WebHook;
using Fig.Web.Facades;
using Fig.Web.Models.Setting;
using Fig.Web.Models.WebHooks;

namespace Fig.Web.Services;

public class WebHookAnalysisService : IWebHookAnalysisService
{
    private readonly ISettingClientFacade _settingClientFacade;

    public WebHookAnalysisService(ISettingClientFacade settingClientFacade)
    {
        _settingClientFacade = settingClientFacade;
    }
    
    public async Task<MatchingClientsModel> PerformAnalysis(WebHookModel webHook)
    {
        var matches = new List<MatchingClientModel>();
        
        if (!_settingClientFacade.SettingClients.Any())
            await _settingClientFacade.LoadAllClients();

        var clientRegex = new Regex(webHook.ClientNameRegex);
        Regex? settingRegex = null;
        if (webHook is { WebHookType: WebHookType.SettingChanged, SettingNameRegex: not null })
        {
            settingRegex = new Regex(webHook.SettingNameRegex);
        }
        
        foreach (var client in _settingClientFacade.SettingClients.Where(a => !a.IsGroup))
        {
            if (clientRegex.IsMatch(client.Name))
            {
                if (settingRegex is not null)
                {
                    GetSettingMatches(client, settingRegex, matches);
                }
                else
                {
                    matches.Add(new MatchingClientModel(client.Name));
                }
            }
        }

        return new MatchingClientsModel(matches);
    }

    private static void GetSettingMatches(SettingClientConfigurationModel client, Regex settingRegex, List<MatchingClientModel> matches)
    {
        foreach (var setting in client.Settings)
        {
            if (settingRegex.IsMatch(setting.Name))
            {
                matches.Add(new MatchingClientModel(client.Name, setting.Name));
            }
        }
    }
}