using System.Collections.Generic;
using Fig.Contracts.Status;

namespace Fig.Client.Status;

public static class ExternallyManagedSettingsBridge
{
    public static List<ExternallyManagedSettingDataContract>? ExternallyManagedSettings { get; set; }
}
