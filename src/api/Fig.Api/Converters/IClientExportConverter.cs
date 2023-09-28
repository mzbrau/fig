using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface IClientExportConverter
{
    SettingClientExportDataContract Convert(SettingClientBusinessEntity client, bool excludeSecrets);

    SettingClientValueExportDataContract ConvertValueOnly(SettingClientBusinessEntity client, bool excludeSecrets);

    SettingClientBusinessEntity Convert(SettingClientExportDataContract client);
}