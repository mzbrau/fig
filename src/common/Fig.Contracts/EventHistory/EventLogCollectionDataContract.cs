using System;
using System.Collections.Generic;

namespace Fig.Contracts.EventHistory
{
    public class EventLogCollectionDataContract
    {
        public DateTime EarliestEvent { get; set; }
        
        public DateTime ResultStartTime { get; set; }
        
        public DateTime ResultEndTime { get; set; }
        
        public IEnumerable<EventLogDataContract> Events { get; set; }
    }
}