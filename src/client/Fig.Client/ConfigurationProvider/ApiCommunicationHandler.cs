using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading; // Added for CancellationToken
using System.Threading.Tasks;
using Fig.Client.ClientSecret;
using Fig.Client.CustomActions; // Added for ICustomAction
using Fig.Client.Logging; // Added for IFigLogger
using Fig.Common.NetStandard.IpAddress;
using Fig.Common.NetStandard.Json; // Assuming this is where JsonSettings.FigDefault comes from. If not, adjust.
using Fig.Contracts;
using Fig.Contracts.CustomActions; // Added for custom action contracts
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Microsoft.Extensions.Options; // Added for FigOptions
using Fig.Client.Configuration; // Added for FigOptions POCO

// Note: The original used Newtonsoft.Json. If System.Text.Json is preferred for new methods,
// ensure consistency or provide specific instructions. This version will continue with Newtonsoft for simplicity
// unless specific System.Text.Json serialization/deserialization is required for new DTOs.
// For this task, we will assume Newtonsoft.Json is acceptable for new methods as well if not specified otherwise.
using Newtonsoft.Json; 

namespace Fig.Client.ConfigurationProvider;

// Implements the new V2 interface
public class ApiCommunicationHandlerV2 : IApiCommunicationHandlerV2
{
    private readonly HttpClient _httpClient;
    private readonly IFigLogger<ApiCommunicationHandlerV2> _logger; // Changed to IFigLogger
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly IClientSecretProvider _clientSecretProvider;
    private readonly FigOptions _options; // Added for FigOptions

    // Updated constructor signature
    public ApiCommunicationHandlerV2(HttpClient httpClient, 
                                     IFigLogger<ApiCommunicationHandlerV2> logger, 
                                     IIpAddressResolver ipAddressResolver, 
                                     IClientSecretProvider clientSecretProvider,
                                     IOptions<FigOptions> options) // Added IOptions<FigOptions>
    {
        _httpClient = httpClient;
        _logger = logger;
        _ipAddressResolver = ipAddressResolver;
        _clientSecretProvider = clientSecretProvider;
        _options = options.Value; // Store FigOptions

        if (_httpClient.BaseAddress == null && _options.ApiUri != null)
        {
            _httpClient.BaseAddress = _options.ApiUri;
        }
    }

    public async Task RegisterWithFigApi(string clientName, SettingsClientDefinitionDataContract settings)
    {
        _logger.LogInformation("Registering configuration with the Fig API at address {FigUri}", _httpClient.BaseAddress);
        var json = JsonConvert.SerializeObject(settings, JsonSettings.FigDefault);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        AddHeaderToHttpClient("ClientSecret", () => _clientSecretProvider.GetSecret(clientName));
        var result = await _httpClient.PostAsync("/clients", data); // Assuming API routes are relative to BaseAddress

        if (result.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully registered settings with Fig API");
        }
        else
        {
            var error = await GetErrorResult(result);
            // ErrorType might not exist on ErrorResultDataContract, check definition or use Message/StatusCode
            _logger.LogError(
                "Unable to successfully register settings. Code:{StatusCode}{NewLine}{Error}", result.StatusCode, Environment.NewLine, await result.Content.ReadAsStringAsync());
        }
    }

    public async Task<List<SettingDataContract>> RequestConfiguration(string clientName, string? instance, Guid runSessionId)
    {
        _logger.LogDebug("Fig: Reading settings from API at address {ApiUri}...", _httpClient.BaseAddress);
        AddHeaderToHttpClient("Fig_IpAddress", () => _ipAddressResolver.Resolve());
        AddHeaderToHttpClient("Fig_Hostname", () => Environment.MachineName);
        AddHeaderToHttpClient("clientSecret", () => _clientSecretProvider.GetSecret(clientName));

        var uri = $"/clients/{Uri.EscapeDataString(clientName)}/settings";
        uri += $"?runSessionId={runSessionId}";
        if (!string.IsNullOrEmpty(instance))
            uri += $"&instance={Uri.EscapeDataString(instance)}";

        var response = await _httpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode(); // Throw on error
        var resultString = await response.Content.ReadAsStringAsync();
        
        var settingValues =
            (JsonConvert.DeserializeObject<IEnumerable<SettingDataContract>>(resultString, JsonSettings.FigDefault) ??
             Array.Empty<SettingDataContract>()).ToList();

        return settingValues;
    }

    // --- NEW CUSTOM ACTION METHODS ---

