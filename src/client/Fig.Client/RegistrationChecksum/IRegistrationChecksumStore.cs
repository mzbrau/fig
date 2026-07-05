namespace Fig.Client.RegistrationChecksum;

public interface IRegistrationChecksumStore
{
    string? Get(string clientName, string? instance);

    void Save(string clientName, string? instance, string checksum);

    void Delete(string clientName, string? instance);

    string GetFilePath(string clientName, string? instance);
}
