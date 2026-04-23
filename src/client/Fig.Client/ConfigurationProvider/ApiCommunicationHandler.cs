using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fig.Client.Contracts;
using Fig.Client.CustomActions;
using Fig.Client.Exceptions;
using Fig.Client.LookupTable;
using Fig.Client.Startup;
using Fig.Common.NetStandard.IpAddress;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.CustomActions;
using Fig.Contracts.LookupTable;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Client.ConfigurationProvider;

public class ApiCommunicationHandler : IApiCommunicationHandler
{
    private readonly string _clientName;
    private readonly string? _instance;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiCommunicationHandler> _logger;
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly IClientSecretProvider _clientSecretProvider;
    private readonly TimeSpan _requestTimeout;
    private readonly IServiceStartupExtender _startupExtender;

    internal ApiCommunicationHandler(string clientName,
        string? instance,
        HttpClient httpClient,
        ILogger<ApiCommunicationHandler> logger,
        IIpAddressResolver ipAddressResolver,
        IClientSecretProvider clientSecretProvider,
        TimeSpan requestTimeout,
        IServiceStartupExtender startupExtender)
    {
        _clientName = clientName;
        _instance = instance;
        _httpClient = httpClient;
        _logger = logger;
        _ipAddressResolver = ipAddressResolver;
        _clientSecretProvider = clientSecretProvider;
        _requestTimeout = requestTimeout;
        _startupExtender = startupExtender;
          // Connect to the bridge
        CustomActionBridge.PollForCustomActionRequests = PollForCustomActionRequests;
        CustomActionBridge.SendCustomActionResults = SendCustomActionResults;
        CustomActionBridge.RegisterCustomActions = RegisterCustomActions;
        LookupTableBridge.RegisterLookupTable = RegisterLookupTable;
    }

