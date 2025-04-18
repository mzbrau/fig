using System;

namespace Fig.Contracts.Scheduling;

public class RescheduleDeferredChangeDataContract
{
    public DateTime NewExecuteAtUtc { get; set; }
}