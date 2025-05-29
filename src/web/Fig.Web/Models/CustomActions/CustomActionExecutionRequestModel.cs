using System;
using System.Collections.Generic;
using Fig.Contracts.CustomActions; // For CustomActionExecutionRequestDataContract
using Fig.Contracts.Settings; // For SettingDataContract

namespace Fig.Web.Models.CustomActions
{
    public class CustomActionExecutionRequestModel
    {
        public Guid CustomActionId { get; set; }
        public string? Instance { get; set; }
        public List<SettingDataContract>? Settings { get; set; }
        public DateTime RequestedAt { get; set; }

        public CustomActionExecutionRequestModel(Guid customActionId, string? instance, List<SettingDataContract>? settings)
        {
            CustomActionId = customActionId;
            Instance = instance;
            Settings = settings;
            RequestedAt = DateTime.UtcNow;
        }

        public CustomActionExecutionRequestDataContract ToDataContract()
        {
            return new CustomActionExecutionRequestDataContract
            {
                CustomActionId = CustomActionId,
                Instance = Instance,
                Settings = Settings,
                RequestedAt = RequestedAt
            };
        }
    }
}
