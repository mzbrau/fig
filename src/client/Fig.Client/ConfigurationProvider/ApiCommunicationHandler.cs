using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Fig.Client.Capabilities;
using Fig.Client.Contracts;
using Fig.Client.Exceptions;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.CustomActions;
using Fig.Contracts.LookupTable;
using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingMigrations;
using Fig.Contracts.Settings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Client.ConfigurationProvider;

public class ApiCommunicationHandler : IApiCommunicationHandler, IFigClientBridge
{
    private readonly string _clientName;
    private readonly string? _instance;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiCommunicationHandler> _logger;
    private readonly IClientSecretProvider _clientSecretProvider;
    private readonly IFigCapabilityProvider _capabilityProvider;

    internal ApiCommunicationHandler(string clientName,
        string? instance,
        HttpClient httpClient,
        ILogger<ApiCommunicationHandler> logger,
        IClientSecretProvider clientSecretProvider,
        IFigCapabilityProvider capabilityProvider)
    {
        _clientName = clientName;
        _instance = InstanceNormalization.Normalize(instance);
        _httpClient = httpClient;
        _logger = logger;
        _clientSecretProvider = clientSecretProvider;
        _capabilityProvider = capabilityProvider;
    }

    Task<IEnumerable<CustomActionPollResponseDataContract>?> IFigClientBridge.PollForCustomActionRequests() =>
        PollForCustomActionRequests();

    Task IFigClientBridge.SendCustomActionResults(CustomActionExecutionResultsDataContract results) =>
        SendCustomActionResults(results);

    Task IFigClientBridge.RegisterCustomActions(List<CustomActionDefinitionDataContract> customActions) =>
        RegisterCustomActions(customActions);

    Task IFigClientBridge.RegisterLookupTable(LookupTableDataContract lookupTable) =>
        RegisterLookupTable(lookupTable);

    Task IFigClientBridge.UpdateSettings(SettingValueUpdatesDataContract updates) =>
        UpdateSettings(updates);

    public async Task<bool> RegisterWithFigApi(SettingsClientDefinitionDataContract settings)
    {
        await _capabilityProvider.FetchAsync().ConfigureAwait(false);
        LogAmbiguousMigrateFromSources(settings);

        var useDeferredDescription = _capabilityProvider.Supports("deferredDescriptionRegistration") 
                                     && settings.Description != null;

        SettingsClientDefinitionDataContract payload;
        if (useDeferredDescription)
        {
            payload = new SettingsClientDefinitionDataContract(
                settings.Name,
                null,
                settings.Instance,
                settings.HasDisplayScripts,
                settings.Settings,
                settings.ClientSettingOverrides,
                settings.CustomActions,
                settings.ClientVersion,
                settings.SettingMigrationResults);
        }
        else
        {
            payload = settings;
        }

        var json = JsonConvert.SerializeObject(payload, JsonSettings.FigDefault);
        var payloadBytes = Encoding.UTF8.GetByteCount(json);
        _logger.LogInformation(
            "Registering configuration with the Fig API at address {FigUri}. " +
            "Payload size: {PayloadBytes} bytes ({PayloadKB:F1} KB), " +
            "setting count: {SettingCount}, custom action count: {CustomActionCount}{DeferredDesc}",
            _httpClient.BaseAddress,
            payloadBytes,
            payloadBytes / 1024.0,
            settings.Settings.Count,
            settings.CustomActions.Count,
            useDeferredDescription ? " (description deferred)" : string.Empty);

        var secret = await _clientSecretProvider.GetSecret(_clientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/clients");
        request.Content = BuildContent(json);
        request.Headers.TryAddWithoutValidation("ClientSecret", secret);

        var sw = Stopwatch.StartNew();
        HttpResponseMessage result;
        try
        {
            result = await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Failed to reach Fig API after {ElapsedMs} ms while registering settings", sw.ElapsedMilliseconds);
            throw;
        }
        sw.Stop();

        using (result)
        {
            if (result.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully registered settings with Fig API in {ElapsedMs} ms", sw.ElapsedMilliseconds);

                if (useDeferredDescription)
                {
                    _ = PostDescriptionAsync(settings.Name, settings.Instance, settings.Description!, secret)
                        .ContinueWith(
                            task =>
                            {
                                _logger.LogError(
                                    task.Exception,
                                    "Failed to update deferred description for client {Name} instance {Instance}",
                                    settings.Name,
                                    settings.Instance);
                            },
                            TaskContinuationOptions.OnlyOnFaulted);
                }

                return true;
            }
            else
            {
                var error = await GetErrorResult(result);
                if (error?.ErrorType == "401")
                {
                    _logger.LogInformation("Did not register settings with Fig API after {ElapsedMs} ms. {Message}", sw.ElapsedMilliseconds, error.Message);
                    return false;
                }
                else
                {
                    _logger.LogError(
                        "Unable to successfully register settings after {ElapsedMs} ms. Code:{StatusCode}{NewLine}{Error}",
                        sw.ElapsedMilliseconds, result.StatusCode, Environment.NewLine, error);
                    throw new FigRegistrationException(error);
                }
            }
        }
    }

    public async Task<List<SettingMigrationRequestDataContract>> GetMigrateFromMigrationRequests(SettingsClientDefinitionDataContract settings)
    {
        if (!settings.Settings.Any(HasCustomMigrateFromMigration))
            return [];

        await _capabilityProvider.FetchAsync(force: true).ConfigureAwait(false);
        if (!_capabilityProvider.Supports("migrateFromClientTransforms"))
        {
            throw new FigRegistrationException(new ErrorResultDataContract(
                "UnsupportedCapability",
                "The Fig API does not support custom MigrateFrom migration methods.",
                "Upgrade the Fig API or remove the migration method from the MigrateFrom attribute.",
                null));
        }

        var secret = await _clientSecretProvider.GetSecret(_clientName);
        var json = JsonConvert.SerializeObject(settings, JsonSettings.FigDefault);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/clients/migrations/preview");
        request.Content = BuildContent(json);
        request.Headers.TryAddWithoutValidation("ClientSecret", secret);

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await GetErrorResult(response);
            throw new FigRegistrationException(error);
        }

        var result = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<List<SettingMigrationRequestDataContract>>(result, JsonSettings.FigDefault) ?? [];
    }

