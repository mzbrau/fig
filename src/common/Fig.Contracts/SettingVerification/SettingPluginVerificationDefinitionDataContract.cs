using System.Collections.Generic;

namespace Fig.Contracts.SettingVerification
{
    public class SettingPluginVerificationDefinitionDataContract
    {
        public string Name { get; set; }

        public string? Description { get; set; }

        public List<string> PropertyArguments { get; set; }
    }
}