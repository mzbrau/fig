using System;
using System.Collections.Generic;
using Fig.Contracts.Settings;

namespace Fig.Contracts.CustomActions
{
    public class CustomActionClientPollResponseDataContract
    {
        public Guid ExecutionId { get; set; }
        public Guid CustomActionId { get; set; }
        public string ActionName { get; set; }
        public List<SettingDataContract>? Settings { get; set; }
    }
}
