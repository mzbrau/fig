using System;
using System.Collections.Generic;

namespace Fig.Contracts.EventHistory
{
    public class EventLogCollectionDataContract
    {
        public EventLogCollectionDataContract(DateTime earliestEvent, DateTime resultStartTime, DateTime resultEndTime, IEnumerable<EventLogDataContract> events)
        {
            EarliestEvent = earliestEvent;
            ResultStartTime = resultStartTime;
            ResultEndTime = resultEndTime;
            Events = events;
        }

        public DateTime EarliestEvent { get; }
        
        public DateTime ResultStartTime { get; }
        
        public DateTime ResultEndTime { get; }
        
        public IEnumerable<EventLogDataContract> Events { get; }
    }
}