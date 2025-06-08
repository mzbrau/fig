using Fig.Client;
using Fig.Client.Attributes;
using Fig.Client.Validation;
using Serilog.Events;
using System.Collections.Generic;

namespace Fig.Integration.VersionCompatibilityTest
{
    public class TestSettings : SettingsBase
    {
        public override string ClientDescription => "Version Compatibility Test";

        [Setting("The name of the city to get weather for.")]
        [Validation(ValidationType.NotEmpty)]
        public string? Location { get; set; } = "Melbourne";

        public override IEnumerable<string> GetValidationErrors()
        {
            return new List<string>();
        }
    }
}
