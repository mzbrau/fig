---
sidebar_position: 15

---

# Instances

Fig supports overriding the default settings for a client with an instance. An instance is a special key that can be passed by the calling client. When an instance is provided, Fig attempts to find settings matching that instance name. If none are found, the default settings for that client are returned.

Instances can be created by selecting the client and pressing the instance button. 

![image-20221129153120621](../../static/img/image-20221129153120621.png)

Once the instance is created, there will be two items in the clients list for that client.

![image-20221129153215045](../../static/img/image-20221129153215045.png)

These settings can be set individually. If a client requests settings with no instance or an instance other than 'Instance1' (in the example above) then they will be given the default settings.

If a client requests settings with instance 'Instance1', they will be provided with those specific settings.



## Setting an Instance on a Client

If you need your application to use an instance, it can be set in 3 ways:

- Appsettings.json file
- Environment variable
- Code

### Appsettings.json

You can specify settings in the appsettings.json file provided you use the services builder extension.

```c#
builder.Services.AddFig<ISettings, Settings>(new ConsoleLogger(), options =>
{
    options.ApiUri = new Uri("https://localhost:7281");
    options.ClientSecret = "757bedb7608244c48697710da05db3ca";
});
```

The appsettings.json might look like this:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "fig": {
    "ApiUri": "https://localhost:7281",
    "secretStore": "appSettings",
    "PollIntervalMs": 20000,
    "LiveReload": true,
    "Instance": "myInstance",
    "clientSecret": "e682dea03f044e0eb571c441eb095ee9"
  },
  "AllowedHosts": "*"
}
```

### Environment Variable

An environment variable can also be used provided the services builder extension is being used.

In that case it should be in the format:

```
FIG_<CLIENT NAME>_INSTANCE
```

For example:

```
FIG_MYCLIENT_INSTANCE
```



### Code

If you are not using the services builder, you must specify the instance in code. You need to set the Instance value in the FigOptions. You could drive this from any other configuration method as a hard coded value wouldn't seem to offer much value.

```c#
var figOptions = new FigOptions
{
    ApiUri = new Uri("http://localhost:5260"),
    ClientSecret = "c059383fc9b145d99b596bd00d892cf0",
    Instance = "MyInstance"
};
```



