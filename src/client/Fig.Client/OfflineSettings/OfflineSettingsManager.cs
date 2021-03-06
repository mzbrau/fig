using System;
using System.Collections.Generic;
using Fig.Client.ClientSecret;
using Fig.Client.Exceptions;
using Fig.Common.Cryptography;
using Fig.Contracts.Settings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Client.OfflineSettings;

public class OfflineSettingsManager : IOfflineSettingsManager
{
    private readonly IBinaryFile _binaryFile;
    private readonly IClientSecretProvider _clientSecretProvider;
    private readonly ICryptography _cryptography;
    private readonly ILogger _logger;

    public OfflineSettingsManager(
        ICryptography cryptography,
        IBinaryFile binaryFile,
        IClientSecretProvider clientSecretProvider,
        ILogger logger)
    {
        _cryptography = cryptography;
        _binaryFile = binaryFile;
        _clientSecretProvider = clientSecretProvider;
        _logger = logger;
    }

    public void Save(string clientName, IEnumerable<SettingDataContract> settings)
    {
        var container = new OfflineSettingContainer(DateTime.UtcNow, settings);

        var json = JsonConvert.SerializeObject(container);
        var clientSecret = _clientSecretProvider.GetSecret(clientName);
        var encrypted = _cryptography.Encrypt(clientSecret, json);
        _binaryFile.Write(clientName, encrypted);

        _logger.LogDebug($"Saved offline settings for client {clientName}");
    }

    public IEnumerable<SettingDataContract> Get(string clientName)
    {
        _logger.LogInformation($"Attempting to read offline settings for client {clientName}");

        var encryptedData = _binaryFile.Read(clientName);

        if (encryptedData is null)
            throw new NoOfflineSettingsException();

        var clientSecret = _clientSecretProvider.GetSecret(clientName);
        var data = _cryptography.Decrypt(clientSecret, encryptedData);
        var settings = JsonConvert.DeserializeObject<OfflineSettingContainer>(data);

        if (settings is null)
            throw new NoOfflineSettingsException();
        
        _logger.LogInformation($"Read offline settings for client {clientName} that were persisted " +
                               $"{Math.Round((DateTime.UtcNow - settings.PersistedUtc).TotalMinutes)} " +
                               $"minutes ago ({settings.PersistedUtc} UTC).");

        return settings.Settings;
    }

    public void Delete(string clientName)
    {
        _binaryFile.Delete(clientName);
    }
}