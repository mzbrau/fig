using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.ClientSecret;
using Fig.Client.Contracts;
using Fig.Client.CustomActions;
using Fig.Common.NetStandard.IpAddress;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.CustomActions;
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
    internal ApiCommunicationHandler(string clientName, string? instance, HttpClient httpClient, ILogger<ApiCommunicationHandler> logger, IIpAddressResolver ipAddressResolver, IClientSecretProvider clientSecretProvider)
    {
        _clientName = clientName;
        _instance = instance;
        _httpClient = httpClient;
        _logger = logger;
        _ipAddressResolver = ipAddressResolver;
        _clientSecretProvider = clientSecretProvider;
          // Connect to the bridge
        CustomActionBridge.PollForCustomActionRequests = PollForCustomActionRequests;
        CustomActionBridge.SendCustomActionResults = SendCustomActionResults;
        CustomActionBridge.RegisterCustomActions = RegisterCustomActions;
    }

    public async Task RegisterWithFigApi(SettingsClientRegistrationDefinitionDataContract settings)
    {
        _logger.LogInformation("Registering configuration with the Fig API at address {FigUri}", _httpClient.BaseAddress);
        var json = JsonConvert.SerializeObject(settings, JsonSettings.FigDefault);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        var secret = await _clientSecretProvider.GetSecret(_clientName);
        AddHeaderToHttpClient("ClientSecret", () => secret);
        var result = await _httpClient.PostAsync("/clients", data);

        if (result.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully registered settings with Fig API");
        }
        else
        {
            var error = await GetErrorResult(result);
            if (error?.ErrorType == "401")
            {
                _logger.LogInformation("Did not register settings with Fig API. {Message}", error.Message);
            }
            else
            {
                _logger.LogError(
                    "Unable to successfully register settings. Code:{StatusCode}{NewLine}{Error}", result.StatusCode, Environment.NewLine, error);
            }
        }
    }

    public async Task<List<SettingDataContract>> RequestConfiguration()
    {
        _logger.LogDebug("Fig: Reading settings from API at address {OptionsApiUri}...", _httpClient.BaseAddress);
        var secret = await _clientSecretProvider.GetSecret(_clientName);
        AddHeaderToHttpClient("Fig_IpAddress", () => _ipAddressResolver.Resolve());
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
        _logger.LogInformation("Registering custom actions with Fig API: {CustomActionNames}", customActions.Select(x => x.Name));

        var request = new CustomActionRegistrationRequestDataContract(_clientName, customActions);
        var secret = await _clientSecretProvider.GetSecret(_clientName);
        AddHeaderToHttpClient("clientSecret", () => secret);
        var json = JsonConvert.SerializeObject(request, JsonSettings.FigDefault);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/customactions/register", data);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully registered custom actions");
        }
        else
        {
            _logger.LogError("Failed to register custom actions. Status: {StatusCode}, Response: {Response}", response.StatusCode, await response.Content.ReadAsStringAsync());
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