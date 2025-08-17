using Fig.Client.Abstractions.Attributes;

namespace Fig.Unit.Test.TestInfrastructure;

public class Level3
{
    [Validation(@"^X+$", "Must be all X")]
    public string Value { get; set; } = "X";
}