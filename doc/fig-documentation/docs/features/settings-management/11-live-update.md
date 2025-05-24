---
sidebar_position: 11
---

# Live Update

Fig supports live setting update by default. This means that when a setting value is updated within the Fig web client and saved, any connected clients will be informed of this change on their next poll interval and then request the updated values and apply them. This can be tracked under connected clients within the web client.

Clients can be configured to disable the live update functionality for that client in the options. 

In addition, some settings may only be read once within the application. For example, a timer interval might be set on startup and even if the interval value is updated by Fig, it will not be applied within the application. This is good information for those configuring the application and Fig provides a way to communicate this by allowing developers to flag a setting that will not 'live update' for the application.

## Usage

```csharp
[Setting("Long Setting", 99, false)] // false indicates that this value is only read once
public long LongSetting { get; set; }
```

## Appearance

A small lightening bolt next to the setting indicates it will be updated without requiring a restart.

![image-20220910222327427](../../../static/img/image-20220910222327427.png)
