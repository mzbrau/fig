---
sidebar_position: 1
---

# Quick Start

To get up and running with Fig, you'll need to set up the API, Web and integrate the client nuget package into your application.

## Install API and Web Client

The API and Web Clients can be installed using Docker. This guide assumes docker is installed and running.

1. Clone the [fig repository](https://github.com/mzbrau/fig) and use the `docker-compose.yml` file included or copy the code below into a `docker-compose.yml` file.

```yaml
version: '3.8'

services:
  fig-api:
    image: mzbrau/fig-api:latest
    ports:
      - "5000:80"

  fig-web:
    image: mzbrau/fig-web:latest
    ports:
      - "8080:80"
    depends_on:
      - fig-api
```

2. Open a terminal / command prompt, navigate to the directory containing the docker-compose file and type `docker-compose up` to download the containers and run them.

## Log in to Web Client

Navigate to http://localhost:8080 and at the login prompt enter user: `admin` password: `admin`. You should see the administration view of fig with all options available.



## Integrate Client

:::tip My tip

In this guide, we'll create an ASP.NET project from scratch and integrate the Fig.Client to use fig for configuration. However the same instructions apply if you have an existing project. Just skip the project creation.

:::

1. Create new ASP.NET project

   ```
   dotnet new 
   ```

2. Open the project in your favourite

3. Add **[Fig.Client](https://www.nuget.org/packages/Fig.Client)** nuget package

4. Create a new class to hold your application settings, extending the SettingsBase class. For example:

   ```c#
   public interface IExampleSettings
   {
       string FavouriteAnimal { get; }
       int FavouriteNumber { get; }
       bool TrueOrFalse { get; }
   }
   
   public class ExampleSettings : SettingsBase, IExampleSettings
   {
       public override string ClientName => "ExampleService";
   
       [Setting("My favourite animal", "Cow")]
       public string FavouriteAnimal { get; set; }
   
       [Setting("My favourite number", 66)]
       public int FavouriteNumber { get; set; }
       
       [Setting("True or false, your choice...", false)]
       public bool TrueOrFalse { get; set; }
   }
   ```

5. Register your settings class in the `program.cs` file.

   ```c#
   await builder.Services.AddFig<ISettings, Settings>(new ConsoleLogger(), options =>
   {
       options.ApiUri = new Uri("https://localhost:5000"); // Note: This should match the api address and is better stored in the appSettings or as an environment variable.
       options.ClientSecret = "757bedb7608244c48697710da05db3ca"; // Note: This should be a unique guid and defined elsewhere
   });
   ```

6. Access the settings class via depedency injection. For example

   ```c#
   public WeatherForecastController(ILogger<WeatherForecastController> logger, IExampleSettings settings)
   {
     _logger = logger;
     _settings = settings;
   }
   ```

7. Use the settings as required in your application.

8. Run your application, the settings will be registered and default values will be used automatically.



See the **examples folder** in the source repository for more examples.

