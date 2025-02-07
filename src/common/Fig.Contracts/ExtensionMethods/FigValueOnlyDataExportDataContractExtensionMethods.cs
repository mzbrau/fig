using Fig.Contracts.ImportExport;

namespace Fig.Contracts.ExtensionMethods;

public static class FigValueOnlyDataExportDataContractExtensionMethods
{
    public static void ProcessExternallyManagedStatus(this FigValueOnlyDataExportDataContract data)
    {
        if (data.IsExternallyManaged == true)
        {
            foreach (var client in data.Clients)
            {
                foreach (var setting in client.Settings)
                {
                    if (setting.IsExternallyManaged != false)
                    {
                        setting.IsExternallyManaged = true;
                    }
                }
            }
        }
        else if (data.IsExternallyManaged == false)
        {
            foreach (var client in data.Clients)
            {
                foreach (var setting in client.Settings)
                {
                    if (setting.IsExternallyManaged != true)
                    {
                        setting.IsExternallyManaged = false;
                    }
                }
            }
        }
    }
}