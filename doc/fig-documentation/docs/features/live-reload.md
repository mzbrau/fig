---
sidebar_position: 13
---

# Live Reload

By default, clients using the `Fig.Client` nuget package will support the live reload of settings. This means that when a setting value is updated in the Web Client, that updated value will immediately be available in the settings class for that client. In addition, a settings changed event will be raised on the settings class which can be used to take action when it occurs.

If live reload is not the desired behaviour, it can be disabled in the options like this:

```c#
await builder.Services.AddFig<ISettings, Settings>(new ConsoleLogger(), options =>
{
    options.ApiUri = new Uri("https://localhost:7281");
    options.ClientSecret = "757bedb7608244c48697710da05db3ca";
    options.LiveReload = false;
});
```

