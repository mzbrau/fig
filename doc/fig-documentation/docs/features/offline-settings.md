---
sidebar_position: 12
---

# Offline Settings

By default, clients using the `Fig.Client` nuget package will support offline settings. This is a fallback mechanism for when the Fig.API is offline. If the client application starts and it is unable to contact the API, it will attempt to load the last values that it got from the API and run with those. It will continue to attempt to contact the API and will update the settings once successfully reconnected.

The offline settings cache is stored as an encrypted binary file in a Fig directory in the local application data directory of the host machine. The client secret is used as the encryption / decryption key.

Offline settings can be disabled in the client configuration:

```csharp
var configuration = new ConfigurationBuilder()
    .AddFig<Settings>(o =>
    {
        o.ClientName = "AspNetApi";
        o.AllowOfflineSettings = false;
    });
```