    [Conditional("DEBUG")]
    private void LogAmbiguousMigrateFromSources(SettingsClientDefinitionDataContract settings)
    {
        foreach (var setting in settings.Settings.Where(setting =>
                     !string.IsNullOrWhiteSpace(setting.MigrateFrom) &&
                     settings.Settings.Any(candidate => candidate.Name == setting.MigrateFrom)))
        {
            _logger.LogWarning(
                "Setting {TargetSettingName} for client {ClientName} declares MigrateFrom source {SourceSettingName}, but the source setting still exists in this application registration",
                setting.Name,
                settings.Name,
                setting.MigrateFrom);
        }
    }

    private static bool HasCustomMigrateFromMigration(SettingDefinitionDataContract setting)
    {
        return !string.IsNullOrWhiteSpace(setting.MigrateFrom) &&
               !string.IsNullOrWhiteSpace(setting.MigrateFromMigrationMethod);
    }

    private async Task PostDescriptionAsync(string clientName, string? instance, string description, string secret)
    {
        var watch = Stopwatch.StartNew();
        var normalizedInstance = InstanceNormalization.Normalize(instance);
        var descJson = JsonConvert.SerializeObject(
            new ClientDescriptionUpdateDataContract(description), JsonSettings.FigDefault);
        var descData = BuildContent(descJson);
        var payloadBytes = Encoding.UTF8.GetByteCount(descJson);
        _logger.LogInformation(
            "Registering client description with the Fig API. Payload size: {PayloadBytes} bytes ({PayloadKB:F1} KB)",
            payloadBytes,
            payloadBytes / 1024.0);
        var uri = normalizedInstance != null
            ? $"/clients/{Uri.EscapeDataString(clientName)}/description?instance={Uri.EscapeDataString(normalizedInstance)}"
            : $"/clients/{Uri.EscapeDataString(clientName)}/description";

        using var msg = new HttpRequestMessage(HttpMethod.Put, uri);
        msg.Content = descData;
        msg.Headers.TryAddWithoutValidation("ClientSecret", secret);
        using var response = await _httpClient.SendAsync(msg).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
            _logger.LogInformation("Description for {ClientName} registered successfully in {ElapsedMs}ms", clientName, watch.ElapsedMilliseconds);
        else
            _logger.LogWarning("Deferred description for {ClientName} returned {StatusCode} in {ElapsedMs}", clientName, response.StatusCode, watch.ElapsedMilliseconds);
    }

