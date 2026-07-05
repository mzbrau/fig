using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingMigrations;
using Fig.Contracts.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fig.Client.ConfigurationProvider;

public interface IApiCommunicationHandler
{
    Task<bool> RegisterWithFigApi(SettingsClientDefinitionDataContract settings);

    Task<List<SettingMigrationRequestDataContract>> GetMigrateFromMigrationRequests(SettingsClientDefinitionDataContract settings);

    Task<List<SettingDataContract>> RequestConfiguration();

    Task UpdateSettings(SettingValueUpdatesDataContract updates);
}
