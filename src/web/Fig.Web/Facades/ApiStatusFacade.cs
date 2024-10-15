using Fig.Common.Events;
using Fig.Contracts.Status;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.Models.Api;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class ApiStatusFacade : IApiStatusFacade
{
    private readonly IApiStatusConverter _apiStatusConverter;
    private readonly IHttpService _httpService;

    public ApiStatusFacade(IHttpService httpService, IApiStatusConverter apiStatusConverter, IEventDistributor eventDistributor)
    {
        _httpService = httpService;
        _apiStatusConverter = apiStatusConverter;
        eventDistributor.Subscribe(EventConstants.LogoutEvent, () =>
        {
            ApiStatusModels.Clear();
        });
    }

    public List<ApiStatusModel> ApiStatusModels { get; } = new();

    public async Task Refresh()
    {
        var result = await _httpService.Get<List<ApiStatusDataContract>>("apistatus");

        if (result == null)
            return;

        ApiStatusModels.Clear();
        var apis = result.Select(a => _apiStatusConverter.Convert(a));

        foreach (var session in apis.OrderBy(a => a.Hostname).ThenBy(a => a.LastSeen))
            ApiStatusModels.Add(session);

        Console.WriteLine($"Loaded {ApiStatusModels.Count} running apis");
    }
}