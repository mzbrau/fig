using Fig.Client.Attributes;

namespace Fig.Test.Common.TestSettings
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SettingsWithCustomAction : TestSettingsBase
    {
        public override string ClientName => "SettingsWithCustomAction";
        public override string ClientDescription => "Settings for custom action tests.";

        [Setting("My Test Setting")]
        public string MySetting { get; set; } = "DefaultValue";

        public override IEnumerable<string> GetValidationErrors()
        {
            // No validation for this simple test settings class
            return [];
        }
    }
}
