---
sidebar_position: 1


---

# Default Values

Fig allows clients to specify a default value for each setting. Default values are specified in the normal way for modern dotnet applications.

## Usage

```csharp
[Setting("Long Setting")]
public long LongSetting { get; set; } = 66;
```

In the case where there is a collection, default values need to be set via the setting attribute rather than directly on the property.

The reason for this is that the configuration provider handling creates a new instance of the class and applies the updated values on top. However, when they are applied, Microsoft decided they should be appended rather than replaced so the updated values end joining the default values in the collection.

```csharp
[Setting("My Items", defaultValueMethodName: nameof(GetDefaultItems)))]
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
