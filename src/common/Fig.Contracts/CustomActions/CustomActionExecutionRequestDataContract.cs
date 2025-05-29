using System;
using System.Collections.Generic;
using Fig.Contracts.Settings;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionExecutionRequestDataContract
    {
        public Guid CustomActionId { get; set; }
        public string? Instance { get; set; }
        public List<SettingDataContract>? Settings { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}
