using System;
using System.Collections.Generic;
using Fig.Client.ClientSecret;
using Fig.Client.Exceptions;
using Fig.Common.NetStandard.Cryptography;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Settings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Client.OfflineSettings;

public class OfflineSettingsManager : IOfflineSettingsManager
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

    public void Save(string clientName, IEnumerable<SettingDataContract> settings)
    {
        var container = new OfflineSettingContainer(DateTime.UtcNow, settings);

        var json = JsonConvert.SerializeObject(container, JsonSettings.FigDefault);
        var clientSecret = _clientSecretProvider.GetSecret(clientName);
        var encrypted = _cryptography.Encrypt(clientSecret.Read(), json);
        _binaryFile.Write(clientName, encrypted);

        _logger.LogDebug($"Saved offline settings for client {clientName}");
    }

    public IEnumerable<SettingDataContract>? Get(string clientName)
    {
        _logger.LogInformation($"Attempting to read offline settings for client {clientName}");

        var encryptedData = _binaryFile.Read(clientName);

        if (encryptedData is null)
        {
            _logger.LogWarning("No local data file. Only default settings can be used.");
            return null;
        }
        
        var clientSecret = _clientSecretProvider.GetSecret(clientName);
        try
        {
            var data = _cryptography.Decrypt(clientSecret.Read(), encryptedData);
            var settings = JsonConvert.DeserializeObject<OfflineSettingContainer>(data, JsonSettings.FigDefault);

            if (settings is null)
            {
                _logger.LogWarning("If you have changed your client name or secret, delete file at {FilePath} and then run again when Fig API is available", _binaryFile.GetFilePath(clientName));
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
            Delete(clientName);
            return null;
        }
    }

    public void Delete(string clientName)
    {
        _binaryFile.Delete(clientName);
    }
}