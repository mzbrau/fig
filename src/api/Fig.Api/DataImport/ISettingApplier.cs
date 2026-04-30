using Fig.Api.Utils;
using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.DataImport;

public interface ISettingApplier
{
    ApplySettingsResult ApplySettings(SettingClientBusinessEntity client, DeferredClientImportBusinessEntity data);

    ApplySettingsResult ApplySettings(SettingClientBusinessEntity client, List<SettingValueExportDataContract> settings, string? customDecryptionKey = null);
}
