using Fig.Api.Datalayer.Repositories;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Contracts.Scheduling;

namespace Fig.Api.Services;

public class SchedulingService : ISchedulingService
{
    private readonly ILogger<SchedulingService> _logger;
    private readonly IDeferredChangeRepository _deferredChangeRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventLogFactory _eventLogFactory;

    public SchedulingService(ILogger<SchedulingService> logger,
        IDeferredChangeRepository deferredChangeRepository,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory)
    {
        _logger = logger;
        _deferredChangeRepository = deferredChangeRepository;
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
            existing.ExecuteAtUtc,
            false,
            true));
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