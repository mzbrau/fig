using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fig.Client.ConfigurationProvider;

public interface IApiCommunicationHandler
{
    Task RegisterWithFigApi(string clientName, SettingsClientDefinitionDataContract settings);

    Task<List<SettingDataContract>> RequestConfiguration(string apiUri, string clientName, string? instance);
}