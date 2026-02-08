using Fig.Contracts.ImportExport;
using Fig.Web.Models.Compare;

namespace Fig.Web.Services;

public interface ISettingCompareService
{
    Task<(IReadOnlyList<SettingCompareModel> Rows, CompareStatisticsModel Stats)> CompareAsync(
        FigDataExportDataContract exportData);

    Task<(IReadOnlyList<SettingCompareModel> Rows, CompareStatisticsModel Stats)> CompareAsync(
        FigValueOnlyDataExportDataContract exportData);
}
