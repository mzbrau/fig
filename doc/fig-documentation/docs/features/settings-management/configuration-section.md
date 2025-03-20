---
sidebar_position: 14

---

# Configuration Section

Fig is a configuration provider which means it sets values when are then made available for consumption within the application. When configuring settings for an application, it makes sense for all configuration items to reside within one class (or at least be referenced from that class). However, there are some use cases where you would like Fig to set values for different configuration sections.

## Usage

```csharp
[ConfigurationSectionOverride(SectionName, NameOverride)]
```

## Example Usage

One example might be Serilog configuration. When configuring Serilog within the appsettings.json file, it might look something like this:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      }
    ]
  }
}
```

In this case, the Serilog configuration is nested within the Serilog section. Fig supports this by allowing developers to specify a configuration section for settings to be set within. This is done by specifying the section name within the `[ConfigurationSectionOverride(SectionName, NameOverride)]` attribute.

To apply the above configuration in Fig, you could use the following settings class:

```csharp
[Setting("The minimum log level", "Information")]
[ConfigurationSectionOverride("Serilog:MinimumLevel", "Default")]
[ValidValues(typeof(LogEventLevel))]
public string MinLogLevel { get; set; }

[Setting("Override for microsoft logs", "Warning")]
[ConfigurationSectionOverride("Serilog:Override", "Microsoft")]
[ValidValues(typeof(LogEventLevel))]
public string MicrosoftLogOverride { get; set; }

[Setting("Override for system logs", "Warning")]
[ConfigurationSectionOverride("Serilog:Override", "System")]
[ValidValues(typeof(LogEventLevel))]
public string SystemLogOverride { get; set; }

[Setting("The name of the section to write to", "Console")]
[ConfigurationSectionOverride("Serilog:WriteTo", "Name")]
public string WriteToName { get; set; }
```

In reality, you might just want to specify certain values in Fig such as the minimum log level and leave the static configuration such as the write to section in the appsettings.json file.

## Multiple Usage

It is also possible to specify multiple configuration section overrides on a single setting. In that case, the value will be applied once for the original, and once per override.

```csharp
[Setting("The minimum log level", "Information")]
[ConfigurationSectionOverride("one", "Level")]
[ConfigurationSectionOverride("two")]
public string MinLogLevel { get; set; }
```

For example, in the value of the setting MinLogLevel will be applied to:

```csharp
MinLogLevel
one:Level
two:MinLogLevel
```

If there are not settings defined at these locations, the values will just be ignored.

## Json Settings and Data Grids

From Fig v1.2, `ConfigurationSectionOverride` is supported for json and data grid settings too.