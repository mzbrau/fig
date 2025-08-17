using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Enums;

namespace Fig.Test.Common.TestSettings;

public class SettingsWithIndentAttribute : TestSettingsBase
{
    public override string ClientDescription => "Settings to test IndentAttribute functionality";

    public override string ClientName => "SettingsWithIndentAttribute";

    [Setting("Root setting - no indentation")]
    [Category("Hierarchy Test", CategoryColor.Blue)]
    public string RootSetting { get; set; } = "Root";

    [Setting("Child setting - 1 level indented")]
    [Category("Hierarchy Test", CategoryColor.Blue)]
    [Indent(1)]
    public string ChildSetting { get; set; } = "Child";

    [Setting("Grandchild setting - 2 levels indented")]
    [Category("Hierarchy Test", CategoryColor.Blue)]
    [Indent(2)]
    public string GrandchildSetting { get; set; } = "Grandchild";

    [Setting("Deep nested setting - 3 levels indented")]
    [Category("Hierarchy Test", CategoryColor.Blue)]
    [Indent(3)]
    public string DeepNestedSetting { get; set; } = "Deep";

    [Setting("Maximum indent level setting")]
    [Category("Edge Cases", CategoryColor.Red)]
    [Indent(5)]
    public string MaxIndentSetting { get; set; } = "Max";

    [Setting("Zero indent level setting")]
    [Category("Edge Cases", CategoryColor.Red)]
    [Indent(0)]
    public string ZeroIndentSetting { get; set; } = "Zero";

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}
