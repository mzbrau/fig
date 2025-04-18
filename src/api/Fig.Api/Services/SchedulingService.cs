using Fig.Api.Datalayer.Repositories;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Api.Utils;
using Fig.Contracts.Authentication;
using Fig.Contracts.Scheduling;
using Fig.Contracts.Settings;

namespace Fig.Api.Services;

public class SchedulingService : ISchedulingService
{
    private readonly ILogger<SchedulingService> _logger;
    private readonly IDeferredChangeRepository _deferredChangeRepository;
    private readonly ISettingsService _settingsService;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventLogFactory _eventLogFactory;

    public SchedulingService(ILogger<SchedulingService> logger,
        IDeferredChangeRepository deferredChangeRepository,
        ISettingsService settingsService,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory)
    {
        _logger = logger;
        _deferredChangeRepository = deferredChangeRepository;
        _settingsService = settingsService;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
    }
    
    public async Task RescheduleChange(Guid deferredChangeId, RescheduleDeferredChangeDataContract change)
    {
        var existing = await _deferredChangeRepository.GetById(deferredChangeId);
        
        if (existing == null)
        {
            throw new ChangeNotFoundException($"No deferred change with id {deferredChangeId}");
        }

        existing.ExecuteAtUtc = change.NewExecuteAtUtc;
        
        await _deferredChangeRepository.UpdateDeferredChange(existing);

        await _eventLogRepository.Add(_eventLogFactory.ChangesScheduled(existing.ClientName,
            existing.Instance,
            existing.RequestingUser,
            existing.ChangeSet!,
            false,
            true));
    }

    public async Task ExecuteDueChanges()
    {
        var changesToExecute = await _deferredChangeRepository.GetChangesToExecute(DateTime.UtcNow);
        _settingsService.SetAuthenticatedUser(new ServiceUser());
        
        foreach (var change in changesToExecute.Where(c => c.ChangeSet is not null))
        {
            change.ChangeSet!.Schedule!.ApplyAtUtc = null; // Otherwise we'll get an endless loop of schedules
            await _settingsService.UpdateSettingValues(change.ClientName, change.Instance, change.ChangeSet!);
        }
    }

    public async Task<SchedulingChangesDataContract> GetAllDeferredChanges()
    {
        var items = await _deferredChangeRepository.GetAllChanges();
        var result = new SchedulingChangesDataContract
        {
            Changes = items.Select(a => a.Convert())
        };

        return result;
    }
    
    public async Task DeleteDeferredChange(Guid deferredChangeId)
    {
        var existing = await _deferredChangeRepository.GetById(deferredChangeId);
        
        if (existing == null)
        {
            throw new ChangeNotFoundException($"No deferred change with id {deferredChangeId}");
        }

        await _deferredChangeRepository.Remove(deferredChangeId);
    }
}