---
sidebar_position: 1


---

# Default Values

Fig allows clients to specify a default value for each setting. Default values are specified wtihin the Setting attribute.

## Usage

Any value can be specified after the description. The value needs to be the same type as the setting.

```csharp
[Setting("Long Setting", 99)]
public long LongSetting { get; set; }
```

In the case where there is a collection, the default value can be set using a public static method included within the settings class. It is only read if the default value parameter is null.

Note that only collections of base types (e.g. string, int, etc.) are supported for default values. Collections of custom object cannot have default values at this point.

```csharp
[Setting("My Items", defaultValueMethodName: "GetDefaultItems")]
public List<string> Items { get; set; }

public static List<string> GetDefaultItems()
{
    return new List<string>()
    {
        "LargeItem",
        "SmallItem"
    };
}
```

## 
