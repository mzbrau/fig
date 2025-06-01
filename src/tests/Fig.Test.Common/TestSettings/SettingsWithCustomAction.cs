using Fig.Client; // For SettingsBase

namespace Fig.Test.Common.TestSettings
{
    public class SettingsWithCustomAction : TestSettingsBase
    {
        public override string ClientName => "SettingsWithCustomAction";
        public override string ClientDescription => "Settings for custom action tests.";

        [Client.Attributes.Setting("My Test Setting")]
        public string MySetting { get; set; } = "DefaultValue";

        public override IEnumerable<string> GetValidationErrors()
        {
            // No validation for this simple test settings class
            return [];
        }
    }
}
