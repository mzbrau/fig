using Fig.Web.Models.Compare;

namespace Fig.Web.Facades;

public class CompareFacade : ICompareFacade
{
    public IReadOnlyList<SettingCompareModel>? CompareRows { get; set; }

    public CompareStatisticsModel? Statistics { get; set; }

    public CompareFilterMode FilterMode { get; set; } = CompareFilterMode.All;

    public string ClientFilter { get; set; } = string.Empty;
}
