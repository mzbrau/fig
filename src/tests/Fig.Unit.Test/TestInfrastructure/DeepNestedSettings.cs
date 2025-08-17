using System.Collections.Generic;
using Fig.Client.Abstractions.Attributes;

namespace Fig.Unit.Test.TestInfrastructure;

public class DeepNestedSettings : Fig.Client.SettingsBase
{
    public override string ClientDescription => "Test";
    public override IEnumerable<string> GetValidationErrors() => new List<string>();

    [NestedSetting]
    public NestedParent Parent { get; set; } = new NestedParent();
}