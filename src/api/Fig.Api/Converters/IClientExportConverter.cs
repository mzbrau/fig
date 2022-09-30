using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface IClientExportConverter
{
    SettingClientExportDataContract Convert(SettingClientBusinessEntity client, bool decryptSecrets);

    SettingClientValueExportDataContract ConvertValueOnly(SettingClientBusinessEntity client);

    SettingClientBusinessEntity Convert(SettingClientExportDataContract client);
}