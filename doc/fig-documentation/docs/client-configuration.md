---
sidebar_position: 2
---

# Client Configuration

Fig can be installed into any client type however this section will focus on asp.net style projects written in dotnet 7.

1. Add the Fig.Client nuget package

2. In your program.cs file, add the following

```csharp
var configuration = new ConfigurationBuilder()
    .AddFig<Settings>(o =>
    {
        o.ClientName = "<YOUR CLIENT NAME>";
    }).Build();
```

:::tip My tip

It is recommended that Fig be the **LAST** configuration provider added. This is because Fig will override any settings that are set in other configuration providers. If you use other configuration providers after Fig, they may overwrite settings in the application but this will not be visible from the Fig web application.
If you need to override a value locally, you can use the [client settings override](https://www.figsettings.com/docs/features/client-settings-override/) feature.

:::


3. Add an environment variable called FIG_API_URI with the URI of the Fig API. For example:

   ```
   FIG_API_URI=https://localhost:7281
   ```

Multiple addresses separated by a comma are also supported with the first address being prioritized. Addresses are tested in order on startup and are not changed once selected.

:::tip My tip

You can disable Fig by removing the FIG_API_URI environment variable. This is useful if you want to use other configuration providers instead in some environments.

:::

4. Create a class to hold your configuration items. e.g. `Settings` (they can be called whatever you want)

5. Extend `Settings` from `SettingsBase`

6. Create a secret for your client. This must be a random string of at least 32 characters. Fig accepts 3 ways to register a secret. 
   On **Windows**, secrets must be stored in DPAPI and the encrypted value set in an environment variable called `FIG_<CLIENT NAME>_SECRET`. There is a DPAPI tool included as part of the Fig release which can be used to easily generate an encrypted secret.
   In a **Docker Container**, the secret must be set as a docker secret called `FIG_<client name>_SECRET`. The file may also have a .txt extension.
   On any other platform, it is possible to specify the secret in code, but this is **not recommended for production use**. It can be set in the options when registering Fig. e.g.

   ```csharp
   var configuration = new ConfigurationBuilder()
    .AddFig<Settings>(o =>
    {
        o.ClientName = "AspNetApi";
        o.ClientSecretOverride = "d4b0b76dfb5943f3b0ab6a7f70b6ffa0";
    }).Build();
   ```

7. It is recommended that you validate the settings when they are changed. To add this functionality, add the following in `program.cs`:
   ```csharp
   builder.Host.UseFigValidation<Settings>();
   ```

8. If you want to allow the Fig web application to be able to restart Fig, add the following in `program.cs`:

   ```csharp
   var configuration = new ConfigurationBuilder()
    .AddFig<Settings>(o =>
    {
        o.ClientName = "AspNetApi";
        o.SupportsRestart = true;
    }).Build();
   
   builder.Host.UseFigRestart<Settings>();
   ```
## Fig Options

There are a number of options that you can configure within Fig.

| Option               | Description                                                  | Example                                |
| -------------------- | ------------------------------------------------------------ | -------------------------------------- |
| LiveReload           | A boolean indicating if this client should live reload its settings. If set to true the values of the properties in the settings class will be updated as soon as they are updated in the Fig web app application. Default to true. | True                                   |
| ClientSecretOverride | A string (at least 32 characters) that is unique to this application which is used to authenticate the client towards the Fig api. | e682dea03f044e0<br />eb571c441eb095ee9 |
| VersionOverride      | By default Fig will attempt to locate the version of your application. This is used to display the version within the Fig Web Application. Fig looks at the `AssemblyFileVersionAttribute` for version information. If your application is not versioned in this way, the version can be overriden here. | 1.2                                    |
| AllowOfflineSettings | True if offline settings should be supported. Offline settings are useful in the case the Fig API is offline and your application needs to start, it can start with the previously issued settings. Settings are stored as an encrypted blob with your client secret as the encryption key. Defaults to true. | True                                   |

