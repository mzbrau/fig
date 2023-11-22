---
sidebar_position: 1
---

# Introduction

<iframe width="100%" height="450" src="https://www.youtube.com/embed/H_gFueEYpYs" title="Introduction to Fig" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" allowfullscreen></iframe>

# Quick Start

To get up and running with Fig, you'll need to set up the API, Web and integrate the client nuget package into your application.

## Install API and Web Client

The API and Web Clients can be installed using Docker. This guide assumes docker is installed and running.

1. Clone the [fig repository](https://github.com/mzbrau/fig) and use the `docker-compose.yml` file included.

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

   ```csharp
   public class ExampleSettings : SettingsBase
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

5. Register Fig as a configuration provider in the `program.cs` file.

   ```csharp
   var configuration = new ConfigurationBuilder()
    .AddFig<Settings>(o =>
    {
        o.ClientName = "AspNetApi";
    }).Build();
   ```

6. Access the settings via the IOptions or IOptionsMonitor interface. E.g.

   ```csharp
   public WeatherForecastController(IOptionsMonitor<ExampleSettings> settings)
   {
       _settings = settings;
   }
   ```

7. Add an environment variable called FIG_API_URI with the URI of the Fig API. For example:

   ```
   FIG_API_URI=https://localhost:7281
   ```

8. Add a client secret (see Client Configuration section for details on how to do that)

See the **examples folder** in the source repository for more examples.

## Example Setup using WSL

![fig-local-machine-setup.excalidraw](C:\Development\SideProjects\fig\doc\fig-documentation\static\img\fig-local-machine-setup.excalidraw.png)
