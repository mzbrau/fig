using Fig.Common.NetStandard.WebHook;
using Fig.Web.ExtensionMethods;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Models.WebHooks;

public class WebHookModel
{
    public Guid? Id { get; set; }
    
    public Guid ClientId { get; set; }
    
    public WebHookType WebHookType { get; set; }

    public string ClientNameRegex { get; set; } = ".*";

    public string? SettingNameRegex { get; set; } = ".*";

    public int MinSessions { get; set; } = 1;
    
    public bool IsInEditMode { get; set; }

    public MatchingClientsModel MatchingClients { get; set; } = new(new List<MatchingClientModel>());
    
    public bool AreMatchDetailsVisible { get; set; }

    public void Edit()
    {
        IsInEditMode = true;
    }

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientNameRegex))
            return "Setting client regex must be populated";

        if (!ClientNameRegex.IsValidRegex())
            return "Setting client regex is not valid";

        if (WebHookType == WebHookType.SettingValueChanged)
        {
            if (string.IsNullOrWhiteSpace(SettingNameRegex))
                return "Setting name regex must be populated";

            if (!SettingNameRegex.IsValidRegex())
                return "Setting name regex is not valid";
        }

        if (WebHookType == WebHookType.MinRunSessions)
        {
            if (MinSessions < 1)
                return "Min sessions must be greater than 0";
        }

        if (ClientId == Guid.Empty)
            return "Web hook must have an associated client";

        return null;
    }

    public void ShowMatchDetails()
    {
        AreMatchDetailsVisible = !AreMatchDetailsVisible;
    }
}