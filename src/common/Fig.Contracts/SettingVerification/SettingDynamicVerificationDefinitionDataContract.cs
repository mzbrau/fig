using System.Collections.Generic;

namespace Fig.Contracts.SettingVerification
{
    public class SettingDynamicVerificationDefinitionDataContract
    {
        public SettingDynamicVerificationDefinitionDataContract(string name, string description, string? code,
            TargetRuntime targetRuntime, List<string> settingsVerified)
        {
            Name = name;
            Description = description;
            Code = code;
            TargetRuntime = targetRuntime;
            SettingsVerified = settingsVerified;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public string? Code { get; set; }

        public TargetRuntime TargetRuntime { get; set; }

        public List<string> SettingsVerified { get; set; }
    }
}