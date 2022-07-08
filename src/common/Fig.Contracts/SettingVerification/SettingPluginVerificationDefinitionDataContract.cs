using System.Collections.Generic;

namespace Fig.Contracts.SettingVerification
{
    public class SettingPluginVerificationDefinitionDataContract
    {
        public SettingPluginVerificationDefinitionDataContract(string name, string? description, List<string> propertyArguments)
        {
            Name = name;
            Description = description;
            PropertyArguments = propertyArguments;
        }

        public string Name { get; set; }

        public string? Description { get; set; }

        public List<string> PropertyArguments { get; set; }
    }
}