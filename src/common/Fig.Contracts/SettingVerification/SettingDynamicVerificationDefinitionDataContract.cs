namespace Fig.Contracts.SettingVerification
{
    public class SettingDynamicVerificationDefinitionDataContract
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Code { get; set; }

        public TargetRuntime TargetRuntime { get; set; }
    }
}