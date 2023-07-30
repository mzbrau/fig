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

        public string Name { get; }

        public string Description { get; }

        public string? Code { get; }

        public TargetRuntime TargetRuntime { get; }

        public List<string> SettingsVerified { get; }
    }
}