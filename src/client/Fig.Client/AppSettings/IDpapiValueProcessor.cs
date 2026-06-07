namespace Fig.Client.AppSettings;

internal interface IDpapiValueProcessor
{
    bool IsSupported { get; }

    string Encrypt(string plainText);

    string Decrypt(string cipherText);
}
