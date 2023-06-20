---
sidebar_position: 10
---

# Client Management

Administrators in Fig are able to see a list of all currently connected clients. Clients are considered connected if they poll the API at least once every 2 times the configured poll interval for that client. Clients will poll automatically as part of the `Fig.Client` nuget package and deault to 30 seconds per poll.

The client management page also includes a number of other details about the client including the version of the `Fig.Client` nuget package and the version of the host application. The host application version is derived using the following code:

```csharp
public string GetHostVersion()
{
    if (!string.IsNullOrEmpty(_options.VersionOverride))
        return _options.VersionOverride!;

    var assembly = Assembly.GetEntryAssembly();

    if (assembly == null)
        return "Unknown";

    var version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
    return version;
}
```

The version can be overriden in the fig configuration. For example:

```csharp
await builder.Services.AddFig<ISettings, Settings>(new ConsoleLogger(), options =>
{
    options.ApiUri = new Uri("https://localhost:7281");
    options.ClientSecret = "757bedb7608244c48697710da05db3ca";
    options.VersionOverride = "v6";
});
```

The options also allows the setting of the poll interval.

It is possible to restart clients if the restart requested event is subscribed to. For example:

```csharp
IConsoleSettings settings = await provider.Initialize<ConsoleSettings>();
settings.RestartRequested += (sender, args) => { Console.WriteLine("Restart requested!"); };
```

It is up to the developer of the host application to take approproiate action when a restart is requested.

## Appearance

![image-20220802230151478](../../static/img/connected-clients.png)