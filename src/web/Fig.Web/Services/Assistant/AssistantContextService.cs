using Fig.Contracts.Assistant;
using Fig.Web.Facades;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Services.Assistant;

public sealed class AssistantContextService : IAssistantContextService
{
    private readonly NavigationManager _navigationManager;
    private readonly IAccountService _accountService;
    private readonly ISettingClientFacade _settingClientFacade;
    private AssistantUiContextDataContract _publishedContext = new();

    public AssistantContextService(
        NavigationManager navigationManager,
        IAccountService accountService,
        ISettingClientFacade settingClientFacade)
    {
        _navigationManager = navigationManager;
        _accountService = accountService;
        _settingClientFacade = settingClientFacade;
    }

    public void Publish(AssistantUiContextDataContract context)
    {
        _publishedContext = context ?? new AssistantUiContextDataContract();
    }

    public AssistantUiContextDataContract BuildContext()
    {
        var relativeRoute = _navigationManager.ToBaseRelativePath(_navigationManager.Uri);
        var route = relativeRoute.Split(['?', '#'], 2)[0];
        var pageName = PageName(route);
        var selectedClient = _settingClientFacade.SelectedSettingClient;
        var onSettings = IsSettingsRoute(route);

        return new AssistantUiContextDataContract
        {
            // Prefer live route naming on Settings so Groups/Lookup Tables Publish cannot leave CurrentPage stale.
            CurrentPage = onSettings ? pageName : (_publishedContext.CurrentPage ?? pageName),
            Route = string.IsNullOrWhiteSpace(relativeRoute) ? "/" : $"/{relativeRoute}",
            // Always prefer the live selected client/instance from the facade.
            SelectedClientName = selectedClient?.Name ?? _publishedContext.SelectedClientName,
            SelectedInstance = selectedClient?.Instance ?? _publishedContext.SelectedInstance,
            SelectedSettingName = _publishedContext.SelectedSettingName,
            SelectedGroupName = selectedClient?.IsGroup == true
                ? selectedClient.Name
                : (onSettings ? null : _publishedContext.SelectedGroupName),
            SelectedLookupTableName = onSettings ? null : _publishedContext.SelectedLookupTableName,
            Username = _accountService.AuthenticatedUser?.Username,
            DirtySettings = CollectDirtySettings()
        };
    }

    private List<AssistantDirtySettingDataContract> CollectDirtySettings()
    {
        return _settingClientFacade.SettingClients
            .SelectMany(client => client.Settings
                .Where(setting => setting.IsDirty &&
                                  !setting.IsSecret &&
                                  setting.DataGridConfiguration?.Columns.Any(column => column.IsSecret) != true)
                .Select(setting =>
                {
                    var valueContract = setting.GetValueDataContract();
                    return new AssistantDirtySettingDataContract
                    {
                        Name = setting.Name,
                        ClientName = client.Name,
                        Instance = client.Instance,
                        ValueType = valueContract?.GetType().Name,
                        Value = valueContract?.GetValue()
                    };
                }))
            .ToList();
    }

    private static bool IsSettingsRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
            return true;

        var segment = route.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(segment) ||
               string.Equals(segment, "settings", StringComparison.OrdinalIgnoreCase);
    }

    private static string PageName(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
            return "Settings";

        return route.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() switch
        {
            "lookuptables" => "Lookup Tables",
            "settingstable" => "Settings Table",
            var segment when !string.IsNullOrWhiteSpace(segment) =>
                char.ToUpperInvariant(segment[0]) + segment[1..],
            _ => "Settings"
        };
    }
}
