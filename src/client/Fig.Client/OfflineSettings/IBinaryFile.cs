namespace Fig.Client.OfflineSettings;

internal interface IBinaryFile
{
    void Write(string clientName, string? instance, string value);

    string? Read(string clientName, string? instance);

    void Delete(string clientName, string? instance);

    string GetFilePath(string clientName, string? instance);
}