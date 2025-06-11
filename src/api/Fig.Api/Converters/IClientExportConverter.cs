using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface IClientExportConverter
{
    SettingClientExportDataContract Convert(SettingClientBusinessEntity client);

    SettingClientValueExportDataContract ConvertValueOnly(SettingClientBusinessEntity client, bool excludeEnvironmentSpecific = false);

    SettingClientBusinessEntity Convert(SettingClientExportDataContract client);
}