using Fig.Common.Events;
using Fig.Contracts.Scheduling;
using Fig.Web.Events;
using Fig.Web.Models.Scheduling;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class SchedulingFacade : ISchedulingFacade
{
    private readonly IHttpService _httpService;

    public SchedulingFacade(IHttpService httpService, IEventDistributor eventDistributor)
    {
        _httpService = httpService;
        eventDistributor.Subscribe(EventConstants.LogoutEvent, () => DeferredChanges.Clear());
    }

    public List<DeferredChangeModel> DeferredChanges { get; } = new();

    public async Task GetAllDeferredChanges()
    {
        var result = await _httpService.Get<SchedulingChangesDataContract>("scheduling");

        if (result != null)
        {
            Console.WriteLine($"Loaded {result.Changes.Count()} scheduled changes");
            DeferredChanges.Clear();
            foreach (var item in result.Changes.OrderBy(a => a.ExecuteAtUtc))
            {
                DeferredChanges.Add(new DeferredChangeModel(item.Id,
                    item.ExecuteAtUtc,
                    item.RequestingUser,
                    item.ClientName,
                    item.Instance,
                    item.ChangeSet));
            }
        }
    }

    public async Task RescheduleChange(Guid deferredChangeId, RescheduleDeferredChangeDataContract change)
    {
        await _httpService.Put($"scheduling/{deferredChangeId}", change);
        await GetAllDeferredChanges();
    }

    public async Task DeleteDeferredChange(Guid deferredChangeId)
    {
        await _httpService.Delete($"scheduling/{deferredChangeId}");
        await GetAllDeferredChanges();
    }
}