namespace Fig.Contracts.EventHistory;

public class EventLogCountDataContract
{
    public EventLogCountDataContract(long eventLogCount)
    {
        EventLogCount = eventLogCount;
    }

    public long EventLogCount { get; set; }
}