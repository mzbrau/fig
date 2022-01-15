using System;

namespace Fig.Contracts.SettingVerification
{
    public class SettingVerificationDefinitionDataContract
    {
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public string Code { get; set; }

        public TargetRuntime TargetRuntime { get; set; }
    }
}