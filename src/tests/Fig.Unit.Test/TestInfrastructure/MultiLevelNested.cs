using Fig.Client.Attributes;

namespace Fig.Unit.Test.TestInfrastructure;

public class MultiLevelNested
{
    [NestedSetting]
    public Level2 Level2 { get; set; } = new Level2();
}