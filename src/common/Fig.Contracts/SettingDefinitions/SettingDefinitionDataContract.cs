namespace Fig.Contracts.SettingDefinitions
{
    public class SettingDefinitionDataContract
    {
        public string Name { get; set; }

        public bool IsSecret { get; set; }

        public dynamic DefaultValue { get; set; }

        public string ValidationRegex { get; set; }

        public string Description { get; set; }

        public string ValidationExplanation { get; set; }

        public string Group { get; set; }
        
        public string FriendlyName { get; set; }
    }
}

