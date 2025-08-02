using Fig.Contracts.Scheduling;

namespace Fig.Api.Services;

public interface ISchedulingService
{
    Task<SchedulingChangesDataContract> GetAllDeferredChanges();
    
    Task RescheduleChange(Guid deferredChangeId, RescheduleDeferredChangeDataContract change);

    Task DeleteDeferredChange(Guid deferredChangeId);
}