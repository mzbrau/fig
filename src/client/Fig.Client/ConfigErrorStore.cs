using System.Collections.Generic;

namespace Fig.Client;

public static class ConfigErrorStore
{
    private static List<string> ConfigurationErrors { get; } = new();
    
    public static bool HasConfigurationError { get; set; }

    public static void AddConfigurationErrors(List<string> configurationErrors)
    {
        ConfigurationErrors.AddRange(configurationErrors);
    }

    public static List<string> GetAndClearConfigurationErrors()
    {
        var errors = new List<string>(ConfigurationErrors);
        ConfigurationErrors.Clear();
        return errors;
    }
}