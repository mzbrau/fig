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

In the case where there is a collection, the default value can be set using a static method. 

```csharp
[Setting("My Items")]
public List<string> Items { get; set; } = GetDefaultItems();

private static List<string> GetDefaultItems()
{
    return new List<string>()
    {
        "LargeItem",
        "SmallItem"
    };
}
```
