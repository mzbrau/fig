using System.Collections.Generic;
using Fig.Client.Attributes;

namespace Fig.Unit.Test.TestInfrastructure;

[ValidationOfAllTypes(@"^.{3,}$", "Must be at least 3 chars", true, typeof(string))]
public class ClassLevelSettings : Fig.Client.SettingsBase
{
    public override string ClientDescription => "Test";
    public override IEnumerable<string> GetValidationErrors() => new List<string>();

    public string Name { get; set; } = "John";
    public string Description { get; set; } = "Desc";
    [Validation(@"^A.*", "Must start with A")]
    public string Special { get; set; } = "Apple";
}