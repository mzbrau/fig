using System.Collections.Generic;

namespace Fig.Contracts.SettingDefinitions
{
    public class SettingClientDescriptionDataContract
    {
        public SettingClientDescriptionDataContract(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; }
        
        public string Description { get; }
    }
}
