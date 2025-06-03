using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Fig.Client.Contracts;

namespace Fig.Client.Azure
{
    public class AzureSecretProvider : IClientSecretProvider, IDisposable
    {
        private const string SecretKeyFormat = "FIG_{0}_SECRET";
        private readonly bool _autoCreate;
        private readonly SecretClient _client;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private string? _secret;
        
        public AzureSecretProvider(string keyVaultUri, bool autoCreate = true)
        {
            _autoCreate = autoCreate;
            _client = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
        }

        public async Task<string> GetSecret(string clientName)
        {
            if (!string.IsNullOrEmpty(_secret))
                return _secret!;

            // Use semaphore to prevent race conditions when multiple threads try to create the same secret
            await _semaphore.WaitAsync();
            try
            {
                // Double-check pattern: verify secret wasn't created by another thread while waiting
                if (!string.IsNullOrEmpty(_secret))
                    return _secret!;

                return await GetOrCreateSecretInternal(clientName);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<string> GetOrCreateSecretInternal(string clientName)
        {
            var secretKey = string.Format(SecretKeyFormat, clientName);
            
            try
            {
                // Try to get existing secret first
                var response = await _client.GetSecretAsync(secretKey);
                if (response?.Value?.Value != null && !string.IsNullOrEmpty(response.Value.Value))
                {
                    _secret = response.Value.Value;
                    return _secret;
                }
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Secret doesn't exist, proceed to create if auto-create is enabled
            }
            catch (RequestFailedException ex) when (IsTransientError(ex))
            {
                // For transient errors, try one more time after a brief delay
                await Task.Delay(1000);
                try
                {
                    var response = await _client.GetSecretAsync(secretKey);
                    if (response?.Value?.Value != null && !string.IsNullOrEmpty(response.Value.Value))
                    {
                        _secret = response.Value.Value;
                        return _secret;
                    }
                }
                catch (RequestFailedException retryEx) when (retryEx.Status == 404)
                {
                    // Secret still doesn't exist after retry
                }
            }

            if (!_autoCreate)
            {
                throw new SecretNotFoundException($"No secret found in Azure Key Vault with key: {secretKey}");
            }

            return await CreateSecretWithRetry(secretKey);
        }

        private async Task<string> CreateSecretWithRetry(string secretKey)
        {
            const int maxRetries = 3;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Generate a new secret value
                    var newSecret = Guid.NewGuid().ToString();
                    
                    // Attempt to create the secret
                    await _client.SetSecretAsync(secretKey, newSecret);
                    
                    // Verify the secret was created successfully by reading it back
                    var verifyResponse = await _client.GetSecretAsync(secretKey);
                    if (verifyResponse?.Value?.Value == newSecret)
                    {
                        _secret = newSecret;
                        return _secret;
                    }
                    
                    // If verification failed, treat as if someone else created it first
                    if (!string.IsNullOrEmpty(verifyResponse?.Value?.Value))
                    {
                        _secret = verifyResponse!.Value!.Value;
                        return _secret;
                    }
                }
                catch (RequestFailedException ex) when (ex.Status == 409)
                {
                    // Conflict - another process may have created the secret concurrently
                    // Try to retrieve the existing secret
                    try
                    {
                        var existingResponse = await _client.GetSecretAsync(secretKey);
                        if (existingResponse?.Value?.Value != null && !string.IsNullOrEmpty(existingResponse.Value.Value))
                        {
                            _secret = existingResponse.Value.Value;
                            return _secret;
                        }
                    }
                    catch (RequestFailedException)
                    {
                        // If we can't read the secret that supposedly exists, continue retrying
                    }
                }
                catch (RequestFailedException ex) when (IsTransientError(ex) && attempt < maxRetries)
                {
                    // Wait before retrying with exponential backoff
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                    await Task.Delay(delay);
                    continue;
                }

                if (attempt == maxRetries)
                {
                    throw new InvalidOperationException(
                        $"Failed to create or retrieve secret '{secretKey}' after {maxRetries} attempts");
                }
            }

            throw new InvalidOperationException($"Unexpected error creating secret '{secretKey}'");
        }

        private static bool IsTransientError(RequestFailedException ex)
        {
            return ex.Status == 429 ||  // Too Many Requests
                   ex.Status == 500 ||  // Internal Server Error
                   ex.Status == 502 ||  // Bad Gateway
                   ex.Status == 503 ||  // Service Unavailable
                   ex.Status == 504;    // Gateway Timeout
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _semaphore?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}