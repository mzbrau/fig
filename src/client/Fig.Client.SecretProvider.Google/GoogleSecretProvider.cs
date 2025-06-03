using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.SecretManager.V1;
using Google.Protobuf;
using Grpc.Core;
using Fig.Client.Contracts;

namespace Fig.Client.Google
{
    public class GoogleSecretProvider : IClientSecretProvider, IDisposable
    {
        private const string SecretKeyFormat = "FIG_{0}_SECRET";
        private readonly bool _autoCreate;
        private readonly SecretManagerServiceClient _client;
        private readonly string _projectId;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private string? _secret;
        
        /// <summary>
        /// Creates a new Google Cloud Secret Manager secret provider using default credentials.
        /// </summary>
        /// <param name="projectId">The Google Cloud project ID where secrets are stored</param>
        /// <param name="autoCreate">Whether to automatically create secrets if they don't exist</param>
        public GoogleSecretProvider(string projectId, bool autoCreate = true)
        {
            _projectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
            _autoCreate = autoCreate;
            _client = SecretManagerServiceClient.Create();
        }

        /// <summary>
        /// Creates a new Google Cloud Secret Manager secret provider with custom client.
        /// </summary>
        /// <param name="client">Custom Secret Manager client (useful for testing or custom configuration)</param>
        /// <param name="projectId">The Google Cloud project ID where secrets are stored</param>
        /// <param name="autoCreate">Whether to automatically create secrets if they don't exist</param>
        public GoogleSecretProvider(SecretManagerServiceClient client, string projectId, bool autoCreate = true)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _projectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
            _autoCreate = autoCreate;
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
            var secretName = SecretName.FromProjectSecret(_projectId, secretKey);
            
            try
            {
                // Try to get existing secret first
                var response = await _client.AccessSecretVersionAsync(new AccessSecretVersionRequest
                {
                    Name = SecretVersionName.FromProjectSecretSecretVersion(_projectId, secretKey, "latest").ToString()
                });
                
                if (response?.Payload?.Data != null)
                {
                    _secret = response.Payload.Data.ToStringUtf8();
                    if (!string.IsNullOrEmpty(_secret))
                        return _secret;
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                // Secret doesn't exist, proceed to create if auto-create is enabled
            }
            catch (RpcException ex) when (IsTransientError(ex))
            {
                // For transient errors, try one more time after a brief delay
                await Task.Delay(1000);
                try
                {
                    var response = await _client.AccessSecretVersionAsync(new AccessSecretVersionRequest
                    {
                        Name = SecretVersionName.FromProjectSecretSecretVersion(_projectId, secretKey, "latest").ToString()
                    });
                    
                    if (response?.Payload?.Data != null)
                    {
                        _secret = response.Payload.Data.ToStringUtf8();
                        if (!string.IsNullOrEmpty(_secret))
                            return _secret;
                    }
                }
                catch (RpcException retryEx) when (retryEx.StatusCode == StatusCode.NotFound)
                {
                    // Secret still doesn't exist after retry
                }
            }

            if (!_autoCreate)
            {
                throw new SecretNotFoundException($"No secret found in Google Cloud Secret Manager with key: {secretKey}");
            }

            return await CreateSecretWithRetry(secretKey, secretName);
        }

        private async Task<string> CreateSecretWithRetry(string secretKey, SecretName secretName)
        {
            const int maxRetries = 3;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Generate a new secret value
                    var newSecret = Guid.NewGuid().ToString();
                    
                    // Attempt to create the secret
                    var secret = await _client.CreateSecretAsync(new CreateSecretRequest
                    {
                        Parent = ProjectName.FromProject(_projectId).ToString(),
                        SecretId = secretKey,
                        Secret = new Secret
                        {
                            Replication = new Replication
                            {
                                Automatic = new Replication.Types.Automatic()
                            },
                            Labels = { { "managed-by", "fig" } }
                        }
                    });
                    
                    // Add the secret version with the actual value
                    await _client.AddSecretVersionAsync(new AddSecretVersionRequest
                    {
                        Parent = secret.Name,
                        Payload = new SecretPayload
                        {
                            Data = ByteString.CopyFromUtf8(newSecret)
                        }
                    });
                    
                    // Verify the secret was created successfully by reading it back
                    var verifyResponse = await _client.AccessSecretVersionAsync(new AccessSecretVersionRequest
                    {
                        Name = SecretVersionName.FromProjectSecretSecretVersion(_projectId, secretKey, "latest").ToString()
                    });
                    
                    if (verifyResponse?.Payload?.Data != null)
                    {
                        var retrievedSecret = verifyResponse.Payload.Data.ToStringUtf8();
                        if (retrievedSecret == newSecret)
                        {
                            _secret = newSecret;
                            return _secret;
                        }
                        
                        // If verification failed but secret exists, use the existing one
                        if (!string.IsNullOrEmpty(retrievedSecret))
                        {
                            _secret = retrievedSecret;
                            return _secret;
                        }
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
                {
                    // Conflict - another process may have created the secret concurrently
                    // Try to retrieve the existing secret
                    try
                    {
                        var existingResponse = await _client.AccessSecretVersionAsync(new AccessSecretVersionRequest
                        {
                            Name = SecretVersionName.FromProjectSecretSecretVersion(_projectId, secretKey, "latest").ToString()
                        });
                        
                        if (existingResponse?.Payload?.Data != null)
                        {
                            _secret = existingResponse.Payload.Data.ToStringUtf8();
                            if (!string.IsNullOrEmpty(_secret))
                                return _secret;
                        }
                    }
                    catch (RpcException)
                    {
                        // If we can't read the secret that supposedly exists, continue retrying
                    }
                }
                catch (RpcException ex) when (IsTransientError(ex) && attempt < maxRetries)
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

        private static bool IsTransientError(RpcException ex)
        {
            return ex.StatusCode == StatusCode.ResourceExhausted ||  // Rate limiting
                   ex.StatusCode == StatusCode.Unavailable ||       // Service unavailable
                   ex.StatusCode == StatusCode.Internal ||          // Internal server error
                   ex.StatusCode == StatusCode.DeadlineExceeded;    // Timeout
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