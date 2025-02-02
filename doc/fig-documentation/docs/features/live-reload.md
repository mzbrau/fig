---
sidebar_position: 13
---

# Live Reload

By default, clients using the `Fig.Client` nuget package will support the live reload of settings. This means that when a setting value is updated in the Web Client, that updated value will immediately be available in the settings class for that client. In addition, a settings changed event will be raised on the settings class which can be used to take action when it occurs.

If live reload is not the desired behavior, it can be disabled in the options like this:

```csharp
var configuration = new ConfigurationBuilder()
    .AddFig<Settings>(o =>
    {
        o.ClientName = "AspNetApi";
        o.LiveReload = false;
    });
```

Note that this will only change the 'default' value for the client. It might be overridden by the Fig web application if the setting is changed there.
