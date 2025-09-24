using Fig.Web.Models.Events;

namespace Fig.Web.Facades;

public interface IEventsFacade
{
    List<EventLogModel> EventLogs { get; }
    
    DateTime EarliestDate { get; }
    
    DateTime StartTime { get; set; }
    
    DateTime EndTime { get; set; }

    Task QueryEvents(DateTime startTime, DateTime endTime);
    
    Task<List<EventLogModel>> GetClientTimeline(string clientName, string? instance);
}