using System;
using System.Collections.Generic;
using System.Linq;

namespace Fig.LoadTest;

public sealed record LoadTestClientDefinition(string ClientName, string InstanceName, Type SettingsType)
{
    public static IReadOnlyList<LoadTestClientDefinition> CreateDefaultPairs()
    {
        var baseDefinitions = new (string ClientName, Type SettingsType)[]
        {
            ("LoadTest-Client01", typeof(LoadTestSettings01)),
            ("LoadTest-Client02", typeof(LoadTestSettings02)),
            ("LoadTest-Client03", typeof(LoadTestSettings03)),
            ("LoadTest-Client04", typeof(LoadTestSettings04)),
            ("LoadTest-Client05", typeof(LoadTestSettings05)),
            ("LoadTest-Client06", typeof(LoadTestSettings06)),
            ("LoadTest-Client07", typeof(LoadTestSettings07)),
            ("LoadTest-Client08", typeof(LoadTestSettings08)),
            ("LoadTest-Client09", typeof(LoadTestSettings09)),
            ("LoadTest-Client10", typeof(LoadTestSettings10)),
        };

        return baseDefinitions
            .SelectMany(def => new[]
            {
                new LoadTestClientDefinition(def.ClientName, "instance-a", def.SettingsType),
                new LoadTestClientDefinition(def.ClientName, "instance-b", def.SettingsType)
            })
            .ToList();
    }
}