    public async Task<List<SettingDataContract>> RequestConfiguration()
    {
        _logger.LogDebug("Fig: Reading settings from API at address {OptionsApiUri}...", _httpClient.BaseAddress);
        var secret = await _clientSecretProvider.GetSecret(_clientName);

        var uri = $"/clients/{Uri.EscapeDataString(_clientName)}/settings";
        uri += $"?runSessionId={RunSession.GetId(_clientName, _instance)}";
        if (!string.IsNullOrEmpty(_instance))
            uri += $"&instance={Uri.EscapeDataString(_instance!)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.TryAddWithoutValidation("Fig_Hostname", Environment.MachineName);
        request.Headers.TryAddWithoutValidation("clientSecret", secret);
        using var response = await _httpClient.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new FigClientNotFoundException(_clientName, _instance);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();

        var settingValues =
            (JsonConvert.DeserializeObject<IEnumerable<SettingDataContract>>(result, JsonSettings.FigDefault) ??
             []).ToList();

        return settingValues;
    }

    public async Task UpdateSettings(SettingValueUpdatesDataContract updates)
    {
        await _capabilityProvider.FetchAsync().ConfigureAwait(false);
        if (!_capabilityProvider.Supports("clientSettingUpdates"))
        {
            throw new FigSettingUpdateException(new ErrorResultDataContract(
                "UnsupportedCapability",
                "The Fig API does not support client setting updates.",
                "Upgrade the Fig API or avoid using ISettingUpdater with this server.",
                null));
        }

        var secret = await _clientSecretProvider.GetSecret(_clientName);
        var valueUpdates = updates.ValueUpdates as IList<SettingDataContract> ?? updates.ValueUpdates.ToList();
        var serializedUpdates = ReferenceEquals(valueUpdates, updates.ValueUpdates)
            ? updates
            : new SettingValueUpdatesDataContract(valueUpdates, updates.ChangeMessage, updates.Schedule);
        var json = JsonConvert.SerializeObject(serializedUpdates, JsonSettings.FigDefault);
        var uri = $"/clients/{Uri.EscapeDataString(_clientName)}/settings/self";
        if (!string.IsNullOrEmpty(_instance))
            uri += $"?instance={Uri.EscapeDataString(_instance!)}";

        using var request = new HttpRequestMessage(HttpMethod.Put, uri);
        request.Content = BuildContent(json);
        request.Headers.TryAddWithoutValidation("Fig_Hostname", Environment.MachineName);
        request.Headers.TryAddWithoutValidation("ClientSecret", secret);
        using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw new FigSettingUpdateException(await GetErrorResult(response).ConfigureAwait(false));

        _logger.LogInformation("Successfully updated {SettingCount} setting(s) in Fig API", valueUpdates.Count);
    }
    
