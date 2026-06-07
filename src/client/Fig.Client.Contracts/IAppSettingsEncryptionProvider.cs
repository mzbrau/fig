namespace Fig.Client.Contracts;

/// <summary>
/// Provides encryption and decryption for appsettings.json secret values used
/// by the <c>--printappsettings</c> and <c>--figoffline</c> command line features.
/// </summary>
public interface IAppSettingsEncryptionProvider
{
    string Name { get; }

    bool IsSupported { get; }

    string Encrypt(string plainText);

    string Decrypt(string cipherText);
}
