---
sidebar_position: 6
---

# Ordering

By default, settings are ordered alphabetically but this can be overridden in the setting configuration.

## Usage

```csharp
[DisplayOrder(1)]
[Setting("Long Setting", 99)]
public long LongSetting { get; set; }
```

