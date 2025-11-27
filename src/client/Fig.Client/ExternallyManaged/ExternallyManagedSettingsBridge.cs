using System.Collections.Generic;
using Fig.Contracts.Status;

namespace Fig.Client.ExternallyManaged;

public static class ExternallyManagedSettingsBridge
{
    public static List<ExternallyManagedSettingDataContract>? ExternallyManagedSettings { get; private set; }

    public static void SetExternallyManagedSettings(List<ExternallyManagedSettingDataContract>? settings)
    {
        ExternallyManagedSettings = settings;
    }

    public static List<ExternallyManagedSettingDataContract>? ConsumeExternallyManagedSettings()
    {
        var settings = ExternallyManagedSettings;
        ExternallyManagedSettings = null;
        return settings;
    }
}
