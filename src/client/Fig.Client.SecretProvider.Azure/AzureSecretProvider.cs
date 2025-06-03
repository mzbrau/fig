using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Fig.Client.Contracts;
using Microsoft.Extensions.Logging;

namespace Fig.Client.Azure
{
    public class AzureSecretProvider : ClientSecretProviderBase<AzureSecretProvider>
    {
        private readonly SecretClient _client;

        public AzureSecretProvider(string keyVaultUri)
            : base("Azure")
        {
            _client = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
        }

        protected override async Task<string> GetOrCreateSecretInternal(string clientName)
        {
            var secretKey = string.Format(SecretKeyFormat, clientName.Replace(" ", "").ToUpper());
            Logger?.LogDebug("Attempting to retrieve Azure secret for key {SecretKey} (client: {ClientName})",
                secretKey, clientName);
            try
            {
                var response = await _client.GetSecretAsync(secretKey);
                if (response?.Value?.Value != null && !string.IsNullOrEmpty(response.Value.Value))
                {
                    SecretCache[clientName] = response.Value.Value;
                    Logger?.LogInformation(
                        "Successfully retrieved Azure secret for key {SecretKey} (client: {ClientName})", secretKey,
                        clientName);
                    return SecretCache[clientName];
                }
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                Logger?.LogDebug("Azure secret not found for key {SecretKey} (client: {ClientName})", secretKey,
                    clientName);
            }
            catch (RequestFailedException ex) when (IsTransientError(ex))
            {
                Logger?.LogWarning("Transient Azure error retrieving secret {SecretKey}: {Error} Retrying", secretKey,
                    ex.Message);
                await Task.Delay(1000);
                try
                {
                    var response = await _client.GetSecretAsync(secretKey);
                    if (response?.Value?.Value != null && !string.IsNullOrEmpty(response.Value.Value))
                    {
                        SecretCache[clientName] = response.Value.Value;
                        Logger?.LogInformation(
                            "Successfully retrieved Azure secret for key {SecretKey} (client: {ClientName}) after retry",
                            secretKey, clientName);
                        return SecretCache[clientName];
                    }
                }
                catch (RequestFailedException retryEx) when (retryEx.Status == 404)
                {
                    Logger?.LogDebug("Azure secret not found for key {SecretKey} after retry (client: {ClientName})",
                        secretKey, clientName);
                }
            }

            if (!AutoCreate)
            {
                Logger?.LogError("No secret found in Azure Key Vault with key: {SecretKey} (client: {ClientName})",
                    secretKey, clientName);
                throw new SecretNotFoundException($"No secret found in Azure Key Vault with key: {secretKey}");
            }

            return await CreateSecretWithRetry(clientName, secretKey);
        }

        private async Task<string> CreateSecretWithRetry(string clientName, string secretKey)
        {
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var newSecret = Guid.NewGuid().ToString();
                    Logger?.LogDebug("Attempting to create Azure secret {SecretKey} (attempt {Attempt})", secretKey,
                        attempt);
                    await _client.SetSecretAsync(secretKey, newSecret);
                    var verifyResponse = await _client.GetSecretAsync(secretKey);
                    if (verifyResponse?.Value?.Value == newSecret)
                    {
                        SecretCache[clientName] = newSecret;
                        Logger?.LogInformation(
                            "Successfully created Azure secret for key {SecretKey} (client: {ClientName})", secretKey,
                            clientName);
                        return SecretCache[clientName];
                    }

                    if (!string.IsNullOrEmpty(verifyResponse?.Value?.Value))
                    {
                        Logger?.LogWarning(
                            "Azure secret {SecretKey} was created concurrently by another process. Using existing value",
                            secretKey);
                        SecretCache[clientName] = verifyResponse!.Value!.Value;
                        return SecretCache[clientName];
                    }
                }
                catch (RequestFailedException ex) when (ex.Status == 409)
                {
                    Logger?.LogWarning("Azure secret {SecretKey} already exists. Attempting to retrieve existing value",
                        secretKey);
                    try
                    {
                        var existingResponse = await _client.GetSecretAsync(secretKey);
                        if (existingResponse?.Value?.Value != null &&
                            !string.IsNullOrEmpty(existingResponse.Value.Value))
                        {
                            SecretCache[clientName] = existingResponse.Value.Value;
                            Logger?.LogInformation(
                                "Successfully retrieved existing Azure secret for key {SecretKey} after conflict (client: {ClientName})",
                                secretKey, clientName);
                            return SecretCache[clientName];
                        }
                    }
                    catch (RequestFailedException ex2)
                    {
                        Logger?.LogError("Failed to read existing Azure secret {SecretKey} after conflict: {Error}",
                            secretKey, ex2.Message);
                    }
                }
                catch (RequestFailedException ex) when (IsTransientError(ex) && attempt < maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    Logger?.LogWarning(
                        "Transient Azure error creating secret {SecretKey}: {Error} Retrying in {DelaySeconds} seconds",
                        secretKey, ex.Message, delay.TotalSeconds);
                    await Task.Delay(delay);
                    continue;
                }

                if (attempt == maxRetries)
                {
                    Logger?.LogError(
                        "Failed to create or retrieve Azure secret {SecretKey} after {MaxRetries} attempts", secretKey,
                        maxRetries);
                    throw new InvalidOperationException(
                        $"Failed to create or retrieve secret '{secretKey}' after {maxRetries} attempts");
                }
            }

            Logger?.LogError("Unexpected error creating Azure secret {SecretKey}", secretKey);
            throw new InvalidOperationException($"Unexpected error creating secret '{secretKey}'");
        }

        private static bool IsTransientError(RequestFailedException ex)
        {
            return ex.Status == 429 || // Too Many Requests
                   ex.Status == 500 || // Internal Server Error
                   ex.Status == 502 || // Bad Gateway
                   ex.Status == 503 || // Service Unavailable
                   ex.Status == 504; // Gateway Timeout
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Semaphore?.Dispose();
            }
        }

        public override void Dispose()
        {
            Dispose(true);
            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}