---
sidebar_position: 1
---

# Client Configuration

Fig can be installed into any client type however this section will focus on asp.net style projects written in dotnet 7.

1. Add the Fig.Client nuget package

2. In your program.cs file, add the following

   ```csharp
   services.AddFig<ISettings, Settings>(logger, options =>
           {
               options.ApiUri = new Uri("https://localhost:7281"); // Point towards Fig API
               options.ClientSecret = "aef943d9825c4bf9a9f1b0a633e3ffc3"; // Should be defined elsewhere
           });
   ```

3. Update the URI to point towards the Fig API and your client secret should be a unique GUID. It should be defined elsewhere later but we can start with it being in code.

4. Create classes `ISettings` and `Settings` (they can be called whatever you want)

5. Extend `Settings` from `SettingsBase`

## Setting the Options

Options can be set in a few ways:

- In code (as in the example above)

- In appsettings.json file - for example:

  ```
  "fig": {
      "ApiUri": "https://localhost:7281",
      "secretStore": "appSettings",
      "PollIntervalMs": 20000,
      "LiveReload": true,
      "Instance": "myInstance",
      "clientSecret": "e682dea03f044e0eb571c441eb095ee9"
    },
  ```

  

- As environment variable (for some settings)



## Fig Options

There are a number of options that you can configure within Fig.

| Option               | Description                                                  | Set from                                                     | Example                                |
| -------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ | -------------------------------------- |
| ApiUri               | The URI of the Fig API                                       | Code, appsettings.json, environment variable (FIG_API_URI)   | https://localhost:7281                 |
| PollIntervalMs       | The interval between polls of the Fig API in milliseconds. A more frequent polling interval will mean the application will be more responsive in loading updated settings or responding to restart requests. Defaults to 30000 milliseconds. | Code, appsettings.json                                       | 30000                                  |
| LiveReload           | A boolean indicating if this client should live reload its settings. If set to true the values of the properties in the settings class will be updated as soon as they are updated in the Fig web app application. Default to true. | Code, appsettings.json                                       | True                                   |
| SecretStore          | An enum defining where Fig should look for the secret. The options are: <br />- EnvironmentVariable<br />- AppSettings<br />- DpApi<br />- InCode<br />More details on these below. | Code, appsettings.json                                       | EnvironmentVariable                    |
| Instance             | The name of the instance for this client. A client instance can be used if multiple copies of an application (with same client name) are deployed with one or more requiring different settings from the others. | Code, appsettings.json, environment variable (FIG_client name without spaces_INSTANCE) | MyInstance                             |
| ClientSecret         | A GUID that is unique to this application which is used to authenticate the client towards the Fig api. | Code (testing only), appsettings.json, environment variable (FIG_client name without spaces_SECRET), DPAPI | e682dea03f044e0<br />eb571c441eb095ee9 |
| VersionOverride      | By default Fig will attempt to locate the version of your application. This is used to display the version within the Fig Web Application. Fig looks at the `AssemblyFileVersionAttribute` for version information. If your application is not versioned in this way, the version can be overriden here. | Code, appsettings.json                                       | 1.2                                    |
| AllowOfflineSettings | True if offline settings should be supported. Offline settings are useful in the case the Fig API is offline and your application needs to start, it can start with the previously issued settings. Settings are stored as an encrypted blob with your client secret as the encryption key. Defaults to true. | Code, appsettings.json                                       | True                                   |



## Managing Client Secrets

Client secrets secure the settings of your client. When the client registers with the Fig API it passes this secret to the API and it is stored encrypted in the database. When the client requests its settings, it uses the secret to authenticate itself and get the settings. This means if the client secret leaks, it can be used to get the settings for that particular client including any secret settings in plain text.

As a result, the client secret should be managed carefully. Fig has a number of options for storing your client secret, these include:

| Method               | Description                                                  |
| -------------------- | ------------------------------------------------------------ |
| In Code              | The secret can be defined in code. This is not recommended for production as the secret cannot be changed if it is compromised and can be found in the decompiled source files. |
| App Settings         | The secret can be defined in the appsettings.json file. This method is suitable if the file is securely located or if the settings do not container any sensitive information. |
| Environment Variable | The secret can be stored as an environment variable. This is a good choice if your application is container based and securely located within a cluster. |
| Data Protection API  | Windows only. The DP API allows information to be stored in an encrypted format accessible only to your user profile. When used the encrypted version should be set as the client secret within the appsettings.json file. Fig will then try and decrypt it. Note that it must be registered using the user that Fig will be running as.<br />See [this post](https://stackoverflow.com/a/58417163) for details of how to put your password in DP API |

If you have any suggestions on other locations where this could be stored, please raise a ticket on [Github](https://github.com/mzbrau/fig/issues).

## Events

The Fig Client also raises 2 events. They are explained below.

| Event            | Description                                                  |
| ---------------- | ------------------------------------------------------------ |
| SettingsChanged  | This event is raised when settings have been updated by fig. It provides the developer an opportunity to take actions to reload parts of the application to use the new setting values. |
| RestartRequested | The Fig Web Application has a Restart button which is enabled if this event is subscribed to. It is up to the implementer to actually take action to restart the application, Fig just raises the event. This can be a good option if not all settings are automatically loaded. By using the restart button, the application can be restarted and then start running with the latest settings. |

Example:

```csharp
services.AddFig<ISettings, Settings>(logger, options =>
        {
            options.ApiUri = new Uri("https://localhost:7281");
            options.ClientSecret = "aef943d9825c4bf9a9f1b0a633e3ffc3";
        }, settings => logLevel.MinimumLevel = settings.LogLevel,
            () => Environment.Exit(0));
```

