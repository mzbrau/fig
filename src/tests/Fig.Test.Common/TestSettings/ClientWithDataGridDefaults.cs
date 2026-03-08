using Fig.Client.Abstractions.Attributes;

namespace Fig.Test.Common.TestSettings;

public class ClientWithDataGridDefaults : TestSettingsBase
{
    public override string ClientName => "ClientWithDataGridDefaults";
    public override string ClientDescription => "Client with DataGrid settings that have default values";

    [Setting("Simple string list", defaultValueMethodName: nameof(GetDefaultStringList))]
    public List<string> SimpleStringList { get; set; } = null!;

    [Setting("Single column complex list", defaultValueMethodName: nameof(GetDefaultSingleColumnItems))]
    public List<SingleColumnItem> SingleColumnComplexList { get; set; } = null!;

    [Setting("Multi column complex list", defaultValueMethodName: nameof(GetDefaultMultiColumnItems))]
    public List<MultiColumnItem> MultiColumnComplexList { get; set; } = null!;

    [Setting("Single column int list", defaultValueMethodName: nameof(GetDefaultIntList))]
    public List<int> SingleColumnIntList { get; set; } = null!;

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }

    public static List<string> GetDefaultStringList()
    {
        return ["alpha", "beta", "gamma"];
    }

    public static List<int> GetDefaultIntList()
    {
        return [10, 20, 30];
    }

    public static List<SingleColumnItem> GetDefaultSingleColumnItems()
    {
        return
        [
            new() { Name = "Item1" },
            new() { Name = "Item2" }
        ];
    }

    public static List<MultiColumnItem> GetDefaultMultiColumnItems()
    {
        return
        [
            new() { Label = "First", Count = 5, IsActive = true },
            new() { Label = "Second", Count = 10, IsActive = false }
        ];
    }
}

public class SingleColumnItem
{
    public string Name { get; set; } = string.Empty;
}

public class MultiColumnItem
{
    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }

    public bool IsActive { get; set; }
}