    public async Task RegisterWithFigApi(SettingsClientDefinitionDataContract settings)
    {
        _startupExtender.RequestAdditionalTime(_requestTimeout + TimeSpan.FromSeconds(5));

        var json = JsonConvert.SerializeObject(settings, JsonSettings.FigDefault);
        var payloadBytes = Encoding.UTF8.GetByteCount(json);
        _logger.LogInformation(
            "Registering configuration with the Fig API at address {FigUri}. " +
            "Payload size: {PayloadBytes} bytes ({PayloadKB:F1} KB), " +
            "setting count: {SettingCount}, custom action count: {CustomActionCount}",
            _httpClient.BaseAddress,
            payloadBytes,
            payloadBytes / 1024.0,
            settings.Settings.Count,
            settings.CustomActions.Count);

        var data = new StringContent(json, Encoding.UTF8, "application/json");
        var secret = await _clientSecretProvider.GetSecret(_clientName);
        AddHeaderToHttpClient("ClientSecret", () => secret);

        var sw = Stopwatch.StartNew();
        HttpResponseMessage result;
        try
        {
            result = await _httpClient.PostAsync("/clients", data);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Failed to reach Fig API after {ElapsedMs} ms while registering settings", sw.ElapsedMilliseconds);
            throw;
        }
        sw.Stop();

        if (result.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully registered settings with Fig API in {ElapsedMs} ms", sw.ElapsedMilliseconds);
        }
        else
        {
            var error = await GetErrorResult(result);
            if (error?.ErrorType == "401")
            {
                _logger.LogInformation("Did not register settings with Fig API after {ElapsedMs} ms. {Message}", sw.ElapsedMilliseconds, error.Message);
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

    public async Task<List<SettingDataContract>> RequestConfiguration()
    {
        _logger.LogDebug("Fig: Reading settings from API at address {OptionsApiUri}...", _httpClient.BaseAddress);
        var secret = await _clientSecretProvider.GetSecret(_clientName);
        AddHeaderToHttpClient("Fig_Hostname", () => Environment.MachineName);
        AddHeaderToHttpClient("clientSecret", () => secret);

        var uri = $"/clients/{Uri.EscapeDataString(_clientName)}/settings";
        uri += $"?runSessionId={RunSession.GetId(_clientName)}";
        if (!string.IsNullOrEmpty(_instance))
            uri += $"&instance={Uri.EscapeDataString(_instance!)}";

        var result = await _httpClient.GetStringAsync(uri);

        var settingValues =
            (JsonConvert.DeserializeObject<IEnumerable<SettingDataContract>>(result, JsonSettings.FigDefault) ??
             []).ToList();

        return settingValues;
    }
    
    private async Task RegisterCustomActions(List<CustomActionDefinitionDataContract> customActions)
    {
        var request = new CustomActionRegistrationRequestDataContract(_clientName, customActions);
        var secret = await _clientSecretProvider.GetSecret(_clientName);
        AddHeaderToHttpClient("clientSecret", () => secret);
        var json = JsonConvert.SerializeObject(request, JsonSettings.FigDefault);
        var payloadBytes = Encoding.UTF8.GetByteCount(json);
        _logger.LogInformation(
            "Registering {CustomActionCount} custom action(s) with Fig API: {CustomActionNames}. Payload size: {PayloadBytes} bytes ({PayloadKB:F1} KB)",
            customActions.Count,
            string.Join(", ", customActions.Select(x => x.Name)),
            payloadBytes,
            payloadBytes / 1024.0);

        var data = new StringContent(json, Encoding.UTF8, "application/json");

        var sw = Stopwatch.StartNew();
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("/customactions/register", data);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Failed to reach Fig API after {ElapsedMs} ms while registering custom actions", sw.ElapsedMilliseconds);
            throw;
        }
        sw.Stop();

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
    
    private async Task RegisterLookupTable(LookupTableDataContract lookupTable)
    {
        lookupTable.Name = $"{_clientName}:{lookupTable.Name}";

        var secret = await _clientSecretProvider.GetSecret(_clientName);
        AddHeaderToHttpClient("clientSecret", () => secret);
        var json = JsonConvert.SerializeObject(lookupTable, JsonSettings.FigDefault);
        var payloadBytes = Encoding.UTF8.GetByteCount(json);
        _logger.LogInformation(
            "Registering lookup table '{LookupName}' with Fig API. Payload size: {PayloadBytes} bytes ({PayloadKB:F1} KB)",
            lookupTable.Name,
            payloadBytes,
            payloadBytes / 1024.0);

        var data = new StringContent(json, Encoding.UTF8, "application/json");

        var sw = Stopwatch.StartNew();
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync($"/lookuptables/{_clientName}", data);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Failed to reach Fig API after {ElapsedMs} ms while registering lookup table", sw.ElapsedMilliseconds);
            throw;
        }
        sw.Stop();

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
    
    private async Task<IEnumerable<CustomActionPollResponseDataContract>?> PollForCustomActionRequests()
    {
        var pollUri = $"/customactions/poll/{Uri.EscapeDataString(_clientName)}";
        pollUri += $"?runSessionId={RunSession.GetId(_clientName)}";

        var secret = await _clientSecretProvider.GetSecret(_clientName);
        AddHeaderToHttpClient("clientSecret", () => secret);

        var response = await _httpClient.GetAsync(pollUri);
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
        results.RunSessionId = RunSession.GetId(_clientName);
        var json = JsonConvert.SerializeObject(results, JsonSettings.FigDefault);
        var data = new StringContent(json, Encoding.UTF8, "application/json");
        var secret = await _clientSecretProvider.GetSecret(_clientName);
        AddHeaderToHttpClient("clientSecret", () => secret);
        
        try
        {
            var response = await _httpClient.PostAsync($"/customactions/results/{Uri.EscapeDataString(_clientName)}", data);
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

    private void AddHeaderToHttpClient(string key, Func<string> getValue)
    {
        if (!_httpClient.DefaultRequestHeaders.Contains(key))
            _httpClient.DefaultRequestHeaders.Add(key, getValue());
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
