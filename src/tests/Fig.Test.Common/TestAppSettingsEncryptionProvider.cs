using Fig.Client.Contracts;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common;

/// <summary>
/// Cross-platform fake encryption for appsettings offline flow tests.
/// Uses an "enc:" prefix instead of real DPAPI.
/// </summary>
public sealed class TestAppSettingsEncryptionProvider : IAppSettingsEncryptionProvider, IClientSecretProvider
{
    public string Name => "Test";

    public bool IsSupported { get; set; } = true;

    public bool IsEnabled => IsSupported;

    public void AddLogger(ILoggerFactory logger)
    {
    }

    public Task<string> GetSecret(string clientName) => Task.FromResult("test-client-secret");

    public string Encrypt(string plainText) => $"enc:{plainText}";

    public string Decrypt(string cipherText) => cipherText.StartsWith("enc:", StringComparison.Ordinal)
        ? cipherText[4..]
        : cipherText;
}
