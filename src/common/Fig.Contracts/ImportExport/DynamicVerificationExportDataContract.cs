using System.Collections.Generic;
using Fig.Contracts.SettingVerification;

namespace Fig.Contracts.ImportExport
{
    public class DynamicVerificationExportDataContract
    {
        public string Name { get; set; }

        public virtual string Description { get; set; }

        public virtual string Code { get; set; }

        public virtual TargetRuntime TargetRuntime { get; set; }

        public IList<string>? SettingsVerified { get; set; }
    }
}