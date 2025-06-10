using System;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fig.Client.ConfigurationProvider;

public interface IApiCommunicationHandler
{
    Task RegisterWithFigApi(SettingsClientRegistrationDefinitionDataContract settings);

    Task<List<SettingDataContract>> RequestConfiguration();
}