using System;
using Newtonsoft.Json;

namespace Fig.Contracts.ImportExport
{
    public class SettingLastChangedDataContract
    {
        public SettingLastChangedDataContract(string changedBy, DateTime changedAt, string? changeMessage)
        {
            ChangedBy = changedBy;
            ChangedAt = changedAt;
            ChangeMessage = changeMessage;
        }

        public string ChangedBy { get; }

        public DateTime ChangedAt { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ChangeMessage { get; }
    }
}
