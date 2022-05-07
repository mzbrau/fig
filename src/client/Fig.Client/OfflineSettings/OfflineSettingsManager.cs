using System;
using System.Collections.Generic;
using Fig.Client.Exceptions;
using Fig.Contracts.Settings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Client.OfflineSettings
{
    public class OfflineSettingsManager : IOfflineSettingsManager
    {
        private readonly ICryptography _cryptography;
        private readonly IBinaryFile _binaryFile;
        private readonly ILogger _logger;

        public OfflineSettingsManager(
            ICryptography cryptography,
            IBinaryFile binaryFile,
            ILogger logger)
        {
            _cryptography = cryptography;
            _binaryFile = binaryFile;
            _logger = logger;
        }
        
        public void Save(string clientName, IEnumerable<SettingDataContract> settings)
        {
            var container = new OfflineSettingContainer()
            {
                PersistedUtc = DateTime.UtcNow,
                Settings = settings
            };

            var json = JsonConvert.SerializeObject(container);
            var encrypted = _cryptography.Encrypt(clientName, json);
            _binaryFile.Write(clientName, encrypted);

            _logger.LogDebug($"Saved offline settings for client {clientName}");
        }

        public IEnumerable<SettingDataContract> Get(string clientName)
        {
            _logger.LogInformation($"Attempting to read offline settings for client {clientName}");

            var encryptedData = _binaryFile.Read(clientName);

            if (encryptedData is null)
                throw new NoOfflineSettingsException();

            var data = _cryptography.Decrypt(clientName, encryptedData);
            var settings = JsonConvert.DeserializeObject<OfflineSettingContainer>(data);
            
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
}