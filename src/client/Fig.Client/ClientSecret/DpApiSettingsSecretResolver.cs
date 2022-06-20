using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Fig.Client.Configuration;
using Fig.Client.Exceptions;
using Fig.Common.Cryptography;

namespace Fig.Client.ClientSecret;

public class DpApiSettingsSecretResolver : ISecretResolver
{
    private readonly IFigOptions _figOptions;

    public DpApiSettingsSecretResolver(IFigOptions figOptions)
    {
        _figOptions = figOptions;
    }

    public SecureString ResolveSecret()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new FigConfigurationException("DPAPI secret resolution is only available on Windows");

        if (string.IsNullOrEmpty(_figOptions.ClientSecret))
            throw new FigConfigurationException("Encrypted client secret must be set in the appSettings.json file");

        return Unprotect(_figOptions.ClientSecret, DataProtectionScope.CurrentUser);
    }

    private SecureString Unprotect(string encryptedString, DataProtectionScope scope)
    {
        try
        {
            return Encoding.UTF8
                .GetString(ProtectedData.Unprotect(Convert.FromBase64String(encryptedString), null, scope))
                .ToSecureString();
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
                "\r Then included the value of $encodedText in the appSettings.json file");
        }
    }
}