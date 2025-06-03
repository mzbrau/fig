using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Fig.Client.Contracts;

namespace Fig.Client.Aws
{
    public class AwsSecretProvider : IClientSecretProvider, IDisposable
    {
        private const string SecretKeyFormat = "FIG_{0}_SECRET";
        private readonly bool _autoCreate;
        private readonly IAmazonSecretsManager _client;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private string? _secret;
        
        /// <summary>
        /// Creates a new AWS Secrets Manager secret provider using default credentials chain.
        /// </summary>
        /// <param name="regionEndpoint">The AWS region where secrets are stored</param>
        /// <param name="autoCreate">Whether to automatically create secrets if they don't exist</param>
        public AwsSecretProvider(RegionEndpoint regionEndpoint, bool autoCreate = true)
        {
            _autoCreate = autoCreate;
            _client = new AmazonSecretsManagerClient(regionEndpoint);
        }

        /// <summary>
        /// Creates a new AWS Secrets Manager secret provider with custom client.
        /// </summary>
        /// <param name="client">Custom AWS Secrets Manager client (useful for testing or custom configuration)</param>
        /// <param name="autoCreate">Whether to automatically create secrets if they don't exist</param>
        public AwsSecretProvider(IAmazonSecretsManager client, bool autoCreate = true)
        {
            _autoCreate = autoCreate;
            _client = client;
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
                var response = await _client.GetSecretValueAsync(new GetSecretValueRequest
                {
                    SecretId = secretKey
                });
                
                if (!string.IsNullOrEmpty(response.SecretString))
                {
                    _secret = response.SecretString;
                    return _secret;
                }
            }
            catch (ResourceNotFoundException)
            {
                // Secret doesn't exist, proceed to create if auto-create is enabled
            }
            catch (AmazonSecretsManagerException ex) when (IsTransientError(ex))
            {
                // For transient errors, try one more time after a brief delay
                await Task.Delay(1000);
                try
                {
                    var response = await _client.GetSecretValueAsync(new GetSecretValueRequest
                    {
                        SecretId = secretKey
                    });
                    
                    if (!string.IsNullOrEmpty(response.SecretString))
                    {
                        _secret = response.SecretString;
                        return _secret;
                    }
                }
                catch (ResourceNotFoundException)
                {
                    // Secret still doesn't exist after retry
                }
            }

            if (!_autoCreate)
            {
                throw new SecretNotFoundException($"No secret found in AWS Secrets Manager with key: {secretKey}");
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
                    await _client.CreateSecretAsync(new CreateSecretRequest
                    {
                        Name = secretKey,
                        SecretString = newSecret,
                        Description = $"Fig client secret for {secretKey}"
                    });
                    
                    // Verify the secret was created successfully by reading it back
                    var verifyResponse = await _client.GetSecretValueAsync(new GetSecretValueRequest
                    {
                        SecretId = secretKey
                    });
                    
                    if (verifyResponse.SecretString == newSecret)
                    {
                        _secret = newSecret;
                        return _secret;
                    }
                    
                    // If verification failed, treat as if someone else created it first
                    if (!string.IsNullOrEmpty(verifyResponse.SecretString))
                    {
                        _secret = verifyResponse.SecretString;
                        return _secret;
                    }
                }
                catch (ResourceExistsException)
                {
                    // Conflict - another process may have created the secret concurrently
                    // Try to retrieve the existing secret
                    try
                    {
                        var existingResponse = await _client.GetSecretValueAsync(new GetSecretValueRequest
                        {
                            SecretId = secretKey
                        });
                        
                        if (!string.IsNullOrEmpty(existingResponse.SecretString))
                        {
                            _secret = existingResponse.SecretString;
                            return _secret;
                        }
                    }
                    catch (AmazonSecretsManagerException)
                    {
                        // If we can't read the secret that supposedly exists, continue retrying
                    }
                }
                catch (AmazonSecretsManagerException ex) when (IsTransientError(ex) && attempt < maxRetries)
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

        private static bool IsTransientError(AmazonSecretsManagerException ex)
        {
            return ex.ErrorCode == "Throttling" ||
                   ex.ErrorCode == "ServiceUnavailable" ||
                   ex.ErrorCode == "InternalServiceError" ||
                   (int)ex.StatusCode == 429 || // Too Many Requests
                   ex.StatusCode == System.Net.HttpStatusCode.InternalServerError ||
                   ex.StatusCode == System.Net.HttpStatusCode.BadGateway ||
                   ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                   ex.StatusCode == System.Net.HttpStatusCode.GatewayTimeout;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _semaphore?.Dispose();
                _client?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}