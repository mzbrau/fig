using Fig.Contracts.ImportExport;

namespace Fig.Api.ExtensionMethods;

public static class SettingClientExportDataContractExtensions
{
    public static string GetIdentifier(this SettingClientExportDataContract client)
    {
        return $"{client.Name}-{client.Instance}";
    }
}