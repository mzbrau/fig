using Fig.Contracts.Scheduling;
using Fig.Web.Models.Scheduling;

namespace Fig.Web.Facades;

public interface ISchedulingFacade
{
    List<DeferredChangeModel> DeferredChanges { get; }

    Task GetAllDeferredChanges();

    Task RescheduleChange(Guid deferredChangeId, RescheduleDeferredChangeDataContract change);

    Task DeleteDeferredChange(Guid deferredChangeId);
}