using System;
using System.Collections.Generic;
using Fig.Contracts.CustomActions; // For CustomActionDefinitionDataContract
using Fig.Web.Models.Setting; // For SettingConfigurationModel

namespace Fig.Web.Models.CustomActions
{
    public class CustomActionDefinitionModel
    {
        public CustomActionDefinitionDataContract OriginalContract { get; } // To access original data if needed

        public string Name { get; set; }
        public string ButtonName { get; set; }
        public string Description { get; set; }
        public List<SettingConfigurationModel> SettingsUsed { get; set; } = new();
        public string? SelectedInstance { get; set; }
        public List<string> AvailableInstances { get; set; } = new(); // Includes "auto"
        public bool CanExecute { get; set; } = true; // Default to true, can be updated based on logic
        public bool IsExecuting { get; set; }
        public CustomActionExecutionStatusModel? LastExecution { get; set; }

        public CustomActionDefinitionModel(CustomActionDefinitionDataContract contract)
        {
            OriginalContract = contract;
            Name = contract.Name;
            ButtonName = contract.ButtonName;
            Description = contract.Description;
            // SettingsUsed will be populated by the facade/page model
            // AvailableInstances will be populated by the facade/page model
        }
    }
}
