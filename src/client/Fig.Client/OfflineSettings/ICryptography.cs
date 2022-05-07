namespace Fig.Client.OfflineSettings
{
    public interface ICryptography
    {
        string Encrypt(string clientName, string plainTextValue);

        string Decrypt(string clientName, string encryptedValue);
    }
}