    public async Task RegisterCustomActions(IEnumerable<ICustomAction> customActions, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering custom actions with Fig API.");
        var contractActions = customActions.Select(action => new CustomActionDefinitionDataContract
        {
            Name = action.Name,
            ButtonName = action.ButtonName,
            Description = action.Description,
            SettingsUsed = new List<SettingDefinitionDataContract>(action.SettingsUsed) // Ensure deep copy if modification is possible
        }).ToList();

        var request = new CustomActionRegistrationRequestDataContract
        {
            ClientName = _options.ClientName,
            Instance = _options.Instance,
            CustomActions = contractActions
        };

        var json = JsonConvert.SerializeObject(request, JsonSettings.FigDefault);
        var data = new StringContent(json, Encoding.UTF8, "application/json");
        AddClientSecretHeader(_options.ClientName);

        try
        {
            var response = await _httpClient.PostAsync("/customactions/register", data, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully registered custom actions.");
            }
            else
            {
                _logger.LogError("Failed to register custom actions. Status: {StatusCode}, Response: {Response}", response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering custom actions.");
            // Optionally rethrow or handle as per application's error handling strategy
        }
    }

    public async Task<IEnumerable<CustomActionClientPollResponseDataContract>?> PollForCustomActionRequests(CancellationToken cancellationToken)
    {
        var pollUri = $"/customactions/poll/{Uri.EscapeDataString(_options.ClientName ?? string.Empty)}";
        if (!string.IsNullOrWhiteSpace(_options.Instance))
        {
            pollUri += $"/{Uri.EscapeDataString(_options.Instance)}";
        }
        _logger.LogDebug("Polling for custom action requests at {PollUri}", pollUri);
        AddClientSecretHeader(_options.ClientName);

        try
        {
            var response = await _httpClient.GetAsync(pollUri, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("No pending custom actions found (404).");
                return Enumerable.Empty<CustomActionClientPollResponseDataContract>();
            }
            
            response.EnsureSuccessStatusCode(); // Throw for other errors

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<IEnumerable<CustomActionClientPollResponseDataContract>>(content, JsonSettings.FigDefault);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
             _logger.LogDebug("No pending custom actions found (HttpRequestException with 404).");
             return Enumerable.Empty<CustomActionClientPollResponseDataContract>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling for custom action requests.");
            return null; // Or rethrow, depending on desired error handling
        }
    }

    public async Task SendCustomActionResults(CustomActionClientExecuteRequestDataContract results, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending custom action results for ExecutionId: {ExecutionId}", results.ExecutionId);
        var json = JsonConvert.SerializeObject(results, JsonSettings.FigDefault);
        var data = new StringContent(json, Encoding.UTF8, "application/json");
        AddClientSecretHeader(_options.ClientName);
        
        try
        {
            var response = await _httpClient.PostAsync("/customactions/results", data, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent custom action results for ExecutionId: {ExecutionId}", results.ExecutionId);
            }
            else
            {
                _logger.LogError("Failed to send custom action results for ExecutionId: {ExecutionId}. Status: {StatusCode}, Response: {Response}", results.ExecutionId, response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending custom action results for ExecutionId: {ExecutionId}", results.ExecutionId);
            // Optionally rethrow
        }
    }

    // --- END NEW CUSTOM ACTION METHODS ---

    private void AddHeaderToHttpClient(string key, Func<string> getValue)
    {
        // Remove existing header before adding, to avoid issues if this method is called multiple times.
        _httpClient.DefaultRequestHeaders.Remove(key);
        _httpClient.DefaultRequestHeaders.Add(key, getValue());
    }
    
    private void AddClientSecretHeader(string clientName)
    {
        var secret = _clientSecretProvider.GetSecret(clientName);
        if (secret != null)
        {
            AddHeaderToHttpClient("ClientSecret", () => secret);
        }
        else
        {
            _logger.LogWarning("Client secret for '{ClientName}' is null. 'ClientSecret' header not added.", clientName);
        }
    }

    private async Task<ErrorResultDataContract?> GetErrorResult(HttpResponseMessage response)
    {
        ErrorResultDataContract? errorContract = null;
        if (!response.IsSuccessStatusCode)
        {
            var resultString = await response.Content.ReadAsStringAsync();
            try
            {
                 errorContract = JsonConvert.DeserializeObject<ErrorResultDataContract>(resultString);
                 if (errorContract?.Message == null && errorContract?.Reference == null) // check if deserialization was meaningful
                 {
                    errorContract = new ErrorResultDataContract("Unknown", response.StatusCode.ToString(), resultString, null);
                 }
            }
            catch (JsonException) // Catch if not valid JSON
            {
                 errorContract = new ErrorResultDataContract("Unknown", response.StatusCode.ToString(), resultString, null);
            }
        }
        return errorContract;
    }
}
