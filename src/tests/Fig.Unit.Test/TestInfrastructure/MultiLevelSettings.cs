using System.Collections.Generic;
using Fig.Client.Abstractions.Attributes;

namespace Fig.Unit.Test.TestInfrastructure;

public class MultiLevelSettings : Fig.Client.SettingsBase
{
    public override string ClientDescription => "Test";
    
    public override IEnumerable<string> GetValidationErrors() => new List<string>();
    
    [NestedSetting]
    public MultiLevelNested Nested { get; set; } = new MultiLevelNested();
}