using Fig.Client.Attributes;

namespace Fig.Unit.Test.TestInfrastructure;

public class NestedChild
{
    [Validation(@"^\d{2}$", "Must be 2 digits")]
    public string Code { get; set; } = "12";
}