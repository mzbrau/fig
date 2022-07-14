using System.Collections.Generic;

namespace Fig.Contracts.SettingVerification
{
    public class SettingPluginVerificationDefinitionDataContract
    {
        public SettingPluginVerificationDefinitionDataContract(string name, string? description,
            List<string> propertyArguments)
        {
            Name = name;
            Description = description;
            PropertyArguments = propertyArguments;
        }

        public string Name { get; }

        public string? Description { get; }

        public List<string> PropertyArguments { get; }
    }
}