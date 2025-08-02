using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fig.Client.Contracts;
using Fig.Common.NetStandard.Cryptography;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Settings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Client.OfflineSettings;

internal class OfflineSettingsManager : IOfflineSettingsManager
{
    private readonly IBinaryFile _binaryFile;
    private readonly IClientSecretProvider _clientSecretProvider;
    private readonly ICryptography _cryptography;
    private readonly ILogger<OfflineSettingsManager> _logger;

    public OfflineSettingsManager(
        ICryptography cryptography,
        IBinaryFile binaryFile,
        IClientSecretProvider clientSecretProvider,
        ILogger<OfflineSettingsManager> logger)
    {
        _cryptography = cryptography;
        _binaryFile = binaryFile;
        _clientSecretProvider = clientSecretProvider;
        _logger = logger;
    }

    public async Task Save(string clientName, string? instance, IEnumerable<SettingDataContract> settings)
    {
        try
        {
            var container = new OfflineSettingContainer(DateTime.UtcNow, settings);

            var json = JsonConvert.SerializeObject(container, JsonSettings.FigDefault);
            var clientSecret = await _clientSecretProvider.GetSecret(clientName);
            var encrypted = _cryptography.Encrypt(clientSecret, json);
            _binaryFile.Write(clientName, instance, encrypted);

            _logger.LogDebug("Saved offline settings for client {ClientName}", clientName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to save offline settings");
        }
    }

    public async Task<IEnumerable<SettingDataContract>?> Get(string clientName, string? instance)
    {
        _logger.LogInformation("Attempting to read offline settings for client {ClientName}", clientName);

        string? encryptedData;
        try
        {
            encryptedData = _binaryFile.Read(clientName, instance);

            if (encryptedData is null)
            {
                _logger.LogWarning("No local Fig data file. Only default settings can be used.");
                return null;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to read offline settings");
            return null;
        }

        var clientSecret = await _clientSecretProvider.GetSecret(clientName);

        try
        {
            var data = _cryptography.Decrypt(clientSecret, encryptedData);
            var settings = JsonConvert.DeserializeObject<OfflineSettingContainer>(data, JsonSettings.FigDefault);

            if (settings is null)
            {
                _logger.LogWarning("If you have changed your client name or secret, delete file at {FilePath} and then run again when Fig API is available", _binaryFile.GetFilePath(clientName, instance));
                return null;
            }

            _logger.LogInformation($"Read offline settings for client {clientName} that were persisted " +
                                   $"{Math.Round((DateTime.UtcNow - settings.PersistedUtc).TotalMinutes)} " +
                                   $"minutes ago ({settings.PersistedUtc} UTC).");

            return settings.Settings;
        }
        catch (Exception)
        {
            _logger.LogWarning("Unable to read decrypted settings, client secret may have changed. Only default settings can be used.");
            Delete(clientName, instance);
            return null;
        }
    }

    public void Delete(string clientName, string? instance)
    {
        try
        {
            _binaryFile.Delete(clientName, instance);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete offline settings");
        }
    }
}