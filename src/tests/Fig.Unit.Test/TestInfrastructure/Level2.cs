using Fig.Client.Attributes;

namespace Fig.Unit.Test.TestInfrastructure;

public class Level2
{
    [NestedSetting]
    public Level3 Level3 { get; set; } = new Level3();
}