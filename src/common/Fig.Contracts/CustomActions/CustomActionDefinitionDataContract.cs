namespace Fig.Contracts.CustomActions
{
    public class CustomActionDefinitionDataContract
    {
        public CustomActionDefinitionDataContract(string name, string buttonName, string description, string settingsUsed)
        {
            Name = name;
            ButtonName = buttonName;
            Description = description;
            SettingsUsed = settingsUsed;
        }

        public string Name { get; set; }
        public string ButtonName { get; set; }
        public string Description { get; set; }
        public string SettingsUsed { get; set; }
    }
}
