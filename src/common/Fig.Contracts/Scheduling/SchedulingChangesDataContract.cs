using System.Collections.Generic;

namespace Fig.Contracts.Scheduling;

public class SchedulingChangesDataContract
{
    public IEnumerable<DeferredChangeDataContract> Changes { get; set; } = new List<DeferredChangeDataContract>();
}