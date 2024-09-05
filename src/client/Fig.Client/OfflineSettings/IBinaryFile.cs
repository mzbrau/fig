namespace Fig.Client.OfflineSettings;

internal interface IBinaryFile
{
    void Write(string clientName, string value);

    string? Read(string clientName);

    void Delete(string clientName);

    string GetFilePath(string clientName);
}