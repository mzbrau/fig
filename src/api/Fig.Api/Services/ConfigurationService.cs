using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.Secrets;
using Fig.Common.Constants;
using Fig.Contracts.Configuration;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class ConfigurationService : AuthenticatedService, IConfigurationService
{
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly IFigConfigurationConverter _figConfigurationConverter;
    private readonly ISecretStore _secretStore;
    private readonly IEncryptionService _encryptionService;
    private readonly IHttpClientFactory _httpClientFactory;

    public ConfigurationService(IConfigurationRepository configurationRepository,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        IFigConfigurationConverter figConfigurationConverter,
        ISecretStore secretStore,
        IEncryptionService encryptionService,
        IHttpClientFactory httpClientFactory)
    {
        _configurationRepository = configurationRepository;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _figConfigurationConverter = figConfigurationConverter;
        _secretStore = secretStore;
        _encryptionService = encryptionService;
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<FigConfigurationDataContract> GetConfiguration()
    {
        var configuration = await _configurationRepository.GetConfiguration();
        return _figConfigurationConverter.Convert(configuration);
    }

    public async Task UpdateConfiguration(FigConfigurationDataContract configuration)
    {
        var currentConfiguration = await _configurationRepository.GetConfiguration(true);
        var currentDataContract = _figConfigurationConverter.Convert(currentConfiguration);
        var incomingToken = configuration.FigAssistantAccessToken;

        var compareCurrent = CloneForCompare(currentDataContract);
        var compareIncoming = CloneForCompare(configuration);
        NormalizeAssistantTokenForCompare(compareCurrent);
        NormalizeAssistantTokenForCompare(compareIncoming);

        if (JsonConvert.SerializeObject(compareCurrent) == JsonConvert.SerializeObject(compareIncoming))
        {
            return;
        }

        await _eventLogRepository.Add(_eventLogFactory.ConfigurationChanged(currentDataContract, configuration, AuthenticatedUser));
        ApplyAssistantAccessToken(currentConfiguration, incomingToken);
        currentConfiguration.Update(configuration);
        await _configurationRepository.UpdateConfiguration(currentConfiguration);
    }

    public async Task<SecretStoreTestResultDataContract> TestAzureKeyVault()
    {
        return await _secretStore.PerformTest();
    }

    public async Task<SecretStoreTestResultDataContract> TestFigAssistant()
    {
        var configuration = await _configurationRepository.GetConfiguration();
        if (string.IsNullOrWhiteSpace(configuration.FigAssistantEndpoint))
            return new SecretStoreTestResultDataContract(false, "Fig Assistant endpoint is not configured.");

        if (string.IsNullOrWhiteSpace(configuration.FigAssistantModel))
            return new SecretStoreTestResultDataContract(false, "Fig Assistant model is not configured.");

        var token = _encryptionService.Decrypt(configuration.FigAssistantAccessTokenEncrypted, throwOnFailure: false);
        if (string.IsNullOrWhiteSpace(token) || token == configuration.FigAssistantAccessTokenEncrypted)
            return new SecretStoreTestResultDataContract(false, "Fig Assistant access token is not configured.");

        try
        {
            var client = _httpClientFactory.CreateClient("FigAssistant");
            var baseUrl = configuration.FigAssistantEndpoint.TrimEnd('/');
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/models");
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return new SecretStoreTestResultDataContract(true, "Successfully connected to the LLM endpoint.");

            var body = await response.Content.ReadAsStringAsync();
            var message = string.IsNullOrWhiteSpace(body)
                ? $"LLM endpoint returned {(int)response.StatusCode} {response.ReasonPhrase}."
                : $"LLM endpoint returned {(int)response.StatusCode}: {Truncate(body, 300)}";
            return new SecretStoreTestResultDataContract(false, message);
        }
        catch (Exception ex)
        {
            return new SecretStoreTestResultDataContract(false, $"Failed to reach LLM endpoint: {ex.Message}");
        }
    }

    private void ApplyAssistantAccessToken(
        Fig.Datalayer.BusinessEntities.FigConfigurationBusinessEntity entity,
        string? incomingToken)
    {
        if (string.IsNullOrWhiteSpace(incomingToken))
        {
            entity.FigAssistantAccessTokenEncrypted = null;
            return;
        }

        if (incomingToken == SecretConstants.SecretPlaceholder)
            return;

        entity.FigAssistantAccessTokenEncrypted = _encryptionService.Encrypt(incomingToken);
    }

    private static void NormalizeAssistantTokenForCompare(FigConfigurationDataContract configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration.FigAssistantAccessToken) &&
            configuration.FigAssistantAccessToken != SecretConstants.SecretPlaceholder)
        {
            // Treat a newly provided token as changed by using a distinct sentinel for compare.
            configuration.FigAssistantAccessToken = $"__NEW_TOKEN__{configuration.FigAssistantAccessToken.Length}";
        }
    }

    private static FigConfigurationDataContract CloneForCompare(FigConfigurationDataContract configuration)
    {
        return JsonConvert.DeserializeObject<FigConfigurationDataContract>(
                   JsonConvert.SerializeObject(configuration))
               ?? configuration;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "…";
    }
}
