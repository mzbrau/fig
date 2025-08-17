using System.Collections.Generic;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Validation;

namespace Fig.Unit.Test.TestInfrastructure;

public class SimpleSettings : Fig.Client.SettingsBase
{
    public override string ClientDescription => "Test";
    public override IEnumerable<string> GetValidationErrors() => new List<string>();

    [Validation(@"^\d+$", "Must be all digits")]
    public string DigitsOnly { get; set; } = "123";

    [Validation(ValidationType.NotEmpty)]
    public string NotEmpty { get; set; } = "abc";
}