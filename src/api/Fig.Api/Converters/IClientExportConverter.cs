using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface IClientExportConverter
{
    SettingClientExportDataContract Convert(SettingClientBusinessEntity client, bool decryptSecrets);

    SettingClientBusinessEntity Convert(SettingClientExportDataContract client);
}