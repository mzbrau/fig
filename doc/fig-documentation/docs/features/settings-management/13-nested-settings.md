---
sidebar_position: 13
---

# Nested Settings

:::info[Note]

This feature was introduced in Fig v0.11.

:::

Application settings are often best modeled as a number of nested classes, this provides a number of advantages including:

- Settings can be locally grouped with related settings
- Setting classes can be reused between multiple components

Fig supports this configuration with the concept of Nested Settings. 

Settings are ordered in the same order they appear in the settings class.

## Usage

```csharp

public class ConsoleSettings : SettingsBase
{
    public override string ClientDescription => "Example of nested settings";

    [NestedSetting]
    public MessageBus? MessageBus { get; set; }
    
    [Setting("a timeout in milliseconds")]
    public double TimeoutMs { get; set; }
    
    [NestedSetting]
    public Database Database { get; set; }

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}

public class MessageBus
{
    [Setting("a message bus uri")]
    public string? Uri { get; set; }
    
    [NestedSetting]
    public Authorization? Auth { get; set; }
}

public class Authorization
{
    [Setting("a message bus user")]
    public string Username { get; set; } = "User1";
    
    [Setting("a message bus password")]
    public string? Password { get; set; }
}

public class Database
{
    [Setting("a database connection string")]
    public string? ConnectionString { get; set; }
    
    [Setting("a database timeout")]
    public int TimeoutMs { get; set; }
}

```

## Appearance

![Nested Settings](../../../static/img/nested-settings.png)