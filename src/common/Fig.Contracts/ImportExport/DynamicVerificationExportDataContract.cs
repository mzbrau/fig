using System.Collections.Generic;
using Fig.Contracts.SettingVerification;

namespace Fig.Contracts.ImportExport
{
    public class DynamicVerificationExportDataContract
    {
        public DynamicVerificationExportDataContract(string name, string description, string code, TargetRuntime targetRuntime, IList<string>? settingsVerified)
        {
            Name = name;
            Description = description;
            Code = code;
            TargetRuntime = targetRuntime;
            SettingsVerified = settingsVerified;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Code { get; set; }

        public TargetRuntime TargetRuntime { get; set; }

        public IList<string>? SettingsVerified { get; set; }
    }
}