using Fig.Client.Attributes;

namespace Fig.Unit.Test.TestInfrastructure;

public class NestedParent
{
    [NestedSetting]
    public NestedChild Child { get; set; } = new NestedChild();
}