    private async Task RegisterCustomActions(List<CustomActionDefinitionDataContract> customActions)
    {
        var request = new CustomActionRegistrationRequestDataContract(_clientName, customActions);
        var secret = await _clientSecretProvider.GetSecret(_clientName);
        var json = JsonConvert.SerializeObject(request, JsonSettings.FigDefault);
        var payloadBytes = Encoding.UTF8.GetByteCount(json);
        _logger.LogInformation(
            "Registering {CustomActionCount} custom action(s) with Fig API: {CustomActionNames}. Payload size: {PayloadBytes} bytes ({PayloadKB:F1} KB)",
            customActions.Count,
            string.Join(", ", customActions.Select(x => x.Name)),
            payloadBytes,
            payloadBytes / 1024.0);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/customactions/register");
        httpRequest.Content = BuildContent(json);
        httpRequest.Headers.TryAddWithoutValidation("clientSecret", secret);

        var sw = Stopwatch.StartNew();
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Failed to reach Fig API after {ElapsedMs} ms while registering custom actions", sw.ElapsedMilliseconds);
            throw;
        }
        sw.Stop();

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully registered custom actions in {ElapsedMs} ms", sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError("Failed to register custom actions after {ElapsedMs} ms. Status: {StatusCode}, Response: {Response}",
                    sw.ElapsedMilliseconds, response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            response.EnsureSuccessStatusCode();
        }
    }
    
    private async Task RegisterLookupTable(LookupTableDataContract lookupTable)
    {
        lookupTable.Name = $"{_clientName}:{lookupTable.Name}";

        var secret = await _clientSecretProvider.GetSecret(_clientName);
        var json = JsonConvert.SerializeObject(lookupTable, JsonSettings.FigDefault);
        var payloadBytes = Encoding.UTF8.GetByteCount(json);
        _logger.LogInformation(
            "Registering lookup table '{LookupName}' with Fig API. Payload size: {PayloadBytes} bytes ({PayloadKB:F1} KB)",
            lookupTable.Name,
            payloadBytes,
            payloadBytes / 1024.0);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/lookuptables/{_clientName}");
        request.Content = BuildContent(json);
        request.Headers.TryAddWithoutValidation("clientSecret", secret);

        var sw = Stopwatch.StartNew();
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Failed to reach Fig API after {ElapsedMs} ms while registering lookup table", sw.ElapsedMilliseconds);
            throw;
        }
        sw.Stop();

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully registered lookup table in {ElapsedMs} ms", sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError("Failed to register lookup table after {ElapsedMs} ms. Status: {StatusCode}, Response: {Response}",
                    sw.ElapsedMilliseconds, response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            response.EnsureSuccessStatusCode();
        }
    }
    
    private async Task<IEnumerable<CustomActionPollResponseDataContract>?> PollForCustomActionRequests()
    {
        var pollUri = $"/customactions/poll/{Uri.EscapeDataString(_clientName)}";
        pollUri += $"?runSessionId={RunSession.GetId(_clientName, _instance)}";

        var secret = await _clientSecretProvider.GetSecret(_clientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, pollUri);
        request.Headers.TryAddWithoutValidation("clientSecret", secret);

        using var response = await _httpClient.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return [];
        }

        response.EnsureSuccessStatusCode(); // Throw for other errors

        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<IEnumerable<CustomActionPollResponseDataContract>>(content, JsonSettings.FigDefault);
    }

    private async Task SendCustomActionResults(CustomActionExecutionResultsDataContract results)
    {
        _logger.LogInformation("Sending custom action results for ExecutionId: {ExecutionId}", results.ExecutionId);
        results.RunSessionId = RunSession.GetId(_clientName, _instance);
        var json = JsonConvert.SerializeObject(results, JsonSettings.FigDefault);
        var secret = await _clientSecretProvider.GetSecret(_clientName);
        
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/customactions/results/{Uri.EscapeDataString(_clientName)}");
            request.Content = BuildContent(json);
            request.Headers.TryAddWithoutValidation("clientSecret", secret);
            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent custom action results for ExecutionId: {ExecutionId}", results.ExecutionId);
            }
            else
            {
                _logger.LogError(
                    "Failed to send custom action results for ExecutionId: {ExecutionId}. Status: {StatusCode}, Response: {Response}",
                    results.ExecutionId,
                    response.StatusCode,
                    await response.Content.ReadAsStringAsync());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending custom action results for ExecutionId: {ExecutionId}", results.ExecutionId);
        }
    }
    
    private HttpContent BuildContent(string json)
    {
        const int CompressionThresholdBytes = 4096;
        if (!_capabilityProvider.Supports("requestCompression"))
            return new StringContent(json, Encoding.UTF8, "application/json");

        var bytes = Encoding.UTF8.GetBytes(json);
        if (bytes.Length < CompressionThresholdBytes)
            return new StringContent(json, Encoding.UTF8, "application/json");

        var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Fastest, leaveOpen: true))
            gz.Write(bytes, 0, bytes.Length);

        ms.Seek(0, SeekOrigin.Begin);
        var content = new StreamContent(ms);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        content.Headers.ContentEncoding.Add("gzip");
        return content;
    }


    private async Task<ErrorResultDataContract?> GetErrorResult(HttpResponseMessage response)
    {
        ErrorResultDataContract? errorContract = null;
        if (!response.IsSuccessStatusCode)
        {
            var resultString = await response.Content.ReadAsStringAsync();

            errorContract = resultString.Contains("Reference")
                ? JsonConvert.DeserializeObject<ErrorResultDataContract>(resultString)
                : new ErrorResultDataContract("Unknown", response.StatusCode.ToString(), resultString, null);
        }

        return errorContract;
    }
}
