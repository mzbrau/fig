using Fig.Contracts.Authentication;
using Fig.Contracts.Scheduling;
using Fig.Contracts.Settings;

namespace Fig.Api.Services;

public interface ISchedulingService
{
    Task<SchedulingChangesDataContract> GetAllDeferredChanges();
    
    Task RescheduleChange(Guid deferredChangeId, RescheduleDeferredChangeDataContract change);

    Task ExecuteDueChanges();
    
    Task DeleteDeferredChange(Guid deferredChangeId);
}