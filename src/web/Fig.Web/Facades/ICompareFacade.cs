using Fig.Web.Models.Compare;

namespace Fig.Web.Facades;

/// <summary>
/// Holds compare page state so it survives navigation.
/// Registered as scoped (one per circuit).
/// </summary>
public interface ICompareFacade
{
    IReadOnlyList<SettingCompareModel>? CompareRows { get; set; }

    CompareStatisticsModel? Statistics { get; set; }

    CompareFilterMode FilterMode { get; set; }

    string ClientFilter { get; set; }
}
