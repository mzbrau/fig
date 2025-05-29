namespace Fig.Contracts.CustomActions
{
    public class CustomActionDefinitionDataContract
    {
        public string Name { get; set; } // Name of the action, internal identifier
        public string ButtonName { get; set; } // Text displayed on the button in the UI
        public string Description { get; set; } // Tooltip or help text for the action
        public List<SettingDefinitionDataContract> SettingsUsed { get; set; } // A list of settings that this action might use or affect.
    }
}
