using Fig.Api.Utils;
using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.DataImport;

public interface IDeferredSettingApplier
{
    List<ChangedSetting> ApplySettings(SettingClientBusinessEntity client, DeferredClientImportBusinessEntity data);

    List<ChangedSetting> ApplySettings(SettingClientBusinessEntity client, List<SettingValueExportDataContract> settings);
}