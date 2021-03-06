using System;
using System.Security;
using Fig.Common.Cryptography;

namespace Fig.Client.ClientSecret;

public class EnvironmentVariableSecretResolver : ISecretResolver
{
    private readonly string _clientName;

    public EnvironmentVariableSecretResolver(string clientName)
    {
        _clientName = clientName;
    }

    public SecureString ResolveSecret()
    {
        var environmentVariableName = $"FIG_{_clientName.Replace(" ", string.Empty)}_SECRET";
        var value = Environment.GetEnvironmentVariable($"FIG_{_clientName.Replace(" ", string.Empty)}_SECRET");
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Environment variable {environmentVariableName} contained no value");

        return value.ToSecureString();
    }
}