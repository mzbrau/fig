using System.Collections.Generic;
using System.Linq;
using Fig.Contracts.EventHistory;

namespace Fig.Integration.Test.Utils;

public static class CollectionExtensionMethods
{
    public static List<EventLogDataContract> RemoveCheckPointEvents(this IEnumerable<EventLogDataContract> eventLogs)
    {
        return eventLogs.Where(a => !a.EventType.Contains("CheckPoint")).ToList();
    }
}