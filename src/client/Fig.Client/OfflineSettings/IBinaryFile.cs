namespace Fig.Client.OfflineSettings
{
    public interface IBinaryFile
    {
        void Write(string clientName, string value);

        string? Read(string clientName);

        void Delete(string clientName);
    }
}