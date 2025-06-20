---
sidebar_position: 1
---

# Advanced Settings

An advanced setting is one that has a reasonable default value that would not need to be changed in a normal deployment. As a result, Fig will hide this setting by default to allow those configuring the application to focus on the settings that do need to be changed. An example might be a timeout value. In most cases, the developer will have chosen a reasonable default but the setting is still included incase it needs to be changed for debugging for when the application is deployed in a specific environment.

## Usage

```csharp
[Advanced]
[Setting("Long Setting", 99)]
public long LongSetting { get; set; }
```

## Appearance

![advanced-settings](./img/advanced-setting.png)
