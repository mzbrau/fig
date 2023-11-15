using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Fig.Client.Exceptions;
using Microsoft.Extensions.FileProviders;

namespace Fig.Client.ClientSecret;

internal class ClientSecretProvider : IClientSecretProvider
{
    private const string SecretKey = "FIG_{0}_SECRET";
    private string? _clientSecret;

    public string GetSecret(string clientName)
    {
        _clientSecret ??= ResolveSecret(clientName);
        return _clientSecret;
    }

    private string ResolveSecret(string clientName)
    {
        var key = string.Format(SecretKey, clientName.Replace(" ", "").ToUpper());

        var dockerSecret = GetDockerSecret(key);

        
        if (!string.IsNullOrWhiteSpace(dockerSecret))
        {
            _clientSecret = dockerSecret;
            return _clientSecret!;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _clientSecret = GetDpApiSecret(key);
            return _clientSecret!;
        }

        throw new FigConfigurationException("Client secret was not found. Unable to request settings from Fig");
    }

    public string? GetDockerSecret(string key)
    {
        const string dockerSecretPath = "/run/secrets/";
        if (Directory.Exists(dockerSecretPath))
        {
            IFileProvider provider = new PhysicalFileProvider(dockerSecretPath);
            IFileInfo fileInfo = provider.GetFileInfo(key);
            if (!fileInfo.Exists && !Path.HasExtension(key))
            {
                key = Path.ChangeExtension(key, ".txt");
                fileInfo = provider.GetFileInfo(key);
            }

            if (fileInfo.Exists)
            {
                using var stream = fileInfo.CreateReadStream();
                using var streamReader = new StreamReader(stream);
                return streamReader.ReadToEnd();
            }
        }

        return null;
    }

    private string GetDpApiSecret(string secretKey)
    {
        var encryptedSecret = Environment.GetEnvironmentVariable(secretKey);
        
        if (string.IsNullOrEmpty(encryptedSecret))
            throw new FigConfigurationException($"Encrypted client secret must be set in an environment variable called {secretKey}");

        return Unprotect(encryptedSecret, DataProtectionScope.CurrentUser);
    }

    private string Unprotect(string encryptedString, DataProtectionScope scope)
    {
        try
        {
            return Encoding.UTF8
                .GetString(ProtectedData.Unprotect(Convert.FromBase64String(encryptedString), null, scope));
        }
        catch (Exception)
        {
            throw new FigConfigurationException(
                $"Invalid DPAPI encrypted value {encryptedString}. Client secret can be added via PowerShell (core) " +
                "running as the same user as your application, using the following commands:" +
                "\r$scope = [System.Security.Cryptography.DataProtectionScope]::CurrentUser" +
                "\r$secret = [System.Text.Encoding]::UTF8.GetBytes(\"<YOUR CLIENT SECRET HERE>\")" +
                "\r$protected = [System.Security.Cryptography.ProtectedData]::Protect($secret, $null, $scope)" +
                "\r$encodedText = [Convert]::ToBase64String($protected)" +
                "\rWrite-Host $encodedText" +
                "\r It can also be set using the dpapi tool available from the Fig repository.");
        }
    }
}