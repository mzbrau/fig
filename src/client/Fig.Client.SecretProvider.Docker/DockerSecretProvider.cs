using Fig.Client.Contracts;
using Microsoft.Extensions.FileProviders;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Fig.Client.SecretProvider.Docker
{
    public class DockerSecretProvider : ClientSecretProviderBase<DockerSecretProvider>
    {
        private const string DockerSecretPath = "/run/secrets/";
        private const string DockerSecretFileExtension = ".txt";

        public DockerSecretProvider()
            : base("Docker")
        {
        }

        public override bool IsEnabled => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        protected override async Task<string> GetOrCreateSecretInternal(string clientName)
        {
            var key = string.Format(SecretKeyFormat, clientName.Replace(" ", "").ToUpper());
            var secretFilePath = Path.Combine(DockerSecretPath, key);
            var secretFilePathWithExt = Path.ChangeExtension(secretFilePath, DockerSecretFileExtension);

            Logger?.LogDebug("Attempting to retrieve Docker secret for key {Key} (client: {ClientName})", key,
                clientName);
            string? secret = TryReadSecretFile(secretFilePath) ?? TryReadSecretFile(secretFilePathWithExt);
            if (!string.IsNullOrEmpty(secret))
            {
                SecretCache[clientName] = secret;
                Logger?.LogInformation("Successfully retrieved Docker secret for key {Key} (client: {ClientName})", key,
                    clientName);
                return secret;
            }

            if (!AutoCreate)
            {
                Logger?.LogError(
                    "No docker secret found at {SecretFilePath} or {SecretFilePathWithExt} (client: {ClientName})",
                    secretFilePath, secretFilePathWithExt, clientName);
                throw new SecretNotFoundException(
                    $"No docker secret found at '{secretFilePath}' or '{secretFilePathWithExt}'");
            }

            try
            {
                var newSecret = Guid.NewGuid().ToString();
                var createPath = Directory.Exists(DockerSecretPath) ? secretFilePathWithExt : null;
                if (createPath == null)
                {
                    Logger?.LogError("Docker secrets directory {DockerSecretPath} does not exist or is not writable",
                        DockerSecretPath);
                    throw new InvalidOperationException(
                        $"Docker secrets directory '{DockerSecretPath}' does not exist or is not writable.");
                }

                if (!File.Exists(createPath))
                {
                    Logger?.LogDebug("Creating Docker secret file {CreatePath}", createPath);
                    await File.WriteAllTextAsync(createPath, newSecret + "\n");
                }

                secret = TryReadSecretFile(createPath);
                if (secret == newSecret)
                {
                    SecretCache[clientName] = newSecret;
                    Logger?.LogInformation("Successfully created Docker secret for key {Key} (client: {ClientName})",
                        key, clientName);
                    return newSecret;
                }

                if (!string.IsNullOrEmpty(secret))
                {
                    Logger?.LogWarning(
                        "Docker secret {Key} was created concurrently by another process. Using existing value", key);
                    SecretCache[clientName] = secret;
                    return secret;
                }

                Logger?.LogError("Failed to create or verify docker secret at {CreatePath}", createPath);
                throw new InvalidOperationException($"Failed to create or verify docker secret at '{createPath}'");
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger?.LogError("Insufficient permissions to create docker secret in {DockerSecretPath}: {Error}",
                    DockerSecretPath, ex.Message);
                throw new InvalidOperationException(
                    $"Insufficient permissions to create docker secret in '{DockerSecretPath}'", ex);
            }
            catch (Exception ex)
            {
                Logger?.LogError("Error creating or reading docker secret for {ClientName}: {Error}", clientName,
                    ex.Message);
                throw new InvalidOperationException(
                    $"Error creating or reading docker secret for '{clientName}': {ex.Message}", ex);
            }
        }

        private static string? TryReadSecretFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var line = File.ReadLines(filePath).FirstOrDefault();
                    if (!string.IsNullOrEmpty(line))
                        return Regex.Replace(line, @"(\r\n|\n)", string.Empty);
                }
            }
            catch (Exception)
            {
                // Ignore and treat as not found
            }

            return null;
        }
    }
}