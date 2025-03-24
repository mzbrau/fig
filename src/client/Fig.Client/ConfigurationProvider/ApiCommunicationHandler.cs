using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fig.Client.ClientSecret;
using Fig.Common.NetStandard.IpAddress;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Client.ConfigurationProvider;

public class ApiCommunicationHandler : IApiCommunicationHandler
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiCommunicationHandler> _logger;
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly IClientSecretProvider _clientSecretProvider;

    internal ApiCommunicationHandler(HttpClient httpClient, ILogger<ApiCommunicationHandler> logger, IIpAddressResolver ipAddressResolver, IClientSecretProvider clientSecretProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _ipAddressResolver = ipAddressResolver;
        _clientSecretProvider = clientSecretProvider;
    }

    public async Task RegisterWithFigApi(string clientName, SettingsClientDefinitionDataContract settings)
    {
        _logger.LogInformation("Registering configuration with the Fig API at address {FigUri}", _httpClient.BaseAddress);
        var json = JsonConvert.SerializeObject(settings, JsonSettings.FigDefault);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        AddHeaderToHttpClient("ClientSecret", () => _clientSecretProvider.GetSecret(clientName));
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

    public async Task<List<SettingDataContract>> RequestConfiguration(string clientName, string? instance, Guid runSessionId)
    {
        _logger.LogDebug("Fig: Reading settings from API at address {OptionsApiUri}...", _httpClient.BaseAddress);
        AddHeaderToHttpClient("Fig_IpAddress", () => _ipAddressResolver.Resolve());
        AddHeaderToHttpClient("Fig_Hostname", () => Environment.MachineName);
        AddHeaderToHttpClient("clientSecret", () => _clientSecretProvider.GetSecret(clientName));

        var uri = $"/clients/{Uri.EscapeDataString(clientName)}/settings";
        uri += $"?runSessionId={runSessionId}";
        if (!string.IsNullOrEmpty(instance))
            uri += $"&instance={Uri.EscapeDataString(instance)}";

        var result = await _httpClient.GetStringAsync(uri);

        var settingValues =
            (JsonConvert.DeserializeObject<IEnumerable<SettingDataContract>>(result, JsonSettings.FigDefault) ??
             Array.Empty<SettingDataContract>()).ToList();

        return settingValues;
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