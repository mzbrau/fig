---
sidebar_position: 4




---

# Integration Testing ASP.NET Core Apps

If your application uses Fig for configuration then you have some options when it comes to integration testing. The first option is to use Fig for configuration as part of your integration test. However, this means you are essentially integration testing the Fig application too and is probably not desirable in most cases. The other option involves disabling fig and injecting in a different configuration provider for the tests. Fig comes packaged with a convenient reloadable configuration provider which makes updating configuraiton in integration tests very easy.

To get started, do the following.

1. Disable the FigConfigurationProvider

   There are 2 ways to disable the provider, the first is to remove the FIG_API_URI environment variable, but this means tests will be flakey depending on the environment. A better way is to pass in a command line argument.

   In your program.cs file, make sure you are passing the command line arguments to Fig.

   ```csharp
   builder.Configuration.SetBasePath(GetBasePath())
       .AddFig<Settings>(options =>
       {
           options.ClientName = "AspNetApi";
           options.CommandLineArgs = args;
       });
   ```

   Then, in your integration test, add the following line into your WebApplicationFactory setup:

   ```csharp
   builder.DisableFig();
   ```

   This will pass in a command line argument which will result in Fig being disabled

2. Inject the reloadable configuration provider

   This configuration provider allows you to pass in your settings class and reload it at will to update the configuration of your application.

   ```csharp
   var settings = new Settings();
   var configReloader = new ConfigReloader()
   builder.ConfigureAppConfiguration((a, conf) =>
   {
       conf.AddIntegrationTestConfiguration(configReloader, settings);
       conf.Build();
   });
   ```

   Passing the settings in initially is optional and allows for the initial configuraiton. After that, settings can be updated as follows:

   ```csharp
   settings.MyProperty = "NewValue";
   configReloader.Reload(settings);
   ```

3. Write your tests and update the value accordingly.

A full example looks like the following:

```csharp
var settings = new Settings();
// This will set an initial configuration value
settings.MyProperty = "OriginalValue";
var configReloader = new ConfigReloader()
var application = new WebApplicationFactory<Worker>().WithWebHostBuilder(builder =>
{
    builder.DisableFig();
    builder.ConfigureAppConfiguration((a, conf) =>
    {
        conf.AddIntegrationTestConfiguration(ConfigReloader, Settings);
        conf.Build();
    });

    builder.ConfigureServices((_, services) =>
    {
        // Override any registrations here.
    });
});

var client = application.CreateClient();

settings.MyProperty = "NewValue"
// Value will be updated.
configReloader.Reload(settings);

```

