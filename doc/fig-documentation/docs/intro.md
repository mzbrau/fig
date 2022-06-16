---
sidebar_position: 1
---

# Quick Start

To get up and running with Fig, you'll need to set up the API, Web and integrate the client nuget package into your application.

## Install API

In the future, this will be a docker container but at the moment, you must clone the code and build it yourself.



## Install Web

In the future, this will be a docker container but at the moment, you must clone the code and build it yourself.



## Integrate Client

:::tip My tip

In this guide, we'll create an ASP.NET project from scratch and integrate the Fig.Client to use fig for configuration. However the same instructions apply if you have an existing project. Just skip the project creation.

:::

1. Create new ASP.NET project

   ```
   dotnet new 
   ```

2. Open the project in your favourite

3. Add **Fig.Client** nuget package

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
   await builder.Services.AddFig<IExampleSettings, ExampleSettings>(new ConsoleLogger());
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



See the **examples folder** in the source repository for more examples.

