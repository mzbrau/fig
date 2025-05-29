using System;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fig.Client.CustomActions; // Added for ICustomAction
using Fig.Contracts.CustomActions; // Added for CustomActionClientPollResponseDataContract and CustomActionClientExecuteRequestDataContract
using System.Threading; // Added for CancellationToken

namespace Fig.Client.ConfigurationProvider;

public interface IApiCommunicationHandlerV2 // Renamed interface
{
    Task RegisterWithFigApi(string clientName, SettingsClientDefinitionDataContract settings);

    Task<List<SettingDataContract>> RequestConfiguration(string clientName, string? instance, Guid runSessionId);

    Task RegisterCustomActions(IEnumerable<ICustomAction> customActions, CancellationToken cancellationToken);

    Task<IEnumerable<CustomActionClientPollResponseDataContract>?> PollForCustomActionRequests(CancellationToken cancellationToken);

    Task SendCustomActionResults(CustomActionClientExecuteRequestDataContract results, CancellationToken cancellationToken);
}
