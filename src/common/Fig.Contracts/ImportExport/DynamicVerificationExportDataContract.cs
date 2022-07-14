using System.Collections.Generic;
using Fig.Contracts.SettingVerification;

namespace Fig.Contracts.ImportExport
{
    public class DynamicVerificationExportDataContract
    {
        public DynamicVerificationExportDataContract(string name, string? description, string? code, TargetRuntime targetRuntime, IList<string>? settingsVerified)
        {
            Name = name;
            Description = description;
            Code = code;
            TargetRuntime = targetRuntime;
            SettingsVerified = settingsVerified;
        }

        public string Name { get; }

        public string? Description { get; }

        public string? Code { get; }

        public TargetRuntime TargetRuntime { get; }

        public IList<string>? SettingsVerified { get; }
    }
}